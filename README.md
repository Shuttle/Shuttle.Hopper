# Shuttle.Hopper

Shuttle.Hopper is a comprehensive message bus implementation that facilitates message-driven communication between different components of an application. It provides a robust and flexible architecture for building distributed systems.

## Installation

```bash
dotnet add package Shuttle.Hopper
```

## Registration

To use Shuttle.Hopper, you need to register it with your service collection:

```csharp
services.AddHopper(options =>
{
    // Configure Hopper options here
});
```

*Note: While configuring options via code is supported as shown above, binding from `IConfiguration` (e.g., using `appsettings.json`) is preferable in most cases.*

The `AddHopper` method returns a `HopperBuilder` that can be used to further configure the bus, such as adding message handlers and subscriptions.

## Messaging Operations

The core interface for sending and publishing messages is `IBus`.

### `IBus`

You can use `IBus` to send commands or publish events:

```csharp
// Sending a command
await bus.SendAsync(new MyCommand { Value = "Hello" });

// Publishing an event
await bus.PublishAsync(new MyEvent { OccurredAt = DateTime.Now });
```

## Message Handlers

Shuttle.Hopper supports two types of message handlers:

### `IContextMessageHandler<T>`

This handler receives an `IHandlerContext<T>` providing access to message metadata and the ability to send or publish messages within the handler context.

```csharp
public class MyContextHandler : IContextMessageHandler<MyMessage>
{
    public async Task HandleAsync(IHandlerContext<MyMessage> context, CancellationToken cancellationToken = default)
    {
        // Handle the message
        var message = context.Message;
        
        // Use the context to send another message
        await context.SendAsync(new AnotherMessage());
    }
}
```

### `IMessageHandler<T>`

This handler receives the message directly, which is useful for simpler handling scenarios.

```csharp
public class MySimpleHandler : IMessageHandler<MyMessage>
{
    public async Task HandleAsync(MyMessage message, CancellationToken cancellationToken = default)
    {
        // Handle the message
        Console.WriteLine(message.Value);
    }
}
```

### Delegate Handlers

You can also register delegates (such as lambda expressions) directly to handle messages without implementing an interface. A delegate can optionally accept an `IHandlerContext<T>` or just the message type, and must return a `Task` or `ValueTask`. An optional `CancellationToken` may also be passed into the delegate if required.

```csharp
services.AddHopper(options => { ... })
    .AddMessageHandler(async (IHandlerContext<MyMessage> context, CancellationToken cancellationToken) =>
    {
        // Handle the message using the context
        await context.SendAsync(new AnotherMessage(), builder: null, cancellationToken: cancellationToken);
    })
    .AddMessageHandler(async (MyMessage message) => 
    {
        // Handle the message directly
        Console.WriteLine(message.Value);
    });
```

### Registering Message Handlers

Interface-based message handlers can be registered using the `HopperBuilder`:

```csharp
services.AddHopper(options => { ... })
    .AddMessageHandler<MyContextHandler>()
    .AddMessageHandler<MySimpleHandler>()
    .AddMessageHandlersFrom(typeof(MyContextHandler).Assembly);
```

## Subscriptions

You can add subscriptions to the bus using the `HopperBuilder`:

```csharp
services.AddHopper(options => { ... })
    .AddSubscription<MyEvent>();
```

## Transports

Shuttle.Hopper abstracts over physical transport implementations via the `ITransport` and `ITransportFactory` interfaces. To perform actual message passing, you'll need to install an implementation package suited for your infrastructure (e.g., MSMQ, RabbitMQ, Azure Service Bus) and ensure its transport factory is registered. Depending on the transport, you may also define `UriMappingOptions` to map your application's abstract logical URIs to physical queue locations.

## Processing Concepts

Shuttle.Hopper provides advanced architectural features such as inbox processing, outbox atomic messaging, and deferred dispatch. 

### Inbox and Outbox Processing

The `InboxProcessor` and `OutboxProcessor` can be configured via `HopperOptions`. 
*   **Inbox processing** defines where work messages arrive and where failure messages go.
*   **Outbox processing** acts as a staging queue, ensuring atomic dispatch in distributed transaction boundaries.

```json
{
  "Shuttle": {
    "Hopper": {
      "Inbox": {
        "WorkTransportUri": "queue://inbox-work",
        "ErrorTransportUri": "queue://inbox-error",
        "ThreadCount": 5
      },
      "Outbox": {
        "WorkTransportUri": "queue://outbox-work",
        "ErrorTransportUri": "queue://outbox-error"
      }
    }
  }
}
```

### Additional Inboxes

An endpoint may process one or more additional inbox work queues next to its primary `Inbox`, without an extra deployment. Each additional inbox is registered by name using `AddInbox` and gets its own dedicated processor threads and idle back-off, so a busy queue does not starve another. There is no precedence between the queues.

```csharp
services
    .AddHopper(options =>
    {
        configuration.GetSection(HopperOptions.SectionName).Bind(options);
    })
    .AddInbox("priority");
```

```json
{
  "Shuttle": {
    "Hopper": {
      "Inbox": {
        "WorkTransportUri": "azuresq://azure/my-server-work",
        "DeferredTransportUri": "azuresq://azure/my-server-deferred",
        "ErrorTransportUri": "azuresq://azure/shuttle-error"
      },
      "AdditionalInboxes": {
        "priority": {
          "WorkTransportUri": "azuresq://azure/my-server-work-priority",
          "ThreadCount": 2
        }
      }
    }
  }
}
```

The options may also be set in code, with or without a configuration entry:

```csharp
.AddInbox("priority", options => options.WorkTransportUri = new("azuresq://azure/my-server-work-priority"));
```

The following rules apply to an additional inbox:

*   The primary `Inbox.WorkTransportUri` is required whenever additional inboxes exist.
*   `WorkTransportUri` is required and may not be any other transport uri used by the endpoint (the primary inbox's work, deferred and error transports, the outbox's transports, or another additional inbox's work or error transport).
*   `ErrorTransportUri` is optional; when it is not set the primary inbox's error transport is used.
*   `DeferredTransportUri` may not be set; deferred messages are parked on the primary inbox's deferred transport.
*   `ThreadCount`, `MaximumFailureCount`, `IdleDurations` and `IgnoreOnFailureDurations` behave as they do for the primary inbox.
*   `DeferredMessageProcessorResetInterval` and `DeferredMessageProcessorIdleDuration` are ignored.
*   Inbox names are case-insensitive, and an `AdditionalInboxes` entry that has not been registered using `AddInbox` causes the bus to fail on start.

The following semantics apply:

*   `SenderInboxWorkTransportUri` and `ToSelf()` use the primary inbox, which remains the endpoint's identity. Replies to messages taken from an additional inbox therefore arrive on the primary inbox, and a handler that sends a message using `ToSelf()` sends it to the primary inbox.
*   Subscriptions and published events target the primary inbox only. Additional inboxes are intended for direct sends using `WithRecipient` or message routes.
*   A deferred message (including a failed message that is retried after an ignore duration) is returned to the additional inbox whose work transport uri matches the message's `RecipientInboxWorkTransportUri`; any other message is returned to the primary inbox. The match is on the uri as configured, so a `resolver://` inbox only matches a recipient using the same `resolver://` uri, and the path is case-sensitive. A message placed on an additional inbox queue with a different or empty recipient moves to the primary inbox after its first deferral.
*   Additional inbox thread pools use the service key `InboxProcessor:{name}`, with the name lower-cased, which is visible in the `ThreadingOptions` events.

### Deferred Messages

If an application requires messages to be deferred and processed at a later time, you can configure the `DeferredTransportUri` in your `InboxOptions`. Shuttle.Hopper will actively monitor this endpoint using a `DeferredMessageProcessor` to pick up the deferred messages when appropriate.

## Message Routing

For outbound commands (`SendAsync`), the bus determines the correct destination through an `IMessageRouteProvider`. You can route messages by configuring `MessageRouteOptions` with matching specifications (e.g., regex matching, starts-with matching, specific assemblies, or explicit type lists):

```json
{
  "Shuttle": {
    "Hopper": {
      "MessageRoutes": [
        {
          "Uri": "queue://external-service",
          "Specifications": [
            {
              "Name": "StartsWith",
              "Value": "MyCompany.Messages"
            },
            {
              "Name": "Assembly",
              "Value": "MyCompany.Messages.Assembly"
            }
          ]
        }
      ]
    }
  }
}
```

## Bus Control

The `IBusControl` interface is used to start and stop the bus dynamically.

### `IBusControl`

```csharp
public interface IBusControl : IDisposable, IAsyncDisposable
{
    bool Started { get; }
    Task<IBusControl> StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
```

### .NET Generic Host Support

Shuttle.Hopper integrates elegantly into the standard .NET `IHostedService` lifecycle. If the `AutoStart` option is set to `true` (which is the default), a registered `BusHostedService` automatically handles starting and stopping the bus alongside your application host.