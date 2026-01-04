using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class TransportMessageBuilder(TransportMessage transportMessage)
{
    private readonly TransportMessage _transportMessage = Guard.AgainstNull(transportMessage);

    public bool HasRecipient => !string.IsNullOrWhiteSpace(_transportMessage.RecipientInboxWorkTransportUri);

    public List<TransportHeader> Headers => _transportMessage.Headers;
    public bool ShouldReply { get; private set; }

    public bool ShouldSendToSelf { get; private set; }

    public TransportMessageBuilder DeferUntil(DateTimeOffset ignoreUntil)
    {
        _transportMessage.IgnoreUntil = ignoreUntil;

        return this;
    }

    public TransportMessageBuilder DeferFor(TimeSpan delay)
    {
        _transportMessage.IgnoreUntil = DateTimeOffset.UtcNow.Add(delay);

        return this;
    }

    private void GuardAgainstMultipleRecipients()
    {
        if (!HasRecipient && !ShouldSendToSelf && !ShouldReply)
        {
            return;
        }

        throw new InvalidOperationException(Resources.TransportMessageRecipientException);
    }

    public TransportMessageBuilder ToSelf()
    {
        GuardAgainstMultipleRecipients();

        ShouldSendToSelf = true;

        return this;
    }

    public TransportMessageBuilder AsReply()
    {
        GuardAgainstMultipleRecipients();

        ShouldReply = true;

        return this;
    }

    public TransportMessageBuilder ExpiresAt(DateTimeOffset expiresAt)
    {
        _transportMessage.ExpiresAt = expiresAt;

        return this;
    }

    public TransportMessageBuilder ExpiresIn(TimeSpan after)
    {
        _transportMessage.ExpiresAt = DateTimeOffset.UtcNow.Add(after);

        return this;
    }

    public TransportMessageBuilder WithCompression(string algorithm)
    {
        _transportMessage.CompressionAlgorithm = algorithm;

        return this;
    }

    public TransportMessageBuilder WithCorrelationId(string correlationId)
    {
        _transportMessage.CorrelationId = correlationId;

        return this;
    }

    public TransportMessageBuilder WithEncryption(string algorithm)
    {
        _transportMessage.EncryptionAlgorithm = algorithm;

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
        GuardAgainstMultipleRecipients();
        
        _transportMessage.RecipientInboxWorkTransportUri = Guard.AgainstEmpty(uri);

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