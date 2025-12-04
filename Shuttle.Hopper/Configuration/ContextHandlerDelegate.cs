using Microsoft.Extensions.DependencyInjection;
using Shuttle.Core.Reflection;

namespace Shuttle.Hopper;

public class ContextHandlerDelegate(Delegate handler, IEnumerable<Type> parameterTypes)
{
    private static readonly Type CancellationTokenType = typeof(CancellationToken);
    private static readonly Type HandlerContextType = typeof(IHandlerContext);

    public Delegate Handler { get; } = handler;

    public object[] GetParameters(IServiceProvider serviceProvider, object handlerContext, CancellationToken cancellationToken)
    {
        return parameterTypes
            .Select(parameterType =>
                parameterType == CancellationTokenType
                    ? cancellationToken
                    : !parameterType.IsCastableTo(HandlerContextType)
                        ? serviceProvider.GetRequiredService(parameterType)
                        : handlerContext
            ).ToArray();
    }
}