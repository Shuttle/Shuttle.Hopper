using Microsoft.Extensions.Options;
using Shuttle.Contract;

namespace Shuttle.Hopper;

public class BusConfiguration(IOptions<HopperOptions> hopperOptions, ITransportService transportService) : IBusConfiguration
{
    private readonly HopperOptions _hopperOptions = Guard.AgainstNull(Guard.AgainstNull(hopperOptions).Value);
    private readonly ITransportService _transportService = Guard.AgainstNull(transportService);

    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Dictionary<string, IInboxConfiguration> _additionalInboxes = new(StringComparer.OrdinalIgnoreCase);
    private bool _configured;

    public IInboxConfiguration? Inbox { get; private set; }
    public IReadOnlyDictionary<string, IInboxConfiguration> AdditionalInboxes => _additionalInboxes;
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

            ValidateAdditionalInboxes();

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

            foreach (var (name, inboxOptions) in _hopperOptions.AdditionalInboxes)
            {
                _additionalInboxes.Add(name, new InboxConfiguration
                {
                    WorkTransport = await _transportService.GetAsync(inboxOptions.WorkTransportUri!, cancellationToken).ConfigureAwait(false),
                    ErrorTransport = inboxOptions.ErrorTransportUri == null
                        ? Inbox!.ErrorTransport
                        : await _transportService.GetAsync(inboxOptions.ErrorTransportUri, cancellationToken).ConfigureAwait(false),
                    DeferredTransport = inboxOptions.DeferredTransportUri == null
                        ? Inbox!.DeferredTransport
                        : await _transportService.GetAsync(inboxOptions.DeferredTransportUri, cancellationToken).ConfigureAwait(false)
                });
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

    private void ValidateAdditionalInboxes()
    {
        if (_hopperOptions.AdditionalInboxes.Count == 0)
        {
            return;
        }

        Guard.Against<InvalidOperationException>(_hopperOptions.Inbox.WorkTransportUri == null, string.Format(Resources.AdditionalInboxPrimaryInboxRequiredException, string.Join(", ", _hopperOptions.AdditionalInboxes.Keys)));

        foreach (var (name, inboxOptions) in _hopperOptions.AdditionalInboxes)
        {
            Guard.Against<InvalidOperationException>(inboxOptions.WorkTransportUri == null, string.Format(Resources.AdditionalInboxWorkTransportUriMissingException, name));
            Guard.Against<InvalidOperationException>(inboxOptions.DeferredTransportUri != null && inboxOptions.ErrorTransportUri == null && _hopperOptions.Inbox.ErrorTransportUri == null, string.Format(Resources.AdditionalInboxErrorTransportUriRequiredException, name));
        }

        // An additional inbox may share an error transport, but its work and deferred transports may not be any other
        // transport used by the endpoint.
        var usedUris = new List<Uri?>
            {
                _hopperOptions.Inbox.WorkTransportUri,
                _hopperOptions.Inbox.DeferredTransportUri,
                _hopperOptions.Inbox.ErrorTransportUri,
                _hopperOptions.Outbox.WorkTransportUri,
                _hopperOptions.Outbox.ErrorTransportUri
            }
            .Concat(_hopperOptions.AdditionalInboxes.Values.Select(item => item.ErrorTransportUri))
            .OfType<Uri>()
            .ToList();

        foreach (var (name, inboxOptions) in _hopperOptions.AdditionalInboxes)
        {
            foreach (var transportUri in new[] { inboxOptions.WorkTransportUri, inboxOptions.DeferredTransportUri }.OfType<Uri>())
            {
                Guard.Against<InvalidOperationException>(usedUris.Any(uri => uri.Equals(transportUri)), string.Format(Resources.AdditionalInboxDuplicateTransportUriException, name, transportUri));

                usedUris.Add(transportUri);
            }
        }
    }
}