namespace Shuttle.Hopper.Tests;

public class SimpleCommandHandler : IDirectMessageHandler<SimpleCommand>
{
    public async Task ProcessMessageAsync(SimpleCommand message, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($@"Handled SimpleCommand with name '{message.Name}.");

        await Task.CompletedTask.ConfigureAwait(false);
    }
}