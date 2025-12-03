using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Serialization;
using Shuttle.Core.Streams;
using Shuttle.Core.System;

namespace Shuttle.Hopper;

public interface IDeserializeTransportMessageObserver : IPipelineObserver<OnDeserializeTransportMessage>;

public class DeserializeTransportMessageObserver(IOptions<ServiceBusOptions> serviceBusOptions, ISerializer serializer, IEnvironmentService environmentService, IProcessService processService)
    : IDeserializeTransportMessageObserver
{
    private readonly IEnvironmentService _environmentService = Guard.AgainstNull(environmentService);
    private readonly IProcessService _processService = Guard.AgainstNull(processService);
    private readonly ISerializer _serializer = Guard.AgainstNull(serializer);
    private readonly ServiceBusOptions _serviceBusOptions = Guard.AgainstNull(Guard.AgainstNull(serviceBusOptions).Value);

    public async Task ExecuteAsync(IPipelineContext<OnDeserializeTransportMessage> pipelineContext, CancellationToken cancellationToken = default)
    {
        var state = Guard.AgainstNull(pipelineContext).Pipeline.State;
        var receivedMessage = Guard.AgainstNull(state.GetReceivedMessage());
        var workTransport = Guard.AgainstNull(state.GetWorkTransport());

        TransportMessage transportMessage;

        try
        {
            await using var stream = await receivedMessage.Stream.CopyAsync().ConfigureAwait(false);
            transportMessage = (TransportMessage)await _serializer.DeserializeAsync(typeof(TransportMessage), stream, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await _serviceBusOptions.TransportMessageDeserializationException.InvokeAsync(new(pipelineContext, workTransport, Guard.AgainstNull(state.GetErrorTransport()), ex), cancellationToken);

            if (_serviceBusOptions.RemoveCorruptMessages)
            {
                await workTransport.AcknowledgeAsync(Guard.AgainstNull(state.GetReceivedMessage()).AcknowledgementToken, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                if (!_environmentService.UserInteractive)
                {
                    _processService.GetCurrentProcess().Kill();
                }

                return;
            }

            pipelineContext.Pipeline.Abort();

            return;
        }

        state.SetTransportMessage(transportMessage);
        state.SetMessageBytes(transportMessage.Message);

        transportMessage.AcceptInvariants();
    }
}