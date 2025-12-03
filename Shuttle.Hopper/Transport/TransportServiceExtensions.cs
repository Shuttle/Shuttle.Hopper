using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public static class TransportServiceExtensions
{
    extension(ITransportService transportService)
    {
        public async ValueTask<bool> ContainsAsync(string uri, CancellationToken cancellationToken = default)
        {
            try
            {
                return await transportService.ContainsAsync(new(Guard.AgainstEmpty(uri)), cancellationToken);
            }
            catch (UriFormatException ex)
            {
                throw new UriFormatException($"{ex.Message} / uri = '{uri}'");
            }
        }

        public async Task<ITransport> GetAsync(string uri, CancellationToken cancellationToken = default)
        {
            try
            {
                return await transportService.GetAsync(new(Guard.AgainstEmpty(uri)), cancellationToken);
            }
            catch (UriFormatException ex)
            {
                throw new UriFormatException($"{ex.Message} / uri = '{uri}'");
            }
        }
    }
}