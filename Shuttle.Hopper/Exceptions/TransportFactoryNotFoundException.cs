namespace Shuttle.Hopper;

internal class TransportFactoryNotFoundException(string scheme) : Exception(string.Format(Resources.TransportFactoryNotFoundException, scheme));