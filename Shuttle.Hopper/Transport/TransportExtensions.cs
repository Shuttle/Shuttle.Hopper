namespace Shuttle.Hopper;

public static class TransportExtensions
{
    extension(ITransport transport)
    {
        public async Task CreateAsync(CancellationToken cancellationToken = default)
        {
            if (transport is not ICreateTransport operation)
            {
                throw new InvalidOperationException(string.Format(Resources.NotImplementedOnTransport, transport.GetType().FullName, "ICreateTransport"));
            }

            await operation.CreateAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task DropAsync(CancellationToken cancellationToken = default)
        {
            if (transport is not IDropTransport operation)
            {
                throw new InvalidOperationException(string.Format(Resources.NotImplementedOnTransport, transport.GetType().FullName, "IDropTransport"));
            }

            await operation.DropAsync(cancellationToken);
        }

        public async Task PurgeAsync(CancellationToken cancellationToken = default)
        {
            if (transport is not IPurgeTransport operation)
            {
                throw new InvalidOperationException(string.Format(Resources.NotImplementedOnTransport, transport.GetType().FullName, "IPurgeTransport"));
            }

            await operation.PurgeAsync(cancellationToken);
        }

        public async ValueTask<bool> TryCreateAsync(CancellationToken cancellationToken = default)
        {
            if (transport is not ICreateTransport operation)
            {
                return false;
            }

            await operation.CreateAsync(cancellationToken).ConfigureAwait(false);

            return true;
        }

        public async ValueTask<bool> TryDropAsync(CancellationToken cancellationToken = default)
        {
            if (transport is not IDropTransport operation)
            {
                return false;
            }

            await operation.DropAsync(cancellationToken).ConfigureAwait(false);

            return true;
        }

        public async ValueTask<bool> TryPurgeAsync(CancellationToken cancellationToken = default)
        {
            if (transport is not IPurgeTransport operation)
            {
                return false;
            }

            await operation.PurgeAsync(cancellationToken);

            return true;
        }
    }
}