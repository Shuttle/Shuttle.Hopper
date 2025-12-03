using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public static class MessageRouteCollectionExtensions
{
    extension(IMessageRouteCollection messageRouteCollection)
    {
        public IMessageRoute? FindByUri(Uri uri)
        {
            return messageRouteCollection.FindByUri(Guard.AgainstNull(uri).ToString());
        }

        public IMessageRoute? FindByUri(ITransport transport)
        {
            return messageRouteCollection.FindByUri(Guard.AgainstNull(transport).Uri.ToString());
        }
    }
}