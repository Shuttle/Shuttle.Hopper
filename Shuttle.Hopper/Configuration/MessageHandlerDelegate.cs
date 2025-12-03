using Microsoft.Extensions.DependencyInjection;
using Shuttle.Core.Reflection;

namespace Shuttle.Hopper;

public class MessageHandlerDelegate(Delegate handler, IEnumerable<Type> parameterTypes)
{
    private static readonly Type HandlerContextType = typeof(IHandlerContext);

    public Delegate Handler { get; } = handler;
    public bool HasParameters { get; } = parameterTypes.Any();

    public object[] GetParameters(IServiceProvider serviceProvider, object handlerContext)
    {
        return parameterTypes
            .Select(parameterType => !parameterType.IsCastableTo(HandlerContextType)
                ? serviceProvider.GetRequiredService(parameterType)
                : handlerContext
            ).ToArray();
    }
}