namespace Shuttle.Hopper;

public interface IUriResolver
{
    void Add(Uri sourceUri, Uri targetUri);
    Uri GetTarget(Uri sourceUri);
}