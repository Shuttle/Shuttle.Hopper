using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public class ServiceBusConfiguration : IServiceBusConfiguration
{
    public ServiceBusConfiguration(IOptions<ServiceBusOptions> serviceBusOptions, ITransportService transportService)
    {
        Guard.AgainstNull(transportService);

        var options = Guard.AgainstNull(Guard.AgainstNull(serviceBusOptions).Value);

        if (!string.IsNullOrWhiteSpace(options.Inbox.WorkTransportUri))
        {
            Inbox = new InboxConfiguration
            {
                WorkTransport = transportService.GetAsync(options.Inbox.WorkTransportUri).GetAwaiter().GetResult(),
                DeferredTransport =
                    string.IsNullOrWhiteSpace(options.Inbox.DeferredTransportUri)
                        ? null
                        : transportService.GetAsync(options.Inbox.DeferredTransportUri).GetAwaiter().GetResult(),
                ErrorTransport =
                    string.IsNullOrWhiteSpace(options.Inbox.ErrorTransportUri)
                        ? null
                        : transportService.GetAsync(options.Inbox.ErrorTransportUri).GetAwaiter().GetResult()
            };
        }

        if (!string.IsNullOrWhiteSpace(options.Outbox.WorkTransportUri))
        {
            Outbox = new OutboxConfiguration
            {
                WorkTransport = transportService.GetAsync(options.Outbox.WorkTransportUri).GetAwaiter().GetResult(),
                ErrorTransport =
                    string.IsNullOrWhiteSpace(options.Outbox.ErrorTransportUri)
                        ? null
                        : transportService.GetAsync(options.Outbox.ErrorTransportUri).GetAwaiter().GetResult()
            };
        }
    }

    public IInboxConfiguration? Inbox { get; }
    public IOutboxConfiguration? Outbox { get; }
}