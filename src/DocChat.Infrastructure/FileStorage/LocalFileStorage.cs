using DocChat.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace DocChat.Infrastructure.FileStorage;

public class LocalFileStorage : IFileStorage
{
    private readonly string _basePath;

    public LocalFileStorage(IConfiguration configuration)
    {
        _basePath = configuration["FileStorage:BasePath"] ?? "./storage/documents";
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> SaveFileAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        // Create a unique subfolder to avoid filename collisions
        var uniqueFolder = Guid.NewGuid().ToString();
        var folderPath = Path.Combine(_basePath, uniqueFolder);
        Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, fileName);

        using var fileStream = new FileStream(filePath, FileMode.Create);
        await content.CopyToAsync(fileStream, cancellationToken);

        return Path.GetFullPath(filePath);
    }

    public Task DeleteFileAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        if (File.Exists(storagePath))
        {
            File.Delete(storagePath);

            // Clean up the parent folder if empty
            var directory = Path.GetDirectoryName(storagePath);
            if (directory != null && Directory.Exists(directory) && !Directory.EnumerateFiles(directory).Any())
            {
                Directory.Delete(directory);
            }
        }

        return Task.CompletedTask;
    }
}
