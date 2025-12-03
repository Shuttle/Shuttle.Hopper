namespace Shuttle.Hopper;

public class OutboxConfiguration : IOutboxConfiguration
{
    public ITransport? WorkTransport { get; set; }
    public ITransport? ErrorTransport { get; set; }
}