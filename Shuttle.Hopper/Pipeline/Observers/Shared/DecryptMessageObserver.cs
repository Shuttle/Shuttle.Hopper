using Shuttle.Core.Contract;
using Shuttle.Core.Encryption;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public interface IDecryptMessageObserver : IPipelineObserver<OnDecryptMessage>;

public class DecryptMessageObserver(IEncryptionService encryptionService) : IDecryptMessageObserver
{
    private readonly IEncryptionService _encryptionService = Guard.AgainstNull(encryptionService);

    public async Task ExecuteAsync(IPipelineContext<OnDecryptMessage> pipelineContext, CancellationToken cancellationToken = default)
    {
        var transportMessage = Guard.AgainstNull(Guard.AgainstNull(pipelineContext).Pipeline.State.GetTransportMessage());

        if (!transportMessage.EncryptionEnabled())
        {
            return;
        }

        transportMessage.Message = await _encryptionService.DecryptAsync(transportMessage.EncryptionAlgorithm, transportMessage.Message).ConfigureAwait(false);
    }
}