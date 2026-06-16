using System.Text.Json;
using Confluent.Kafka;
using DocChat.Application.Common.Interfaces;
using DocChat.Consumer.Services;
using DocChat.Domain.Entities;
using DocChat.Domain.Enums;

namespace DocChat.Consumer.Workers;

public class DocumentProcessingWorker : BackgroundService
{
    private readonly ILogger<DocumentProcessingWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly PdfParserService _pdfParser;
    private readonly TextChunkerService _textChunker;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;

    public DocumentProcessingWorker(
        ILogger<DocumentProcessingWorker> logger,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        PdfParserService pdfParser,
        TextChunkerService textChunker,
        IEmbeddingService embeddingService,
        IVectorStore vectorStore)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _pdfParser = pdfParser;
        _textChunker = textChunker;
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"],
            GroupId = _configuration["Kafka:ConsumerGroupId"] ?? "docchat-document-processor",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();

        var topic = _configuration["Kafka:DocumentUploadedTopic"] ?? "document-uploaded";
        consumer.Subscribe(topic);

        _logger.LogInformation("Listening on Kafka topic: {Topic}", topic);

        // Ensure Qdrant collection exists
        await _vectorStore.EnsureCollectionExistsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                var message = JsonSerializer.Deserialize<DocumentUploadedEvent>(result.Message.Value);

                if (message == null) continue;

                _logger.LogInformation("Processing document: {DocumentId}", message.DocumentId);

                await ProcessDocumentAsync(message, stoppingToken);

                consumer.Commit(result);
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing document");
            }
        }

        consumer.Close();
    }

    private async Task ProcessDocumentAsync(DocumentUploadedEvent message, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var documentRepo = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();

        var document = await documentRepo.GetByIdAsync(message.DocumentId, cancellationToken);
        if (document == null)
        {
            _logger.LogWarning("Document {DocumentId} not found in database", message.DocumentId);
            return;
        }

        try
        {
            // Update status to Processing
            document.Status = DocumentStatus.Processing;
            await documentRepo.UpdateAsync(document, cancellationToken);

            // 1. Extract text from file
            var text = ExtractText(document.StoragePath, document.ContentType);
            _logger.LogInformation("Extracted {Length} characters from {FileName}", text.Length, document.FileName);

            // 2. Chunk the text
            var chunks = _textChunker.ChunkText(text);
            _logger.LogInformation("Created {Count} chunks from {FileName}", chunks.Count, document.FileName);

            // 3. Save chunks to database
            for (int i = 0; i < chunks.Count; i++)
            {
                document.Chunks.Add(new DocumentChunk
                {
                    DocumentId = document.Id,
                    ChunkIndex = i,
                    Content = chunks[i],
                    TokenCount = chunks[i].Length // rough approximation
                });
            }

            document.ChunkCount = chunks.Count;
            await documentRepo.UpdateAsync(document, cancellationToken);
            _logger.LogInformation("Saved {Count} chunks to database", chunks.Count);

            // 4. Generate embeddings and store in Qdrant
            _logger.LogInformation("Generating embeddings for {Count} chunks", chunks.Count);
            var embeddings = await _embeddingService.GenerateEmbeddingsAsync(chunks, cancellationToken);

            for (int i = 0; i < chunks.Count; i++)
            {
                var chunkEntity = document.Chunks.ElementAt(i);
                await _vectorStore.UpsertAsync(chunkEntity.Id, document.Id, i, embeddings[i], cancellationToken);
            }
            _logger.LogInformation("Stored {Count} vectors in Qdrant", chunks.Count);

            // 5. Mark as ready
            document.Status = DocumentStatus.Ready;
            document.ProcessedAt = DateTime.UtcNow;
            await documentRepo.UpdateAsync(document, cancellationToken);
            _logger.LogInformation("Document {DocumentId} processed successfully", document.Id);
        }
        catch (Exception ex)
        {
            document.Status = DocumentStatus.Failed;
            document.ErrorMessage = ex.Message;
            await documentRepo.UpdateAsync(document, cancellationToken);

            _logger.LogError(ex, "Failed to process document {DocumentId}", document.Id);
        }
    }

    private string ExtractText(string filePath, string contentType)
    {
        return contentType switch
        {
            "application/pdf" => _pdfParser.ExtractText(filePath),
            "text/plain" => File.ReadAllText(filePath),
            _ => throw new NotSupportedException($"Content type {contentType} is not supported")
        };
    }
}

// Event model matching what the API publishes
public class DocumentUploadedEvent
{
    public Guid DocumentId { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}