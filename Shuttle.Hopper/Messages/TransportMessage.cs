namespace Shuttle.Hopper;

[Serializable]
public class TransportMessage
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
    public string RecipientInboxWorkTransportUri { get; set; } = string.Empty;
    public DateTime SendDate { get; set; } = DateTime.UtcNow;
    public string SenderInboxWorkTransportUri { get; set; } = string.Empty;
}