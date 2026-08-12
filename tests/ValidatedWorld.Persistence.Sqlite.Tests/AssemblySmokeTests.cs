using ValidatedWorld.Persistence.Sqlite;

namespace ValidatedWorld.Persistence.Sqlite.Tests;

public sealed class AssemblySmokeTests
{
    [Fact]
    public void Sqlite_persistence_assembly_is_loadable()
    {
        Assert.NotNull(typeof(SqlitePersistenceAssembly).Assembly);
    }
}
