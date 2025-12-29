namespace Shuttle.Hopper;

[Serializable]
public class TransportMessage
{
    public string AssemblyQualifiedName { get; set; } = string.Empty;
    public string CompressionAlgorithm { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string EncryptionAlgorithm { get; set; } = string.Empty;
    public DateTimeOffset ExpiryDateTime { get; set; } = DateTimeOffset.MaxValue;
    public List<string> FailureMessages { get; set; } = [];
    public List<TransportHeader> Headers { get; set; } = [];
    public DateTimeOffset IgnoreTillDateTime { get; set; } = DateTimeOffset.MinValue;
    public byte[] Message { get; set; } = null!;
    public Guid MessageId { get; set; } = Guid.NewGuid();

    public Guid MessageReceivedId { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public string PrincipalIdentityName { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string RecipientInboxWorkTransportUri { get; set; } = string.Empty;
    public DateTimeOffset SendDateTime { get; set; } = DateTimeOffset.UtcNow;
    public string SenderInboxWorkTransportUri { get; set; } = string.Empty;
}