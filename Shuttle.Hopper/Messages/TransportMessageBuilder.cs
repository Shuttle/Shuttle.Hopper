using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class TransportMessageBuilder(TransportMessage transportMessage)
{
    private readonly TransportMessage _transportMessage = Guard.AgainstNull(transportMessage);

    public bool HasRecipient => !string.IsNullOrWhiteSpace(_transportMessage.RecipientInboxWorkTransportUri);

    public List<TransportHeader> Headers => _transportMessage.Headers;
    public bool ShouldReply { get; private set; }

    public bool ShouldSendLocal { get; private set; }

    public TransportMessageBuilder Defer(DateTime ignoreTillDate)
    {
        _transportMessage.IgnoreTillDate = ignoreTillDate;

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

    public TransportMessageBuilder WillExpire(DateTime expiryDate)
    {
        _transportMessage.ExpiryDate = expiryDate;

        return this;
    }

    public TransportMessageBuilder WillExpire(TimeSpan fromUtcNow)
    {
        _transportMessage.ExpiryDate = DateTime.UtcNow.Add(fromUtcNow);

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