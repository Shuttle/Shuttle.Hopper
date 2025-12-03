using System.Text.RegularExpressions;
using Shuttle.Core.Contract;
using Shuttle.Core.Specification;

namespace Shuttle.Hopper;

public class RegexMessageRouteSpecification(string pattern) : ISpecification<string>
{
    private readonly Regex _regex = new(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public bool IsSatisfiedBy(string messageType)
    {
        return _regex.IsMatch(Guard.AgainstEmpty(messageType));
    }
}