using Microsoft.Extensions.Options;
using Shuttle.Contract;
using Shuttle.Pipelines;
using Shuttle.Reflection;
using Shuttle.Serialization;

namespace Shuttle.Hopper;

public interface IDeserializeMessageObserver : IPipelineObserver<DeserializeMessage>;

public class DeserializeMessageObserver(IOptions<HopperOptions> hopperOptions, ISerializer serializer) : IDeserializeMessageObserver
{
    private readonly ISerializer _serializer = Guard.AgainstNull(serializer);
    private readonly HopperOptions _hopperOptions = Guard.AgainstNull(Guard.AgainstNull(hopperOptions).Value);

    public async Task ExecuteAsync(IPipelineContext<DeserializeMessage> pipelineContext, CancellationToken cancellationToken = default)
    {
        var state = Guard.AgainstNull(pipelineContext).Pipeline.State;

        var transportMessage = Guard.AgainstNull(state.GetTransportMessage());

        object message;

        try
        {
            var data = transportMessage.Message;

            using var stream = new MemoryStream(data, 0, data.Length, false, true);
            message = await _serializer.DeserializeAsync(Guard.AgainstNull(Type.GetType(Guard.AgainstNull(transportMessage.AssemblyQualifiedName), true, true)), stream, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var workTransport = Guard.AgainstNull(state.GetWorkTransport());
            var errorTransport = Guard.AgainstNull(state.GetErrorTransport());
            var receivedMessage = state.GetReceivedMessage();

            await _hopperOptions.MessageDeserializationException.InvokeAsync(new(pipelineContext, workTransport, errorTransport, ex), cancellationToken);

            if (workTransport == null || errorTransport == null || receivedMessage == null || workTransport.Type == TransportType.Stream)
            {
                throw;
            }

            transportMessage.RegisterFailure(ex.AllMessages(), TimeSpan.Zero);

            await errorTransport.SendAsync(await _serializer.SerializeAsync(transportMessage, cancellationToken).ConfigureAwait(false), pipelineContext.Pipeline.State, cancellationToken).ConfigureAwait(false);
            await workTransport.AcknowledgeAsync(receivedMessage.AcknowledgementToken, cancellationToken).ConfigureAwait(false);

            pipelineContext.Pipeline.Abort();

            return;
        }

        state.SetMessage(message);
    }
}