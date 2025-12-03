using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public static class ServiceBusExtensions
{
    extension(IServiceBus serviceBus)
    {
        public async Task CreatePhysicalTransportsAsync()
        {
            if (serviceBus.HasInbox())
            {
                await serviceBus.Inbox!.TryCreateAsync().ConfigureAwait(false);

                if (serviceBus.Inbox!.HasDeferredTransport())
                {
                    await serviceBus.Inbox!.DeferredTransport!.TryCreateAsync().ConfigureAwait(false);
                }
            }

            if (serviceBus.HasOutbox())
            {
                await serviceBus.Outbox!.TryCreateAsync().ConfigureAwait(false);
            }
        }

        public bool HasInbox()
        {
            return Guard.AgainstNull(serviceBus).Inbox != null;
        }

        public bool HasOutbox()
        {
            return Guard.AgainstNull(serviceBus).Outbox != null;
        }
    }
}