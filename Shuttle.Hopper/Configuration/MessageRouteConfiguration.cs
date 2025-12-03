using System.Collections.ObjectModel;
using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class MessageRouteConfiguration(string uri)
{
    private readonly List<MessageRouteSpecificationConfiguration> _specifications = [];

    public IEnumerable<MessageRouteSpecificationConfiguration> Specifications => new
        ReadOnlyCollection<MessageRouteSpecificationConfiguration>(_specifications);

    public string Uri { get; } = uri;

    public void AddSpecification(string name, string value)
    {
        Guard.AgainstEmpty(name);
        Guard.AgainstEmpty(value);

        _specifications.Add(new(name, value));
    }
}