using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class TransportMessageBuilder(TransportMessage transportMessage)
{
    private readonly TransportMessage _transportMessage = Guard.AgainstNull(transportMessage);

    public bool HasRecipient => !string.IsNullOrWhiteSpace(_transportMessage.RecipientInboxWorkTransportUri);

    public List<TransportHeader> Headers => _transportMessage.Headers;
    public bool ShouldReply { get; private set; }

    public bool ShouldSendLocal { get; private set; }

    public TransportMessageBuilder Defer(DateTimeOffset ignoreTillDateTime)
    {
        _transportMessage.IgnoreTillDateTime = ignoreTillDateTime;

        return this;
    }

    public TransportMessageBuilder Defer(TimeSpan ignoreTimeSpan)
    {
        _transportMessage.IgnoreTillDateTime = DateTimeOffset.UtcNow.Add(ignoreTimeSpan);

        return this;
    }

    private void GuardRecipient()
    {
        if (!HasRecipient && !ShouldSendLocal && !ShouldReply)
        {
            return;
        }

        throw new InvalidOperationException(Resources.TransportMessageRecipientException);
    }

    public TransportMessageBuilder Local()
    {
        GuardRecipient();

        ShouldSendLocal = true;

        return this;
    }

    public TransportMessageBuilder Reply()
    {
        GuardRecipient();

        ShouldReply = true;

        return this;
    }

    public TransportMessageBuilder WillExpire(DateTimeOffset expiryDateTime)
    {
        _transportMessage.ExpiryDateTime = expiryDateTime;

        return this;
    }

    public TransportMessageBuilder WillExpire(TimeSpan expiryTimeSpan)
    {
        _transportMessage.ExpiryDateTime = DateTimeOffset.UtcNow.Add(expiryTimeSpan);

        return this;
    }

    public TransportMessageBuilder WithCompression(string compression)
    {
        _transportMessage.CompressionAlgorithm = compression;

        return this;
    }

    public TransportMessageBuilder WithCorrelationId(string correlationId)
    {
        _transportMessage.CorrelationId = correlationId;

        return this;
    }

    public TransportMessageBuilder WithEncryption(string encryption)
    {
        _transportMessage.EncryptionAlgorithm = encryption;

        return this;
    }

    public TransportMessageBuilder WithPriority(int priority)
    {
        _transportMessage.Priority = priority;

        return this;
    }

    public TransportMessageBuilder WithRecipient(ITransport transport)
    {
        return WithRecipient(transport.Uri.ToString());
    }

    public TransportMessageBuilder WithRecipient(Uri uri)
    {
        return WithRecipient(uri.ToString());
    }

    public TransportMessageBuilder WithRecipient(string uri)
    {
        GuardRecipient();

        _transportMessage.RecipientInboxWorkTransportUri = uri;

        return this;
    }

    public TransportMessageBuilder WithSender(ITransport transport)
    {
        return WithSender(transport.Uri.ToString());
    }

    public TransportMessageBuilder WithSender(Uri uri)
    {
        return WithSender(uri.ToString());
    }

    public TransportMessageBuilder WithSender(string uri)
    {
        _transportMessage.SenderInboxWorkTransportUri = uri;

        return this;
    }
}