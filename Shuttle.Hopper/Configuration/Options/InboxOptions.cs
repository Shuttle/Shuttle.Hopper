namespace Shuttle.Hopper;

public class InboxOptions : ProcessorOptions
{
    public InboxOptions()
    {
        ThreadCount = 5;
    }

    public TimeSpan DeferredMessageProcessorResetInterval { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan DeferredMessageProcessorIdleDuration { get; set; } = TimeSpan.FromSeconds(1);

    public Uri? DeferredTransportUri { get; set; }
}