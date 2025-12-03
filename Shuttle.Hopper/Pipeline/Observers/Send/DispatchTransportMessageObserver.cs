using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Streams;

namespace Shuttle.Hopper;

public interface IDispatchTransportMessageObserver : IPipelineObserver<OnDispatchTransportMessage>;

public class DispatchTransportMessageObserver(IServiceBusConfiguration serviceBusConfiguration, ITransportService transportService)
    : IDispatchTransportMessageObserver
{
    private readonly IServiceBusConfiguration _serviceBusConfiguration = Guard.AgainstNull(serviceBusConfiguration);
    private readonly ITransportService _transportService = Guard.AgainstNull(transportService);

    public async Task ExecuteAsync(IPipelineContext<OnDispatchTransportMessage> pipelineContext, CancellationToken cancellationToken = default)
    {
        var state = Guard.AgainstNull(pipelineContext).Pipeline.State;
        var transportMessage = Guard.AgainstNull(state.GetTransportMessage());

        Guard.AgainstEmpty(transportMessage.RecipientInboxWorkTransportUri);

        var transport = !_serviceBusConfiguration.HasOutbox()
            ? await _transportService.GetAsync(transportMessage.RecipientInboxWorkTransportUri, cancellationToken)
            : Guard.AgainstNull(_serviceBusConfiguration.Outbox!.WorkTransport);

        await using var stream = await Guard.AgainstNull(state.GetTransportMessageStream()).CopyAsync().ConfigureAwait(false);

        await transport.SendAsync(transportMessage, stream, cancellationToken).ConfigureAwait(false);
    }
}