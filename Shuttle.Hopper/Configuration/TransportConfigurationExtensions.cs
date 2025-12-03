namespace Shuttle.Hopper;

public static class TransportConfigurationExtensions
{
    extension(IWorkTransportConfiguration workTransportConfiguration)
    {
        public async Task TryCreateAsync()
        {
            if (workTransportConfiguration.WorkTransport != null)
            {
                await workTransportConfiguration.WorkTransport.TryCreateAsync().ConfigureAwait(false);
            }

            if (workTransportConfiguration is IErrorTransportConfiguration { ErrorTransport: not null } errorTransportConfiguration)
            {
                await errorTransportConfiguration.ErrorTransport.TryCreateAsync().ConfigureAwait(false);
            }
        }
    }
}