using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Voluta;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Results;
using Voluta.DependencyInjection;
using Voluta.DependencyInjection.Checkpoints;
using Voluta.Graph.Builder;
using Voluta.Hosting;
using Voluta.Hosting.Wake;
using Voluta.Hosting.Worker;
using Xunit;

namespace Voluta.Hosting.Unit;

public sealed class AddVolutaWorkerHostingShould
{
    [Fact(DisplayName = "Given AddVolutaWorkerHosting, when built, then bus runner and worker resolve as singletons")]
    public void RegisterBusRunnerAndWorker()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddVoluta(voluta =>
        {
            voluta.Checkpoints.UseInMemory();
            voluta.Graph((_, checkpointer) => new StateGraph()
                .AddChannel("x", ChannelKind.LastValue)
                .AddNode("n", static (_, _) => Task.FromResult<NodeResult>(NodeResult.Continue()))
                .AddEdge(GraphConstants.Start, "n")
                .AddEdge("n", GraphConstants.End)
                .Compile(checkpointer));
        });
        services.AddVolutaWorkerHosting();

        using var provider = services.BuildServiceProvider();

        var bus = provider.GetRequiredService<IThreadWakeBus>();
        var concrete = provider.GetRequiredService<InMemoryThreadWakeBus>();
        var runner = provider.GetRequiredService<GraphThreadRunner>();
        var worker = provider.GetRequiredService<GraphWorkerService>();
        var hosted = provider.GetServices<IHostedService>().OfType<GraphWorkerService>().ToArray();

        bus.ShouldBeSameAs(concrete);
        runner.ShouldNotBeNull();
        worker.ShouldNotBeNull();
        hosted.ShouldHaveSingleItem();
        hosted[0].ShouldBeSameAs(worker);
    }
}
