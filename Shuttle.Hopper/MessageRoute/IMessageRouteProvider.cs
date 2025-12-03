namespace Shuttle.Hopper;

public interface IMessageRouteProvider
{
    IEnumerable<IMessageRoute> MessageRoutes { get; }
    Task AddAsync(IMessageRoute messageRoute, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetRouteUrisAsync(string messageType, CancellationToken cancellationToken = default);
}