using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Reflection;

namespace Shuttle.Hopper;

public class TransportService(IOptions<HopperOptions> hopperOptions, ITransportFactoryService transportFactoryService, IUriResolver uriResolver)
    : ITransportService
{
    private static readonly SemaphoreSlim Lock = new(1, 1);
    private readonly HopperOptions _hopperOptions = Guard.AgainstNull(Guard.AgainstNull(hopperOptions).Value);
    private readonly ITransportFactoryService _transportFactoryService = Guard.AgainstNull(transportFactoryService);

    private readonly List<ITransport> _transports = [];
    private readonly IUriResolver _uriResolver = Guard.AgainstNull(uriResolver);
    private bool _disposed;

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }

    public async ValueTask<bool> ContainsAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        return await FindAsync(uri, cancellationToken).ConfigureAwait(false) != null;
    }

    public Task<ITransport?> FindAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(uri);

        return Task.FromResult(_transports.Find(candidate => candidate.Uri.Uri.Equals(uri)));
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var transport in _transports)
        {
            await _hopperOptions.TransportDisposing.InvokeAsync(new(transport)).ConfigureAwait(false);

            await transport.TryDisposeAsync();

            await _hopperOptions.TransportDisposed.InvokeAsync(new(transport)).ConfigureAwait(false);
        }

        _transports.Clear();

        _disposed = true;
    }

    public async Task<ITransport> GetAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        var transport = await FindAsync(Guard.AgainstNull(uri), cancellationToken).ConfigureAwait(false);

        if (transport != null)
        {
            return transport;
        }

        await Lock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            transport = _transports.Find(candidate => candidate.Uri.Uri.Equals(uri));

            if (transport != null)
            {
                return transport;
            }

            var transportUri = uri;

            if (transportUri.Scheme.Equals("resolver"))
            {
                var resolvedTransportUri = _uriResolver.GetTarget(transportUri);

                if (resolvedTransportUri == null)
                {
                    throw new KeyNotFoundException(string.Format(Resources.UriNameNotFoundException, _uriResolver.GetType().FullName, uri));
                }

                transport = new ResolvedTransport(await CreateAsync(_transportFactoryService.Get(resolvedTransportUri.Scheme), resolvedTransportUri, cancellationToken).ConfigureAwait(false), transportUri);
            }
            else
            {
                transport = await CreateAsync(_transportFactoryService.Get(transportUri.Scheme), transportUri, cancellationToken).ConfigureAwait(false); 
            }

            _transports.Add(transport);

            return transport;
        }
        finally
        {
            Lock.Release();
        }
    }

    private async Task<ITransport> CreateAsync(ITransportFactory transportFactory, Uri transportUri, CancellationToken cancellationToken)
    {
        var result = await transportFactory.CreateAsync(transportUri, cancellationToken).ConfigureAwait(false); 

        Guard.AgainstNull(result, string.Format(Resources.TransportFactoryCreatedNullTransport, transportFactory.GetType().FullName, transportUri));

        await _hopperOptions.TransportCreated.InvokeAsync(new(result), cancellationToken).ConfigureAwait(false); 

        return result;
    }
}