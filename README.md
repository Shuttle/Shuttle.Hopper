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