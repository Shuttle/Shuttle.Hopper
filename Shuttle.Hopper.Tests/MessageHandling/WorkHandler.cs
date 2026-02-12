namespace Shuttle.Hopper.Tests.MessageHandling;

public class WorkHandler : IMessageHandler<WorkMessage>
{
    public async Task ProcessMessageAsync(WorkMessage message, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($@"[work-message] : guid = {message.Guid}");

        await Task.CompletedTask;
    }
}