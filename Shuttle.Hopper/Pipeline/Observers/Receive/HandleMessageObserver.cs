using System.Reflection;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Reflection;
using Shuttle.Core.Serialization;

namespace Shuttle.Hopper;

public interface IHandleMessageObserver : IPipelineObserver<HandleMessage>;

public class HandleMessageObserver(IOptions<ServiceBusOptions> serviceBusOptions, IMessageHandlerInvoker messageHandlerInvoker, ISerializer serializer)
    : IHandleMessageObserver
{
    private readonly IMessageHandlerInvoker _messageHandlerInvoker = Guard.AgainstNull(messageHandlerInvoker);
    private readonly ISerializer _serializer = Guard.AgainstNull(serializer);
    private readonly ServiceBusOptions _serviceBusOptions = Guard.AgainstNull(Guard.AgainstNull(serviceBusOptions).Value);

    public async Task ExecuteAsync(IPipelineContext<HandleMessage> pipelineContext, CancellationToken cancellation = default)
    {
        var state = Guard.AgainstNull(pipelineContext).Pipeline.State;
        var transportMessage = Guard.AgainstNull(state.GetTransportMessage());

        if (transportMessage.HasExpired())
        {
            return;
        }

        var message = Guard.AgainstNull(state.GetMessage());
        var errorTransport = state.GetErrorTransport();

        try
        {
            var messageHandlerInvoked = await _messageHandlerInvoker.InvokeAsync(pipelineContext, cancellation).ConfigureAwait(false);

            state.SetMessageHandlerInvoked(messageHandlerInvoked);

            if (messageHandlerInvoked)
            {
                return;
            }

            var workTransport = state.GetWorkTransport();

            if (workTransport == null)
            {
                throw new InvalidOperationException(string.Format(Resources.MessageNotHandledMissingWorkTransportFailure, message.GetType().FullName, transportMessage.MessageId));
            }

            await _serviceBusOptions.MessageNotHandled.InvokeAsync(new(workTransport, pipelineContext, transportMessage, message), cancellation);

            if (_serviceBusOptions.RemoveMessagesNotHandled)
            {
                return;
            }

            if (errorTransport == null)
            {
                throw new InvalidOperationException(string.Format(Resources.MessageNotHandledMissingErrorTransportFailure, message.GetType().FullName, transportMessage.MessageId));
            }

            transportMessage.RegisterFailure(string.Format(Resources.MessageNotHandledFailure, message.GetType().FullName, transportMessage.MessageId, errorTransport.Uri));

            await using var stream = await _serializer.SerializeAsync(transportMessage, cancellation).ConfigureAwait(false);

            await errorTransport.SendAsync(transportMessage, stream, cancellation).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var exception = ex.TrimLeading<TargetInvocationException>();

            await _serviceBusOptions.HandlerException.InvokeAsync(new(pipelineContext, transportMessage, message, exception), cancellation);

            throw exception;
        }
    }
}