namespace Shuttle.Hopper;

public class InvalidSchemeException(string supportedScheme, string invalidUri) : Exception(string.Format(Resources.InvalidSchemeException, supportedScheme, invalidUri));