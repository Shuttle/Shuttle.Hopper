namespace Shuttle.Hopper;

public interface IInboxConfiguration : IWorkProcessorConfiguration
{
    ITransport? DeferredTransport { get; set; }
}