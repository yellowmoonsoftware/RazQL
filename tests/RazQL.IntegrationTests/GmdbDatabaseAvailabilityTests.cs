using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using RazQL.Dapper;
using RazQL.Execution;
using RazQL.IntegrationTests.Fixtures;
using RazQL.Template;

namespace RazQL.IntegrationTests;

[Collection(GmdbCollection.Name)]
public sealed class GmdbDatabaseAvailabilityTests(GmdbDatabaseFixture fixture)
{
    [Fact]
    public async Task Fixture_ProvidesSeededDatabaseAndRazQLServices()
    {
        var services = fixture.Services;
        Assert.IsType<DapperExecutionAdapter>(services.GetRequiredService<IExecutionAdapter>());
        Assert.IsType<QueryExecutor>(services.GetRequiredService<IQueryExecutor>());
        Assert.IsType<SqlGenerator>(services.GetRequiredService<ISqlGenerator>());
        Assert.IsType<DefaultTemplateCache>(services.GetRequiredService<ITemplateCache>());

        var dataSource = services.GetRequiredService<DbDataSource>();
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "select count(*) from gmdb.artist where name = 'AC/DC'";

        Assert.Equal(1L, Convert.ToInt64(await command.ExecuteScalarAsync()));
    }
}
