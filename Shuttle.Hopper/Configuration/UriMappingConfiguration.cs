using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class UriMappingConfiguration(Uri sourceUri, Uri targetUri)
{
    public Uri SourceUri { get; set; } = Guard.AgainstNull(sourceUri);
    public Uri TargetUri { get; set; } = Guard.AgainstNull(targetUri);
}