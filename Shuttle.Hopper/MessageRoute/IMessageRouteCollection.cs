namespace Shuttle.Hopper;

public interface IMessageRouteCollection : IEnumerable<IMessageRoute>
{
    IMessageRouteCollection Add(IMessageRoute messageRoute);

    List<IMessageRoute> FindByMessageType(string messageType);
    IMessageRoute? FindByUri(string uri);
}