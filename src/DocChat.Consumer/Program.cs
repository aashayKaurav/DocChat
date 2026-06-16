using DocChat.Consumer.Services;
using DocChat.Consumer.Workers;
using DocChat.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddSingleton<PdfParserService>();
builder.Services.AddSingleton<TextChunkerService>();
builder.Services.AddHostedService<DocumentProcessingWorker>();

var host = builder.Build();
host.Run();