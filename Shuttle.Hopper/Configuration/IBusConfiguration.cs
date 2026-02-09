namespace Shuttle.Hopper;

public interface IBusConfiguration
{
    IInboxConfiguration? Inbox { get; }
    IOutboxConfiguration? Outbox { get; }
    Task ConfigureAsync(CancellationToken cancellationToken = default);
}