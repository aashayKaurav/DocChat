using DocChat.Application.Common.Interfaces;
using DocChat.Infrastructure.AI;
using DocChat.Infrastructure.Embeddings;
using DocChat.Infrastructure.FileStorage;
using DocChat.Infrastructure.Kafka;
using DocChat.Infrastructure.Persistence;
using DocChat.Infrastructure.Persistence.Repositories;
using DocChat.Infrastructure.VectorStore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocChat.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddSingleton<IEventProducer, KafkaProducer>();

        // Ollama services (free, local)
        services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>(client =>
        {
            client.BaseAddress = new Uri(configuration["Ollama:Host"] ?? "http://localhost:11434");
        });

        services.AddHttpClient<ILlmService, OllamaLlmService>(client =>
        {
            client.BaseAddress = new Uri(configuration["Ollama:Host"] ?? "http://localhost:11434");
            client.Timeout = TimeSpan.FromMinutes(5);
        });

        services.AddHttpClient<IVectorStore, QdrantVectorStore>(client =>
        {
            client.BaseAddress = new Uri(configuration["Qdrant:Host"] ?? "http://localhost:6333");
        });

        return services;
    }
}