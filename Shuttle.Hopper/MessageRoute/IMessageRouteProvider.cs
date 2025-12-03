namespace Shuttle.Hopper;

public interface IMessageRouteProvider
{
    IEnumerable<IMessageRoute> MessageRoutes { get; }
    Task AddAsync(IMessageRoute messageRoute);
    Task<IEnumerable<string>> GetRouteUrisAsync(string messageType);
}