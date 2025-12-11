# Documentation

Please visit the [Shuttle.Hopper documentation](https://www.pendel.co.za/shuttle-hopper/home.html) for more information.

# Getting Started

Start a new **Console Application** project.  We'll need to install one of the support transport implementations.  For this example we'll use `Shuttle.Hopper.AzureStorageQueues` which can be hosted locally using [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio%2Cblob-storage):

```
PM> Install-Package Shuttle.Hopper.AzureStorageQueues
```

We'll also make use of the [.NET generic host](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host):

```
PM> Install-Package Microsoft.Extensions.Hosting
```

Next we'll implement our endpoint in order to start listening on our transport:

``` c#
internal class Program
{
    static async Task Main(string[] args)
    {
        await Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services
                    .AddServiceBus(builder =>
                    {
                        builder.Options.Inbox.WorkTransportUri = "azuresq://azure/work";

                        // Delegates may also be added to the builder, including adding dependencies
                        builder.AddMessageHandler(async (IHandlerContext<SomeMessage> context, ISomeDependency instance) =>
                        {
                            Console.WriteLine($@"[some-message] : guid = {context.Message.Guid}");

                            await Task.CompletedTask;
                        });
                    })
                    .AddAzureStorageQueues(builder =>
                    {
                        builder.AddOptions("azure", new AzureStorageTransportOptions
                        {
                            ConnectionString = "UseDevelopmentStorage=true;"
                        });
                    });
            })
            .Build()
            .RunAsync();
    }
}
```

Even though the options may be set directly as above, typically one would make use of a configuration provider:

```c#
internal class Program
{
    private static async Task Main(string[] args)
    {
        await Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                var configuration =
                    new ConfigurationBuilder()
                        .AddJsonFile("appsettings.json")
                        .Build();

                services
                    .AddSingleton<IConfiguration>(configuration)
                    .AddServiceBus(builder =>
                    {
                        configuration
                            .GetSection(ServiceBusOptions.SectionName)
                            .Bind(builder.Options);
                    })
                    .AddAzureStorageQueues(builder =>
                    {
                        builder.AddOptions("azure", new AzureStorageTransportOptions
                        {
                            ConnectionString = configuration
                                .GetConnectionString("azure")
                        });
                    });
            })
            .Build()
            .RunAsync();
    }
}
```

The `appsettings.json` file would be as follows (remember to set to `Copy always`):

```json
{
  "ConnectionStrings": {
    "azure": "UseDevelopmentStorage=true;"
  },
  "Shuttle": {
    "ServiceBus": {
      "Inbox": {
        "WorkTransportUri": "azuresq://azure/work",
      }
    }
  }
}
```

### Send a command message for processing

``` c#
await serviceBus.SendAsync(new RegisterMember
{
    UserName = "user-name",
    EMailAddress = "user@domain.com"
});
```

### Publish an event message when something interesting happens

Before publishing an event one would need to register an `ISubscrtiptionService` implementation such as [Shuttle.Hopper.Sql.Subscription](/implementations/subscription/sql.md).

``` c#
await serviceBus.PublishAsync(new MemberRegistered
{
    UserName = "user-name"
});
```

### Subscribe to those interesting events

``` c#
services.AddServiceBus(builder =>
{
    builder.AddSubscription<MemberRegistered>();
});
```

### Handle any messages

``` c#
public class RegisterMemberHandler : IMessageHandler<RegisterMember>
{
    public RegisterMemberHandler(IDependency dependency)
    {
    }

	public async Task ProcessMessageAsync(IHandlerContext<RegisterMember> context, CancellationToken cancellationToken = default)
	{
        // perform member registration

		await context.PublishAsync(new MemberRegistered
		{
			UserName = context.Message.UserName
		});
	}
}
```

``` c#
public class MemberRegisteredHandler : IMessageHandler<MemberRegistered>
{
	public async Task ProcessMessageAsync(IHandlerContext<MemberRegistered> context, CancellationToken cancellationToken = default)
	{
        // processing
	}
}
```

