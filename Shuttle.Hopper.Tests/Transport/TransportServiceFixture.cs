using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace Shuttle.Hopper.Tests;

[TestFixture]
public class TransportServiceFixture
{
    private static TransportService CreateTransportService(Mock<ITransportFactory> transportFactory, IUriResolver? uriResolver = null)
    {
        var hopperOptions = Options.Create(new HopperOptions());

        return new(hopperOptions, new TransportFactoryService([transportFactory.Object]), uriResolver ?? new UriResolver(hopperOptions));
    }

    private static Mock<ITransportFactory> CreateNormalisingTransportFactory(Func<int> onCreate)
    {
        var transportFactory = new Mock<ITransportFactory>();

        transportFactory.Setup(m => m.Scheme).Returns("normalising");
        transportFactory.Setup(m => m.CreateAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>())).ReturnsAsync((Uri uri, CancellationToken _) =>
        {
            onCreate();

            var transport = new Mock<ITransport>();

            transport.Setup(m => m.Uri).Returns(new TransportUri(new UriBuilder(uri) { Host = "normalised-host" }.Uri));

            return transport.Object;
        });

        return transportFactory;
    }

    [Test]
    public async Task Should_cache_transport_by_requested_uri_when_transport_normalises_its_uri_async()
    {
        var created = 0;
        var transportService = CreateTransportService(CreateNormalisingTransportFactory(() => ++created));
        var uri = new Uri("normalising://./work");

        var first = await transportService.GetAsync(uri);
        var second = await transportService.GetAsync(uri);

        Assert.That(first.Uri.Uri, Is.Not.EqualTo(uri));
        Assert.That(second, Is.SameAs(first));
        Assert.That(created, Is.EqualTo(1));
        Assert.That(await transportService.ContainsAsync(uri), Is.True);
        Assert.That(await transportService.FindAsync(uri), Is.SameAs(first));
        Assert.That(await transportService.FindAsync(first.Uri.Uri), Is.SameAs(first));
    }

    [Test]
    public async Task Should_cache_resolved_transport_by_resolver_uri_async()
    {
        var created = 0;
        var uriResolver = new Mock<IUriResolver>();
        var resolverUri = new Uri("resolver://mapped/work");

        uriResolver.Setup(m => m.GetTarget(resolverUri)).Returns(new Uri("normalising://./work"));

        var transportService = CreateTransportService(CreateNormalisingTransportFactory(() => ++created), uriResolver.Object);

        var first = await transportService.GetAsync(resolverUri);
        var second = await transportService.GetAsync(resolverUri);

        Assert.That(first, Is.InstanceOf<ResolvedTransport>());
        Assert.That(first.Uri.Uri, Is.EqualTo(resolverUri));
        Assert.That(second, Is.SameAs(first));
        Assert.That(created, Is.EqualTo(1));
    }
}
