using Shuttle.Core.Threading;

namespace Shuttle.Hopper;

public interface IDeferredMessageProcessor : IProcessor
{
    Task MessageDeferredAsync(DateTime ignoreTillDate);
}