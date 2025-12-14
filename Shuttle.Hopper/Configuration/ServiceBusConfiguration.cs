using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class ServiceBusConfiguration(IOptions<ServiceBusOptions> serviceBusOptions, ITransportService transportService) : IServiceBusConfiguration
{
    private readonly ServiceBusOptions _serviceBusOptions = Guard.AgainstNull(Guard.AgainstNull(serviceBusOptions).Value);
    private readonly ITransportService _transportService = Guard.AgainstNull(transportService);

    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _configured;

    public IInboxConfiguration? Inbox { get; private set; }
    public IOutboxConfiguration? Outbox { get; private set; }

    public async Task ConfigureAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            if (_configured)
            {
                return;
            }

            if (_serviceBusOptions.Inbox.WorkTransportUri != null)
            {
                Inbox = new InboxConfiguration
                {
                    WorkTransport = await _transportService.GetAsync(_serviceBusOptions.Inbox.WorkTransportUri, cancellationToken).ConfigureAwait(false),
                    DeferredTransport =
                        _serviceBusOptions.Inbox.DeferredTransportUri == null
                            ? null
                            : await _transportService.GetAsync(_serviceBusOptions.Inbox.DeferredTransportUri, cancellationToken).ConfigureAwait(false),
                    ErrorTransport =
                        _serviceBusOptions.Inbox.ErrorTransportUri == null
                            ? null
                            : await _transportService.GetAsync(_serviceBusOptions.Inbox.ErrorTransportUri, cancellationToken).ConfigureAwait(false)
                };
            }

            if (_serviceBusOptions.Outbox.WorkTransportUri != null)
            {
                Outbox = new OutboxConfiguration
                {
                    WorkTransport = await _transportService.GetAsync(_serviceBusOptions.Outbox.WorkTransportUri, cancellationToken).ConfigureAwait(false),
                    ErrorTransport =
                        _serviceBusOptions.Outbox.ErrorTransportUri == null
                            ? null
                            : await _transportService.GetAsync(_serviceBusOptions.Outbox.ErrorTransportUri, cancellationToken).ConfigureAwait(false)
                };
            }

            _configured = true;
        }
        finally
        {
            _lock.Release();
        }
    }
}