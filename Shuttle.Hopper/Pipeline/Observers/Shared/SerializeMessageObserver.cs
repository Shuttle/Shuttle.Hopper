using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Serialization;
using Shuttle.Core.Streams;

namespace Shuttle.Hopper;

public interface ISerializeMessageObserver : IPipelineObserver<OnSerializeMessage>;

public class SerializeMessageObserver(ISerializer serializer) : ISerializeMessageObserver
{
    private readonly ISerializer _serializer = Guard.AgainstNull(serializer);

    public async Task ExecuteAsync(IPipelineContext<OnSerializeMessage> pipelineContext, CancellationToken cancellationToken = default)
    {
        var state = Guard.AgainstNull(pipelineContext).Pipeline.State;
        var message = Guard.AgainstNull(state.GetMessage());
        var transportMessage = Guard.AgainstNull(state.GetTransportMessage());

        await using (var stream = await _serializer.SerializeAsync(message).ConfigureAwait(false))
        {
            transportMessage.Message = await stream.ToBytesAsync().ConfigureAwait(false);
        }

        state.SetMessageBytes(transportMessage.Message);
    }
}