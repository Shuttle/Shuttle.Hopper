namespace Shuttle.Hopper;

public interface IDropTransport
{
    Task DropAsync(CancellationToken cancellationToken = default);
}