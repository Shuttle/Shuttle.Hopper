using Shuttle.Core.Compression;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public interface IDecompressMessageObserver : IPipelineObserver<DecompressMessage>;

public class DecompressMessageObserver(ICompressionService compressionService) : IDecompressMessageObserver
{
    private readonly ICompressionService _compressionService = Guard.AgainstNull(compressionService);

    public async Task ExecuteAsync(IPipelineContext<DecompressMessage> pipelineContext, CancellationToken cancellationToken = default)
    {
        var transportMessage = Guard.AgainstNull(Guard.AgainstNull(pipelineContext).Pipeline.State.GetTransportMessage());

        if (!transportMessage.IsCompressionEnabled())
        {
            return;
        }

        transportMessage.Message = await _compressionService.DecompressAsync(transportMessage.CompressionAlgorithm, transportMessage.Message, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}