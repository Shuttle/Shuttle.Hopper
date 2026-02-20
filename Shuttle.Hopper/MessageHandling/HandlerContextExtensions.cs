using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public static class HandlerContextExtensions
{
    public static async Task<TransportMessage> SendAsync(this IHandlerContext context, object message, CancellationToken cancellationToken1 = default)
    {
        return await Guard.AgainstNull(context).SendAsync(message, null, cancellationToken1).ConfigureAwait(false);
    }

    public static async Task<IEnumerable<TransportMessage>> PublishAsync(this IHandlerContext context, object message, CancellationToken cancellationToken1 = default)
    {
        return await Guard.AgainstNull(context).PublishAsync(message, null, cancellationToken1).ConfigureAwait(false);
    }
}