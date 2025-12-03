namespace Shuttle.Hopper;

public class TransportFactoryNotFoundException(string scheme) : Exception(string.Format(Resources.TransportFactoryNotFoundException, scheme));