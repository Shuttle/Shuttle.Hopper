using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public interface IFindMessageRouteObserver : IPipelineObserver<OnFindRouteForMessage>;

public class FindMessageRouteObserver(IMessageRouteProvider messageRouteProvider) : IFindMessageRouteObserver
{
    private readonly IMessageRouteProvider _messageRouteProvider = Guard.AgainstNull(messageRouteProvider);

    public async Task ExecuteAsync(IPipelineContext<OnFindRouteForMessage> pipelineContext, CancellationToken cancellationToken = default)
    {
        var state = Guard.AgainstNull(pipelineContext).Pipeline.State;
        var transportMessage = Guard.AgainstNull(state.GetTransportMessage());

        if (!string.IsNullOrEmpty(transportMessage.RecipientInboxWorkTransportUri))
        {
            return;
        }

        var routeUris = (await _messageRouteProvider.GetRouteUrisAsync(transportMessage.MessageType)).ToList();

        if (!routeUris.Any())
        {
            throw new SendMessageException(string.Format(Resources.MessageRouteNotFound, transportMessage.MessageType));
        }

        if (routeUris.Count > 1)
        {
            throw new SendMessageException(string.Format(Resources.MessageRoutedToMoreThanOneEndpoint, transportMessage.MessageType, string.Join(",", routeUris.ToArray())));
        }

        transportMessage.RecipientInboxWorkTransportUri = routeUris.ElementAt(0);
    }
}