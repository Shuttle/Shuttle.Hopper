using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class TransportUri
{
    public TransportUri(Uri uri)
    {
        Uri = Guard.AgainstNull(uri);

        if (uri.LocalPath == "/" || uri.Segments.Length != 2)
        {
            throw new UriFormatException(string.Format(Resources.UriFormatException, $"{uri.Scheme}://{{configuration-name}}/{{topic}}", uri));
        }

        ConfigurationName = Uri.Host;
        TransportName = Uri.Segments[1];
    }

    public TransportUri(string uri) : this(new Uri(uri))
    {
    }

    public string ConfigurationName { get; }

    public string TransportName { get; }
    public Uri Uri { get; }

    public TransportUri SchemeInvariant(string scheme)
    {
        return !Uri.Scheme.Equals(Guard.AgainstEmpty(scheme), StringComparison.InvariantCultureIgnoreCase) ? throw new InvalidSchemeException(Uri.Scheme, Uri.ToString()) : this;
    }

    public override string ToString()
    {
        return Uri.ToString();
    }
}