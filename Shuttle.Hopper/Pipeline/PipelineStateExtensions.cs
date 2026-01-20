using Shuttle.Core.Pipelines;

namespace Shuttle.Hopper;

public static class PipelineStateExtensions
{
    extension(IState state)
    {
        public bool GetDeferredMessageReturned()
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

        public object? GetHandlerContext()
        {
            return state.Get<object>(StateKeys.HandlerContext);
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

        public bool GetWorkPerformed()
        {
            return state.Contains(StateKeys.WorkPerformed) && state.Get<bool>(StateKeys.WorkPerformed);
        }

        public ITransport? GetWorkTransport()
        {
            return state.Get<ITransport>(StateKeys.WorkTransport);
        }

        public void ResetWorkPerformed()
        {
            state.Replace(StateKeys.WorkPerformed, false);
        }

        public void SetDeferredMessageReturned(bool deferredMessageReturned)
        {
            state.Replace(StateKeys.DeferredMessageReturned, deferredMessageReturned);
        }

        public void SetDeferredTransport(ITransport? transport)
        {
            state.Add(StateKeys.DeferredTransport, transport);
        }

        public void SetDurationToIgnoreOnFailure(IEnumerable<TimeSpan> timeSpans)
        {
            state.Add(StateKeys.DurationToIgnoreOnFailure, timeSpans);
        }

        public void SetErrorTransport(ITransport? transport)
        {
            state.Add(StateKeys.ErrorTransport, transport);
        }

        public void SetHandlerContext(object handlerContext)
        {
            state.Replace(StateKeys.HandlerContext, handlerContext);
        }

        public void SetMaximumFailureCount(int count)
        {
            state.Add(StateKeys.MaximumFailureCount, count);
        }

        public void SetMessage(object message)
        {
            state.Replace(StateKeys.Message, message);
        }

        public void SetMessageBytes(byte[] bytes)
        {
            state.Replace(StateKeys.MessageBytes, bytes);
        }

        public void SetMessageHandlerInvoked(bool value)
        {
            state.Replace(StateKeys.MessageHandlerInvokeResult, value);
        }

        public void SetReceivedMessage(ReceivedMessage? receivedMessage)
        {
            state.Replace(StateKeys.ReceivedMessage, receivedMessage);
        }

        public void SetTransportMessage(TransportMessage? value)
        {
            state.Replace(StateKeys.TransportMessage, value);
        }

        public void SetTransportMessageBuilder(Action<TransportMessageBuilder>? builder)
        {
            state.Replace(StateKeys.TransportMessageBuilder, builder);
        }

        public void SetTransportMessageReceived(TransportMessage? value)
        {
            state.Replace(StateKeys.TransportMessageReceived, value);
        }

        public void SetTransportMessageStream(Stream value)
        {
            state.Replace(StateKeys.TransportMessageStream, value);
        }

        public void SetWorking()
        {
            state.Replace(StateKeys.WorkPerformed, true);
        }

        public void SetWorkTransport(ITransport transport)
        {
            state.Add(StateKeys.WorkTransport, transport);
        }
    }
}