using Shuttle.Pipelines;

namespace Shuttle.Hopper;

public static class PipelineStateExtensions
{
    extension(IState state)
    {
        public bool HasDeferredMessageReturned()
        {
            return state.Get<bool>(StateKeys.DeferredMessageReturned);
        }

        public ITransport? GetDeferredTransport()
        {
            return state.Get<ITransport>(StateKeys.DeferredTransport);
        }

        public IEnumerable<TimeSpan>? GetDurationToIgnoreOnFailure()
        {
            return state.Get<IEnumerable<TimeSpan>>(StateKeys.DurationToIgnoreOnFailure);
        }

        public ITransport? GetErrorTransport()
        {
            return state.Get<ITransport>(StateKeys.ErrorTransport);
        }

        public int? GetMaximumFailureCount()
        {
            return state.Get<int>(StateKeys.MaximumFailureCount);
        }

        public object? GetMessage()
        {
            return state.Get<object>(StateKeys.Message);
        }

        public byte[]? GetMessageBytes()
        {
            return state.Get<byte[]>(StateKeys.MessageBytes);
        }

        public bool GetMessageHandlerInvoked()
        {
            return state.Get<bool>(StateKeys.MessageHandlerInvokeResult);
        }

        public ReceivedMessage? GetReceivedMessage()
        {
            return state.Get<ReceivedMessage>(StateKeys.ReceivedMessage);
        }

        public TransportMessage? GetTransportMessage()
        {
            return state.Get<TransportMessage>(StateKeys.TransportMessage);
        }

        public Action<TransportMessageBuilder>? GetTransportMessageBuilder()
        {
            return state.Get<Action<TransportMessageBuilder>>(StateKeys.TransportMessageBuilder);
        }

        public TransportMessage? GetTransportMessageReceived()
        {
            return state.Get<TransportMessage>(StateKeys.TransportMessageReceived);
        }

        public Stream? GetTransportMessageStream()
        {
            return state.Get<Stream>(StateKeys.TransportMessageStream);
        }

        public bool HasReceivedMessage()
        {
            return state.Contains(StateKeys.ReceivedMessage);
        }

        public ITransport? GetWorkTransport()
        {
            return state.Get<ITransport>(StateKeys.WorkTransport);
        }

        public IState ResetDeferredMessageReturned()
        {
            state.Replace(StateKeys.DeferredMessageReturned, false);
            return state;
        }

        public IState DeferredMessageReturned()
        {
            state.Replace(StateKeys.DeferredMessageReturned, true);
            return state;
        }

        public IState SetDeferredTransport(ITransport? transport)
        {
            state.Add(StateKeys.DeferredTransport, transport);
            return state;
        }

        public IState SetDurationToIgnoreOnFailure(IEnumerable<TimeSpan> timeSpans)
        {
            state.Add(StateKeys.DurationToIgnoreOnFailure, timeSpans);
            return state;
        }

        public IState SetErrorTransport(ITransport? transport)
        {
            state.Add(StateKeys.ErrorTransport, transport);
            return state;
        }

        public IState SetHandlerContext(object handlerContext)
        {
            state.Replace(StateKeys.HandlerContext, handlerContext);
            return state;
        }

        public IState SetMaximumFailureCount(int count)
        {
            state.Add(StateKeys.MaximumFailureCount, count);
            return state;
        }

        public IState SetMessage(object message)
        {
            state.Replace(StateKeys.Message, message);
            return state;
        }

        public IState SetMessageBytes(byte[] bytes)
        {
            state.Replace(StateKeys.MessageBytes, bytes);
            return state;
        }

        public IState SetMessageHandlerInvoked(bool value)
        {
            state.Replace(StateKeys.MessageHandlerInvokeResult, value);
            return state;
        }

        public IState ResetReceivedMessage()
        {
            state.Remove(StateKeys.ReceivedMessage);
            return state;
        }

        public IState SetReceivedMessage(ReceivedMessage receivedMessage)
        {
            state.Replace(StateKeys.ReceivedMessage, receivedMessage);
            return state;
        }

        public IState SetTransportMessage(TransportMessage? value)
        {
            state.Replace(StateKeys.TransportMessage, value);
            return state;
        }

        public IState SetTransportMessageBuilder(Action<TransportMessageBuilder>? builder)
        {
            state.Replace(StateKeys.TransportMessageBuilder, builder);
            return state;
        }

        public IState SetTransportMessageReceived(TransportMessage? value)
        {
            state.Replace(StateKeys.TransportMessageReceived, value);
            return state;
        }

        public IState SetTransportMessageStream(Stream value)
        {
            state.Replace(StateKeys.TransportMessageStream, value);
            return state;
        }

        public IState SetWorkTransport(ITransport transport)
        {
            state.Add(StateKeys.WorkTransport, transport);
            return state;
        }
    }
}