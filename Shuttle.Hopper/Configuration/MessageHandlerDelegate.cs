using Microsoft.Extensions.DependencyInjection;

namespace Shuttle.Hopper;

public class MessageHandlerDelegate(Delegate handler, IEnumerable<Type> parameterTypes)
{
    private static readonly Type CancellationTokenType = typeof(CancellationToken);

    public Delegate Handler { get; } = handler;

    public object[] GetParameters(IServiceProvider serviceProvider, object message, CancellationToken cancellationToken)
    {
        return parameterTypes
            .Select((type, index) =>
                index == 0
                    ? message
                    : type == CancellationTokenType
                        ? cancellationToken
                        : serviceProvider.GetRequiredService(type))
            .ToArray();
    }
}