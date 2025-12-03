using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class MessageRouteSpecificationConfiguration(string name, string value)
{
    public string Name { get; } = Guard.AgainstEmpty(name);
    public string Value { get; } = Guard.AgainstEmpty(value);
}