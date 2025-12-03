using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public sealed class MessageRouteProvider : IMessageRouteProvider
{
    private readonly IMessageRouteCollection _messageRoutes = new MessageRouteCollection();

    public MessageRouteProvider(IOptions<ServiceBusOptions> serviceBusOptions)
    {
        var specificationFactory = new MessageRouteSpecificationFactory();

        foreach (var messageRouteOptions in Guard.AgainstNull(Guard.AgainstNull(serviceBusOptions).Value).MessageRoutes)
        {
            var messageRoute = _messageRoutes.FindByUri(messageRouteOptions.Uri);

            if (messageRoute == null)
            {
                messageRoute = new MessageRoute(new(messageRouteOptions.Uri));

                _messageRoutes.Add(messageRoute);
            }

            foreach (var specification in messageRouteOptions.Specifications)
            {
                messageRoute.AddSpecification(specificationFactory.Create(specification.Name, specification.Value));
            }
        }
    }

    public async Task<IEnumerable<string>> GetRouteUrisAsync(string messageType, CancellationToken cancellationToken = default)
    {
        var uri = _messageRoutes.FindByMessageType(Guard.AgainstEmpty(messageType)).Select(messageRoute => messageRoute.Uri.ToString()).FirstOrDefault();

        return await Task.FromResult<IEnumerable<string>>(string.IsNullOrEmpty(uri) ? [] : [uri]);
    }

    public async Task AddAsync(IMessageRoute messageRoute, CancellationToken cancellationToken = default)
    {
        var existing = _messageRoutes.FindByUri(Guard.AgainstNull(messageRoute).Uri);

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

        await Task.CompletedTask;
    }

    public IEnumerable<IMessageRoute> MessageRoutes => new List<IMessageRoute>(_messageRoutes).AsReadOnly();
}