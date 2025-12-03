using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class UriResolver : IUriResolver
{
    private readonly Dictionary<string, Uri> _targetUris = new();

    public UriResolver(IOptions<ServiceBusOptions> serviceBusOptions)
    {
        foreach (var configuration in Guard.AgainstNull(Guard.AgainstNull(serviceBusOptions).Value).UriMappings)
        {
            Add(new(configuration.SourceUri), new(configuration.TargetUri));
        }
    }

    public Uri GetTarget(Uri sourceUri)
    {
        return !_targetUris.TryGetValue(sourceUri.OriginalString.ToLower(), out var result)
            ? throw new InvalidOperationException(string.Format(Resources.CouldNotResolveSourceUriException, sourceUri.ToString()))
            : result;
    }

    public void Add(Uri sourceUri, Uri targetUri)
    {
        _targetUris.Add(Guard.AgainstNull(sourceUri).OriginalString.ToLower(), targetUri);
    }
}