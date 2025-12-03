namespace Shuttle.Hopper;

public static class TransportFactoryExtensions
{
    extension(ITransportFactory factory)
    {
        public bool CanCreate(Uri uri)
        {
            return uri.Scheme.Equals(factory.Scheme, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}