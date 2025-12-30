using Microsoft.Extensions.Configuration;

namespace Shuttle.Hopper.Tests;

public class OptionsFixture
{
    protected HopperOptions GetOptions()
    {
        var result = new HopperOptions();

        new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @".\Options\appsettings.json")).Build()
            .GetSection(HopperOptions.SectionName).Bind(result);

        return result;
    }
}