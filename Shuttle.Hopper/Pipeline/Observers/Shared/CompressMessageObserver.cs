using Shuttle.Core.Compression;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public interface ICompressMessageObserver : IPipelineObserver<CompressMessage>;

public class CompressMessageObserver(ICompressionService compressionService) : ICompressMessageObserver
{
    private readonly ICompressionService _compressionService = Guard.AgainstNull(compressionService);

    public async Task ExecuteAsync(IPipelineContext<CompressMessage> pipelineContext, CancellationToken cancellationToken = default)
    {
        var transportMessage = Guard.AgainstNull(Guard.AgainstNull(pipelineContext).Pipeline.State.GetTransportMessage());

        if (!transportMessage.IsCompressionEnabled())
        {
            return;
        }

        transportMessage.Message = await _compressionService.CompressAsync(transportMessage.CompressionAlgorithm, transportMessage.Message, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}