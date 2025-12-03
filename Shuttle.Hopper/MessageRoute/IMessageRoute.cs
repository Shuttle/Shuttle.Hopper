using Shuttle.Core.Specification;

namespace Shuttle.Hopper;

public interface IMessageRoute
{
    IEnumerable<ISpecification<string>> Specifications { get; }
    Uri Uri { get; }
    IMessageRoute AddSpecification(ISpecification<string> specification);
    bool IsSatisfiedBy(string messageType);
}