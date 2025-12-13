using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public static class ServiceBusConfigurationExtensions
{
    extension(IServiceBusConfiguration serviceBusConfiguration)
    {
        public async Task CreatePhysicalTransportsAsync(CancellationToken cancellationToken = default)
        {
            if (serviceBusConfiguration.HasInbox())
            {
                await serviceBusConfiguration.Inbox!.TryCreateAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                if (serviceBusConfiguration.Inbox!.HasDeferredTransport())
                {
                    await serviceBusConfiguration.Inbox!.DeferredTransport!.TryCreateAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }

            if (serviceBusConfiguration.HasOutbox())
            {
                await serviceBusConfiguration.Outbox!.TryCreateAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
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