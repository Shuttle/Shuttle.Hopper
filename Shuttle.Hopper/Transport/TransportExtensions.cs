namespace Shuttle.Hopper;

public static class TransportExtensions
{
    extension(ITransport transport)
    {
        public async Task CreateAsync(CancellationToken cancellationToken = default)
        {
            if (transport is not ICreateTransport operation)
            {
                throw new InvalidOperationException(string.Format(Resources.NotImplementedOnTransport, transport.GetType().FullName, nameof(ICreateTransport)));
            }

            await operation.CreateAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteAsync(CancellationToken cancellationToken = default)
        {
            if (transport is not IDeleteTransport operation)
            {
                throw new InvalidOperationException(string.Format(Resources.NotImplementedOnTransport, transport.GetType().FullName, nameof(IDeleteTransport)));
            }

            await operation.DeleteAsync(cancellationToken);
        }

        public async Task PurgeAsync(CancellationToken cancellationToken = default)
        {
            if (transport is not IPurgeTransport operation)
            {
                throw new InvalidOperationException(string.Format(Resources.NotImplementedOnTransport, transport.GetType().FullName, nameof(IPurgeTransport)));
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

        public async ValueTask<bool> TryDeleteAsync(CancellationToken cancellationToken = default)
        {
            if (transport is not IDeleteTransport operation)
            {
                return false;
            }

            await operation.DeleteAsync(cancellationToken).ConfigureAwait(false);

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