using DocChat.Application.Common.Interfaces;
using DocChat.Application.Documents.Commands;
using MediatR;
using System.Threading;

namespace DocChat.API.Endpoints;

public static class DocumentEndpoints
{
    public static void MapDocumentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/documents");

        // Upload a document
        group.MapPost("/upload", async (IFormFile file, ISender mediator, CancellationToken cancellationToken) =>
        {
            if (file == null || file.Length == 0)
                return Results.BadRequest("No file provided.");

            var allowedTypes = new[] { "application/pdf", "text/plain" };
            if (!allowedTypes.Contains(file.ContentType))
                return Results.BadRequest("Only PDF and TXT files are supported.");

            var command = new UploadDocumentCommand(
                file.FileName,
                file.ContentType,
                file.Length,
                file.OpenReadStream()
            );

            var documentId = await mediator.Send(command, cancellationToken);

            return Results.Ok(new { documentId, message = "Document uploaded successfully. Processing started." });
        })
        .DisableAntiforgery();

        // Get all documents
        group.MapGet("/", async (IDocumentRepository repo, CancellationToken cancellationToken) =>
        {
            var documents = await repo.GetAllAsync(cancellationToken);
            return Results.Ok(documents);
        });
    }
}