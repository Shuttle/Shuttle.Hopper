using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class MessageContext : IMessageContext
{
    public Guid Id = Guid.NewGuid();

    public ExceptionHandling ExceptionHandling { get; set; }

    public TransportMessage TransportMessage
    {
        get => Guard.AgainstNull(field, nameof(TransportMessage));
        set
        {
            if (field != null)
            {
                throw new InvalidOperationException(Resources.MessageContextTransportMessageException);
            }
            field = value;
        }
    }
}