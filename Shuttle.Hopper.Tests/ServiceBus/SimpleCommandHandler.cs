namespace Shuttle.Hopper.Tests;

public class SimpleCommandHandler : IMessageHandler<SimpleCommand>
{
    public async Task ProcessMessageAsync(IHandlerContext<SimpleCommand> context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($@"Handled SimpleCommand with name '{context.Message.Name}.");

        await Task.CompletedTask.ConfigureAwait(false);
    }
}