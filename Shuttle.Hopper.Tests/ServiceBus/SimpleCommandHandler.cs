namespace Shuttle.Hopper.Tests;

public class SimpleCommandHandler : IMessageHandler<SimpleCommand>
{
    public async Task HandleAsync(SimpleCommand message, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($@"Handled SimpleCommand with name '{message.Name}.");

        await Task.CompletedTask.ConfigureAwait(false);
    }
}