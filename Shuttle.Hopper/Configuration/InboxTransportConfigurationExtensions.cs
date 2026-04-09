using Shuttle.Contract;

namespace Shuttle.Hopper;

public static class InboxTransportConfigurationExtensions
{
    extension(IInboxConfiguration configuration)
    {
        public bool HasDeferredTransport()
        {
            return Guard.AgainstNull(configuration).DeferredTransport != null;
        }
    }
}