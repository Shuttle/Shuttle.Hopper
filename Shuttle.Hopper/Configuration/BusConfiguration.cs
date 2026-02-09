using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class BusConfiguration(IOptions<HopperOptions> hopperOptions, ITransportService transportService) : IBusConfiguration
{
    private readonly HopperOptions _hopperOptions = Guard.AgainstNull(Guard.AgainstNull(hopperOptions).Value);
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

            if (_hopperOptions.Inbox.WorkTransportUri != null)
            {
                Inbox = new InboxConfiguration
                {
                    WorkTransport = await _transportService.GetAsync(_hopperOptions.Inbox.WorkTransportUri, cancellationToken).ConfigureAwait(false),
                    DeferredTransport =
                        _hopperOptions.Inbox.DeferredTransportUri == null
                            ? null
                            : await _transportService.GetAsync(_hopperOptions.Inbox.DeferredTransportUri, cancellationToken).ConfigureAwait(false),
                    ErrorTransport =
                        _hopperOptions.Inbox.ErrorTransportUri == null
                            ? null
                            : await _transportService.GetAsync(_hopperOptions.Inbox.ErrorTransportUri, cancellationToken).ConfigureAwait(false)
                };
            }

            if (_hopperOptions.Outbox.WorkTransportUri != null)
            {
                Outbox = new OutboxConfiguration
                {
                    WorkTransport = await _transportService.GetAsync(_hopperOptions.Outbox.WorkTransportUri, cancellationToken).ConfigureAwait(false),
                    ErrorTransport =
                        _hopperOptions.Outbox.ErrorTransportUri == null
                            ? null
                            : await _transportService.GetAsync(_hopperOptions.Outbox.ErrorTransportUri, cancellationToken).ConfigureAwait(false)
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