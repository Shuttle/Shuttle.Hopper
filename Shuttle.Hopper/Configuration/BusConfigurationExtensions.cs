using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public static class BusConfigurationExtensions
{
    extension(IBusConfiguration busConfiguration)
    {
        public async Task CreatePhysicalTransportsAsync(CancellationToken cancellationToken = default)
        {
            if (busConfiguration.HasInbox())
            {
                await busConfiguration.Inbox!.TryCreateAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                if (busConfiguration.Inbox!.HasDeferredTransport())
                {
                    await busConfiguration.Inbox!.DeferredTransport!.TryCreateAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }

            if (busConfiguration.HasOutbox())
            {
                await busConfiguration.Outbox!.TryCreateAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        public bool HasInbox()
        {
            return Guard.AgainstNull(busConfiguration).Inbox != null;
        }

        public bool HasOutbox()
        {
            return Guard.AgainstNull(busConfiguration).Outbox != null;
        }
    }
}