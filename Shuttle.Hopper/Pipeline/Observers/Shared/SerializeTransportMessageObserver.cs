using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Serialization;

namespace Shuttle.Hopper;

public interface ISerializeTransportMessageObserver : IPipelineObserver<OnSerializeTransportMessage>;

public class SerializeTransportMessageObserver(ISerializer serializer) : ISerializeTransportMessageObserver
{
    private readonly ISerializer _serializer = Guard.AgainstNull(serializer);

    public async Task ExecuteAsync(IPipelineContext<OnSerializeTransportMessage> pipelineContext, CancellationToken cancellationToken = default)
    {
        var state = Guard.AgainstNull(pipelineContext).Pipeline.State;

        state.SetTransportMessageStream(await _serializer.SerializeAsync(Guard.AgainstNull(state.GetTransportMessage()), cancellationToken).ConfigureAwait(false));
    }
}