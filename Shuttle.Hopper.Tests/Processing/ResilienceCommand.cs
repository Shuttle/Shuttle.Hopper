namespace Shuttle.Hopper.Tests;

public class ResilienceCommand
{
    public ResilienceCommand()
    {
    }

    public ResilienceCommand(Guid id, string behaviour)
    {
        Id = id;
        Behaviour = behaviour;
    }

    /// <summary>
    ///     One of the `ResilienceBehaviour` constants.
    /// </summary>
    public string Behaviour { get; set; } = ResilienceBehaviour.Succeed;

    public Guid Id { get; set; } = Guid.NewGuid();
}

public static class ResilienceBehaviour
{
    public const string AlwaysFail = "always-fail";
    public const string Cancel = "cancel";
    public const string RetryThenSucceed = "retry-then-succeed";
    public const string Succeed = "succeed";
}
