namespace Shuttle.Hopper;

public class InboxOptions : ProcessorOptions
{
    public InboxOptions()
    {
        ThreadCount = 5;
    }

    public TimeSpan DeferredMessageProcessorResetInterval { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan DeferredMessageProcessorWaitInterval { get; set; } = TimeSpan.FromSeconds(1);

    public string DeferredTransportUri { get; set; } = string.Empty;
}