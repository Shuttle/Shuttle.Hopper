namespace Shuttle.Hopper.Tests;

public class AdditionalInboxCommand
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool Fail { get; set; }
}
