namespace Shuttle.Hopper;

public class MessageRouteOptions
{
    public List<SpecificationOptions> Specifications { get; set; } = [];
    public string Uri { get; set; } = string.Empty;

    public class SpecificationOptions
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}