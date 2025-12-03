namespace Shuttle.Hopper;

public class OutboxOptions : ProcessorOptions
{
    public OutboxOptions()
    {
        ThreadCount = 1;
    }
}