using System.Collections.ObjectModel;
using Shuttle.Contract;
using Shuttle.Specification;

namespace Shuttle.Hopper;

public class MessageRoute(Uri uri) : IMessageRoute
{
    private readonly List<ISpecification<string>> _specifications = [];

    public IMessageRoute AddSpecification(ISpecification<string> specification)
    {
        _specifications.Add(Guard.AgainstNull(specification));

        return this;
    }

    public bool IsSatisfiedBy(string messageType)
    {
        return _specifications.Any(specification => specification.IsSatisfiedBy(messageType));
    }

    public Uri Uri { get; } = Guard.AgainstNull(uri);

    public IEnumerable<ISpecification<string>> Specifications => new ReadOnlyCollection<ISpecification<string>>(_specifications);
}