using Shuttle.Core.Contract;
using Shuttle.Core.Threading;

namespace Shuttle.Hopper;

public class DeferredMessageProcessorFactory(IDeferredMessageProcessor deferredMessageProcessor) : IProcessorFactory
{
    private static readonly SemaphoreSlim Lock = new(1, 1);
    private readonly IDeferredMessageProcessor _deferredMessageProcessor = Guard.AgainstNull(deferredMessageProcessor);
    private bool _instanced;

    public async Task<IProcessor> CreateAsync(CancellationToken cancellationToken = default)
    {
        await Lock.WaitAsync(cancellationToken);

        try
        {
            if (_instanced)
            {
                throw new ProcessorException(Resources.DeferredMessageProcessorInstanceException);
            }

            _instanced = true;

            return _deferredMessageProcessor;
        }
        finally
        {
            Lock.Release();
        }
    }
}