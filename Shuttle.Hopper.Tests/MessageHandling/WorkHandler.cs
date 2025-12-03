namespace Shuttle.Hopper.Tests.MessageHandling;

public class WorkHandler : IMessageHandler<WorkMessage>
{
    public async Task ProcessMessageAsync(IHandlerContext<WorkMessage> context)
    {
        Console.WriteLine($@"[work-message] : guid = {context.Message.Guid}");

        await Task.CompletedTask;
    }
}