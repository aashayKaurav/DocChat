using DocChat.Application.Common.Interfaces;
using DocChat.Domain.Entities;
using DocChat.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace DocChat.Application.Documents.Commands;

// The command — what the endpoint sends in
public record UploadDocumentCommand(string FileName, string ContentType, long FileSize, Stream FileStream) : IRequest<Guid>;

// The handler — what actually happens
public class UploadDocumentCommandHandler : IRequestHandler<UploadDocumentCommand, Guid>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IFileStorage _fileStorage;
    private readonly IEventProducer _eventProducer;
    private readonly IConfiguration _configuration;

    public UploadDocumentCommandHandler(
        IDocumentRepository documentRepository,
        IFileStorage fileStorage,
        IEventProducer eventProducer,
        IConfiguration configuration)
    {
        _documentRepository = documentRepository;
        _fileStorage = fileStorage;
        _eventProducer = eventProducer;
        _configuration = configuration;
    }

    public async Task<Guid> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        // 1. Save file to disk
        var storagePath = await _fileStorage.SaveFileAsync(request.FileName, request.FileStream, cancellationToken);

        // 2. Create document record in database
        var document = new Document
        {
            Id = Guid.NewGuid(),
            FileName = request.FileName,
            ContentType = request.ContentType,
            FileSize = request.FileSize,
            StoragePath = storagePath,
            Status = DocumentStatus.Uploaded
        };

        await _documentRepository.AddAsync(document, cancellationToken);

        // 3. Publish event to Kafka for async processing
        var topic = _configuration["Kafka:DocumentUploadedTopic"] ?? "document-uploaded";
        await _eventProducer.PublishAsync(topic, document.Id.ToString(), new
        {
            DocumentId = document.Id,
            document.StoragePath,
            document.FileName
        }, cancellationToken);

        return document.Id;
    }
}