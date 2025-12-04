namespace Shuttle.Hopper.Tests.MessageHandling;

public class WorkHandler : IContextHandler<WorkMessage>
{
    public async Task ProcessMessageAsync(IHandlerContext<WorkMessage> context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($@"[work-message] : guid = {context.Message.Guid}");

        await Task.CompletedTask;
    }
}