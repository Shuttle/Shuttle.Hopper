namespace Shuttle.Hopper;

public interface IServiceBusConfiguration
{
    IInboxConfiguration? Inbox { get; }
    IOutboxConfiguration? Outbox { get; }
    Task ConfigureAsync(CancellationToken cancellationToken = default);
}