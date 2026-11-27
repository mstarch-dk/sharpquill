using Xunit;

namespace SharpQuill.Api.Tests.Fixtures;

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}
