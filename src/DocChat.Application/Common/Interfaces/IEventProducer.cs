using System.Threading;
using System.Threading.Tasks;

namespace DocChat.Application.Common.Interfaces;

public interface IEventProducer
{
    Task PublishAsync<T>(string topic, string key, T message, CancellationToken cancellationToken = default);
}