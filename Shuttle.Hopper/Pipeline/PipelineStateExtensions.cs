using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public static class PipelineStateExtensions
{
    public static bool GetDeferredMessageReturned(this IState state)
    {
        return state.Get<bool>(StateKeys.DeferredMessageReturned);
    }

    public static ITransport? GetDeferredTransport(this IState state)
    {
        return state.Get<ITransport>(StateKeys.DeferredTransport);
    }

    public static IEnumerable<TimeSpan>? GetDurationToIgnoreOnFailure(this IState state)
    {
        return state.Get<IEnumerable<TimeSpan>>(StateKeys.DurationToIgnoreOnFailure);
    }

    public static ITransport? GetErrorTransport(this IState state)
    {
        return state.Get<ITransport>(StateKeys.ErrorTransport);
    }

    public static object? GetHandlerContext(this IState state)
    {
        return state.Get<object>(StateKeys.HandlerContext);
    }

    public static int? GetMaximumFailureCount(this IState state)
    {
        return state.Get<int>(StateKeys.MaximumFailureCount);
    }

    public static object? GetMessage(this IState state)
    {
        return state.Get<object>(StateKeys.Message);
    }

    public static byte[]? GetMessageBytes(this IState state)
    {
        return state.Get<byte[]>(StateKeys.MessageBytes);
    }

    public static bool GetMessageHandlerInvoked(this IState state)
    {
        return state.Get<bool>(StateKeys.MessageHandlerInvokeResult);
    }

    public static ReceivedMessage? GetReceivedMessage(this IState state)
    {
        return state.Get<ReceivedMessage>(StateKeys.ReceivedMessage);
    }

    public static TransportMessage? GetTransportMessage(this IState state)
    {
        return state.Get<TransportMessage>(StateKeys.TransportMessage);
    }

    public static Action<TransportMessageBuilder>? GetTransportMessageBuilder(this IState state)
    {
        return state.Get<Action<TransportMessageBuilder>>(StateKeys.TransportMessageBuilder);
    }

    public static TransportMessage? GetTransportMessageReceived(this IState state)
    {
        return state.Get<TransportMessage>(StateKeys.TransportMessageReceived);
    }

    public static Stream? GetTransportMessageStream(this IState state)
    {
        return state.Get<Stream>(StateKeys.TransportMessageStream);
    }

    public static bool GetWorkPerformed(this IState state)
    {
        return state.Contains(StateKeys.WorkPerformed) && state.Get<bool>(StateKeys.WorkPerformed);
    }

    public static ITransport? GetWorkTransport(this IState state)
    {
        return state.Get<ITransport>(StateKeys.WorkTransport);
    }

    public static void ResetWorkPerformed(this IState state)
    {
        state.Replace(StateKeys.WorkPerformed, false);
    }

    public static void SetDeferredMessageReturned(this IState state, bool deferredMessageReturned)
    {
        state.Replace(StateKeys.DeferredMessageReturned, deferredMessageReturned);
    }

    public static void SetDeferredTransport(this IState state, ITransport? transport)
    {
        state.Add(StateKeys.DeferredTransport, transport);
    }

    public static void SetDurationToIgnoreOnFailure(this IState state, IEnumerable<TimeSpan> timeSpans)
    {
        state.Add(StateKeys.DurationToIgnoreOnFailure, timeSpans);
    }

    public static void SetErrorTransport(this IState state, ITransport? transport)
    {
        state.Add(StateKeys.ErrorTransport, transport);
    }

    public static void SetHandlerContext(this IState state, object handlerContext)
    {
        state.Replace(StateKeys.HandlerContext, handlerContext);
    }

    public static void SetMaximumFailureCount(this IState state, int count)
    {
        state.Add(StateKeys.MaximumFailureCount, count);
    }

    public static void SetMessage(this IState state, object message)
    {
        state.Replace(StateKeys.Message, message);
    }

    public static void SetMessageBytes(this IState state, byte[] bytes)
    {
        state.Replace(StateKeys.MessageBytes, bytes);
    }

    public static void SetMessageHandlerInvoked(this IState state, bool value)
    {
        state.Replace(StateKeys.MessageHandlerInvokeResult, value);
    }

    public static void SetReceivedMessage(this IState state, ReceivedMessage? receivedMessage)
    {
        state.Replace(StateKeys.ReceivedMessage, receivedMessage);
    }

    public static void SetTransportMessage(this IState state, TransportMessage? value)
    {
        state.Replace(StateKeys.TransportMessage, value);
    }

    public static void SetTransportMessageBuilder(this IState state, Action<TransportMessageBuilder>? builder)
    {
        state.Replace(StateKeys.TransportMessageBuilder, builder);
    }

    public static void SetTransportMessageReceived(this IState state, TransportMessage? value)
    {
        state.Replace(StateKeys.TransportMessageReceived, value);
    }

    public static void SetTransportMessageStream(this IState state, Stream value)
    {
        state.Replace(StateKeys.TransportMessageStream, value);
    }

    public static void SetWorking(this IState state)
    {
        state.Replace(StateKeys.WorkPerformed, true);
    }

    public static void SetWorkTransport(this IState state, ITransport transport)
    {
        state.Add(StateKeys.WorkTransport, transport);
    }
}