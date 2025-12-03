using NUnit.Framework;

namespace Shuttle.Hopper.Tests;

[TestFixture]
public class TypeListMessageRouteSpecificationFixture
{
    [Test]
    public void Should_be_able_to_get_types_from_given_valid_value_string()
    {
        new TypeListMessageRouteSpecification(
            "Shuttle.Hopper.Tests.SimpleCommand, Shuttle.Hopper.Tests;" +
            "Shuttle.Hopper.Tests.SimpleEvent, Shuttle.Hopper.Tests");
    }

    [Test]
    public void Should_fail_when_given_a_type_that_cannot_be_determined()
    {
        Assert.Throws<MessageRouteSpecificationException>(() => new TypeListMessageRouteSpecification("bogus"));
    }
}