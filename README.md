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

### Registering Message Handlers

Message handlers can be registered using the `HopperBuilder`:

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

## Bus Control

The `IBusControl` interface is used to start and stop the bus. By default, the bus starts automatically when the application starts if the `AutoStart` option is set to `true` (which is the default).

### `IBusControl`

```csharp
public interface IBusControl : IDisposable, IAsyncDisposable
{
    bool Started { get; }
    Task<IBusControl> StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
```