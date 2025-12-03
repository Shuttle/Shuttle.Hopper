using System.Collections;
using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class MessageRouteCollection : IMessageRouteCollection
{
    private readonly List<IMessageRoute> _messageRoutes = [];

    public IMessageRouteCollection Add(IMessageRoute messageRoute)
    {
        Guard.AgainstNull(messageRoute);

        var existing = this.FindByUri(messageRoute.Uri);

        if (existing == null)
        {
            _messageRoutes.Add(messageRoute);
        }
        else
        {
            foreach (var specification in messageRoute.Specifications)
            {
                existing.AddSpecification(specification);
            }
        }

        return this;
    }

    public List<IMessageRoute> FindByMessageType(string messageType)
    {
        Guard.AgainstNull(messageType);

        return _messageRoutes.Where(map => map.IsSatisfiedBy(messageType)).ToList();
    }

    public IMessageRoute? FindByUri(string uri)
    {
        Guard.AgainstEmpty(uri);

        return _messageRoutes.Find(map => map.Uri.ToString().Equals(uri, StringComparison.InvariantCultureIgnoreCase));
    }

    public IEnumerator<IMessageRoute> GetEnumerator()
    {
        return _messageRoutes.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}