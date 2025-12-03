using System.Reflection;
using System.Reflection.Emit;

namespace Shuttle.Hopper;

internal class ProcessMessageMethodInvoker
{
    private static readonly Type HandlerContextType = typeof(HandlerContext<>);

    private readonly InvokeHandler _invoker;

    public ProcessMessageMethodInvoker(MethodInfo methodInfo)
    {
        var dynamicMethod = new DynamicMethod(string.Empty, typeof(Task), [typeof(object), typeof(object), typeof(CancellationToken)], HandlerContextType.Module);

        var il = dynamicMethod.GetILGenerator();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_2);

        il.EmitCall(OpCodes.Callvirt, methodInfo, null);
        il.Emit(OpCodes.Ret);

        _invoker = (InvokeHandler)dynamicMethod.CreateDelegate(typeof(InvokeHandler));
    }

    public async Task InvokeAsync(object handler, object handlerContext, CancellationToken cancellationToken)
    {
        await _invoker.Invoke(handler, handlerContext, cancellationToken).ConfigureAwait(false);
    }

    private delegate Task InvokeHandler(object handler, object handlerContext, CancellationToken cancellationToken);
}