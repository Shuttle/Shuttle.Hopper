using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Serialization;
using Shuttle.Core.Streams;
using Shuttle.Core.System;

namespace Shuttle.Hopper;

public interface IDeserializeTransportMessageObserver : IPipelineObserver<DeserializeTransportMessage>;

public class DeserializeTransportMessageObserver(IOptions<HopperOptions> serviceBusOptions, ISerializer serializer, IEnvironmentService environmentService, IProcessService processService)
    : IDeserializeTransportMessageObserver
{
    private class TransportMessageObsolete
    {
        public string AssemblyQualifiedName { get; set; } = string.Empty;
        public string CompressionAlgorithm { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public string EncryptionAlgorithm { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; } = DateTime.MaxValue;
        public List<string> FailureMessages { get; set; } = [];
        public List<TransportHeader> Headers { get; set; } = [];
        public DateTime IgnoreTillDate { get; set; } = DateTime.MinValue;
        public byte[] Message { get; set; } = null!;
        public Guid MessageId { get; set; } = Guid.NewGuid();

        public Guid MessageReceivedId { get; set; }
        public string MessageType { get; set; } = string.Empty;
        public string PrincipalIdentityName { get; set; } = string.Empty;
        public int Priority { get; set; }
        public string RecipientInboxWorkQueueUri { get; set; } = string.Empty;
        public DateTime SendDate { get; set; } = DateTime.UtcNow;
        public string SenderInboxWorkQueueUri { get; set; } = string.Empty;
    }

    private readonly IEnvironmentService _environmentService = Guard.AgainstNull(environmentService);
    private readonly IProcessService _processService = Guard.AgainstNull(processService);
    private readonly ISerializer _serializer = Guard.AgainstNull(serializer);
    private readonly HopperOptions _hopperOptions = Guard.AgainstNull(Guard.AgainstNull(serviceBusOptions).Value);

    public async Task ExecuteAsync(IPipelineContext<DeserializeTransportMessage> pipelineContext, CancellationToken cancellationToken = default)
    {
        var state = Guard.AgainstNull(pipelineContext).Pipeline.State;
        var receivedMessage = Guard.AgainstNull(state.GetReceivedMessage());
        var workTransport = Guard.AgainstNull(state.GetWorkTransport());

        TransportMessage transportMessage;

        try
        {
            await using var stream = await receivedMessage.Stream.CopyAsync().ConfigureAwait(false);

            try
            {
                transportMessage = (TransportMessage)await _serializer.DeserializeAsync(typeof(TransportMessage), stream, cancellationToken).ConfigureAwait(false);
            }
            catch 
            {
                stream.Position = 0;

                var obsolete = (TransportMessageObsolete)await _serializer.DeserializeAsync(typeof(TransportMessageObsolete), stream, cancellationToken).ConfigureAwait(false);

                transportMessage = new()
                {
                    AssemblyQualifiedName = obsolete.AssemblyQualifiedName,
                    CompressionAlgorithm = obsolete.CompressionAlgorithm,
                    CorrelationId = obsolete.CorrelationId,
                    EncryptionAlgorithm = obsolete.EncryptionAlgorithm,
                    ExpiresAt = new(obsolete.ExpiryDate),
                    FailureMessages = obsolete.FailureMessages,
                    Headers = obsolete.Headers,
                    IgnoreUntil = new(obsolete.IgnoreTillDate),
                    Message = obsolete.Message,
                    MessageId = obsolete.MessageId,
                    MessageReceivedId = obsolete.MessageReceivedId,
                    MessageType = obsolete.MessageType,
                    PrincipalIdentityName = obsolete.PrincipalIdentityName,
                    Priority = obsolete.Priority,
                    RecipientInboxWorkTransportUri = obsolete.RecipientInboxWorkQueueUri,
                    SentAt = new(obsolete.SendDate),
                    SenderInboxWorkTransportUri = obsolete.SenderInboxWorkQueueUri
                };
            }
        }
        catch (Exception ex)
        {
            await _hopperOptions.TransportMessageDeserializationException.InvokeAsync(new(pipelineContext, workTransport, Guard.AgainstNull(state.GetErrorTransport()), ex), cancellationToken);

            if (_hopperOptions.RemoveCorruptMessages)
            {
                await workTransport.AcknowledgeAsync(Guard.AgainstNull(state.GetReceivedMessage()).AcknowledgementToken, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                if (!_environmentService.UserInteractive)
                {
                    _processService.GetCurrentProcess().Kill();
                }

                return;
            }

            pipelineContext.Pipeline.Abort();

            return;
        }

        state.SetTransportMessage(transportMessage);
        state.SetMessageBytes(transportMessage.Message);

        transportMessage.AcceptInvariants();
    }
}