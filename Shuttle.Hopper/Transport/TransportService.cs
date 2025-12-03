using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Reflection;

namespace Shuttle.Hopper;

public class TransportService(IOptions<ServiceBusOptions> serviceBusOptions, ITransportFactoryService transportFactoryService, IUriResolver uriResolver)
    : ITransportService
{
    private static readonly SemaphoreSlim Lock = new(1, 1);
    private readonly ServiceBusOptions _serviceBusOptions = Guard.AgainstNull(Guard.AgainstNull(serviceBusOptions).Value);
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
        return await FindAsync(uri, cancellationToken) != null;
    }

    public async Task<ITransport?> FindAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(uri);

        return await Task.FromResult(_transports.Find(candidate => candidate.Uri.Uri.Equals(uri)));
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var transport in _transports)
        {
            await _serviceBusOptions.TransportDisposing.InvokeAsync(new(transport));

            await transport.TryDisposeAsync();

            await _serviceBusOptions.TransportDisposed.InvokeAsync(new(transport));
        }

        _transports.Clear();

        _disposed = true;
    }

    public async Task<ITransport> GetAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        var transport = await FindAsync(Guard.AgainstNull(uri), cancellationToken);

        if (transport != null)
        {
            return transport;
        }

        await Lock.WaitAsync(cancellationToken);

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

                transport = new ResolvedTransport(_serviceBusOptions, await CreateAsync(_transportFactoryService.Get(resolvedTransportUri.Scheme), resolvedTransportUri, cancellationToken), transportUri);
            }
            else
            {
                transport = await CreateAsync(_transportFactoryService.Get(transportUri.Scheme), transportUri, cancellationToken);
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
        var result = transportFactory.Create(transportUri);

        Guard.AgainstNull(result, string.Format(Resources.TransportFactoryCreatedNullTransport, transportFactory.GetType().FullName, transportUri));

        await _serviceBusOptions.TransportCreated.InvokeAsync(new(result), cancellationToken);

        return result;
    }
}