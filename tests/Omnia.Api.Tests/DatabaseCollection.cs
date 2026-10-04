namespace Omnia.Api.Tests;

[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<PostgresContainerFixture>
{
    public const string Name = "postgres-database";
}
