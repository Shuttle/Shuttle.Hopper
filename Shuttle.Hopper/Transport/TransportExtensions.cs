namespace Shuttle.Hopper;

public static class TransportExtensions
{
    extension(ITransport transport)
    {
        public async Task CreateAsync()
        {
            if (transport is not ICreateTransport operation)
            {
                throw new InvalidOperationException(string.Format(Resources.NotImplementedOnTransport, transport.GetType().FullName, "ICreateTransport"));
            }

            await operation.CreateAsync().ConfigureAwait(false);
        }

        public async Task DropAsync()
        {
            if (transport is not IDropTransport operation)
            {
                throw new InvalidOperationException(string.Format(Resources.NotImplementedOnTransport, transport.GetType().FullName, "IDropTransport"));
            }

            await operation.DropAsync();
        }

        public async Task PurgeAsync()
        {
            if (transport is not IPurgeTransport operation)
            {
                throw new InvalidOperationException(string.Format(Resources.NotImplementedOnTransport, transport.GetType().FullName, "IPurgeTransport"));
            }

            await operation.PurgeAsync();
        }

        public async ValueTask<bool> TryCreateAsync()
        {
            if (transport is not ICreateTransport operation)
            {
                return false;
            }

            await operation.CreateAsync().ConfigureAwait(false);

            return true;
        }

        public async ValueTask<bool> TryDropAsync()
        {
            if (transport is not IDropTransport operation)
            {
                return false;
            }

            await operation.DropAsync().ConfigureAwait(false);

            return true;
        }

        public async ValueTask<bool> TryPurgeAsync()
        {
            if (transport is not IPurgeTransport operation)
            {
                return false;
            }

            await operation.PurgeAsync();

            return true;
        }
    }
}