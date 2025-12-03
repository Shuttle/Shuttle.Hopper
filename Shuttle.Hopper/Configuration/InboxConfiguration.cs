namespace Shuttle.Hopper;

public class InboxConfiguration : IInboxConfiguration
{
    public ITransport? WorkTransport { get; set; }
    public ITransport? ErrorTransport { get; set; }
    public ITransport? DeferredTransport { get; set; }
}