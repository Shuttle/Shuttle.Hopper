using Microsoft.Extensions.Options;
using Moq;
using Shuttle.Core.Pipelines;
using Shuttle.Core.TransactionScope;

namespace Shuttle.Hopper.Tests;

public static class PipelineDependenciesExtensions
{
    extension(PipelineDependencies)
    {
        public static IPipelineDependencies Empty()
        {
            return new PipelineDependencies(Options.Create(new PipelineOptions()), Options.Create(new TransactionScopeOptions()), new Mock<ITransactionScopeFactory>().Object, new Mock<IServiceProvider>().Object);
        }
    }
}