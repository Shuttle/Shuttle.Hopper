namespace Shuttle.Hopper;

public interface IBusConfiguration
{
    IInboxConfiguration? Inbox { get; }
    IReadOnlyDictionary<string, IInboxConfiguration> AdditionalInboxes { get; }
    IOutboxConfiguration? Outbox { get; }
    Task ConfigureAsync(CancellationToken cancellationToken = default);
}