using ValidatedWorld.Application;

namespace ValidatedWorld.Application.Tests;

public sealed class AssemblySmokeTests
{
    [Fact]
    public void Application_assembly_is_loadable()
    {
        Assert.NotNull(typeof(ApplicationAssembly).Assembly);
    }
}
