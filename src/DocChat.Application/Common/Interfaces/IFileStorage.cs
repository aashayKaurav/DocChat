using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DocChat.Application.Common.Interfaces;

public interface IFileStorage
{
    Task<string> SaveFileAsync(string fileName, Stream content, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string storagePath, CancellationToken cancellationToken = default);
}
