using Shuttle.Core.Contract;
using Shuttle.Core.Encryption;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public interface IEncryptMessageObserver : IPipelineObserver<OnEncryptMessage>;

public class EncryptMessageObserver(IEncryptionService encryptionService) : IEncryptMessageObserver
{
    private readonly IEncryptionService _encryptionService = Guard.AgainstNull(encryptionService);

    public async Task ExecuteAsync(IPipelineContext<OnEncryptMessage> pipelineContext, CancellationToken cancellationToken = default)
    {
        var transportMessage = Guard.AgainstNull(Guard.AgainstNull(pipelineContext).Pipeline.State.GetTransportMessage());

        if (!transportMessage.EncryptionEnabled())
        {
            return;
        }

        transportMessage.Message = await _encryptionService.EncryptAsync(transportMessage.EncryptionAlgorithm, transportMessage.Message, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}