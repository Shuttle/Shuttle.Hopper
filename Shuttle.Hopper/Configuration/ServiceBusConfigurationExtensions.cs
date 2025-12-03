using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public static class ServiceBusConfigurationExtensions
{
    extension(IServiceBusConfiguration serviceBusConfiguration)
    {
        public async Task CreatePhysicalTransportsAsync()
        {
            if (serviceBusConfiguration.HasInbox())
            {
                await serviceBusConfiguration.Inbox!.TryCreateAsync().ConfigureAwait(false);

                if (serviceBusConfiguration.Inbox!.HasDeferredTransport())
                {
                    await serviceBusConfiguration.Inbox!.DeferredTransport!.TryCreateAsync().ConfigureAwait(false);
                }
            }

            if (serviceBusConfiguration.HasOutbox())
            {
                await serviceBusConfiguration.Outbox!.TryCreateAsync().ConfigureAwait(false);
            }
        }

        public bool HasInbox()
        {
            return Guard.AgainstNull(serviceBusConfiguration).Inbox != null;
        }

        public bool HasOutbox()
        {
            return Guard.AgainstNull(serviceBusConfiguration).Outbox != null;
        }
    }
}