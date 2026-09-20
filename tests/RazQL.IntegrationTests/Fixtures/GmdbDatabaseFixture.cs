using System.Data.Common;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Images;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using RazQL.Dapper;
using RazQL.DependencyInjection;
using RazQL.IntegrationTests.Mappers;
using RazQL.IntegrationTests.Template;
using Testcontainers.PostgreSql;

namespace RazQL.IntegrationTests.Fixtures;

public sealed class GmdbDatabaseFixture : IAsyncLifetime
{
    private readonly string _superuserPassword = Guid.NewGuid().ToString("N");
    private readonly string _appPassword = Guid.NewGuid().ToString("N");
    private readonly string _adminPassword = Guid.NewGuid().ToString("N");

    private IFutureDockerImage? _image;
    private PostgreSqlContainer? _container;
    private ServiceProvider? _services;

    public IServiceProvider Services => _services ?? throw new InvalidOperationException("The GMDB fixture has not started.");

    public async Task InitializeAsync()
    {
        try
        {
            var dockerDirectory = Path.Combine(AppContext.BaseDirectory, "docker");
            _image = new ImageFromDockerfileBuilder()
                .WithDockerfileDirectory(dockerDirectory)
                .WithName("razql-gmdb-integration:local")
                .Build();
            await _image.CreateAsync();

            var seedPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "test-data.sql");
            _container = new PostgreSqlBuilder(_image)
                .WithDatabase("gmdb")
                .WithUsername("gmdbuser")
                .WithPassword(_superuserPassword)
                .WithEnvironment("GMDB_APP_PASSWORD", _appPassword)
                .WithEnvironment("GMDB_ADMIN_PASSWORD", _adminPassword)
                .WithResourceMapping(seedPath, "/tmp/")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilContainerIsHealthy())
                .Build();
            await _container.StartAsync();

            var seedResult = await _container.ExecAsync(
                ["psql", "-v", "ON_ERROR_STOP=1", "--single-transaction", "-U", "gmdbuser", "-d", "gmdb", "-f", "/tmp/test-data.sql"]);
            if (seedResult.ExitCode != 0)
            {
                throw new InvalidOperationException($"GMDB seed SQL failed: {seedResult.Stderr}");
            }

            var appConnectionString = new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
            {
                Username = "gmdb_app_user",
                Password = _appPassword
            }.ConnectionString;

            var services = new ServiceCollection();
            services.AddSingleton<DbDataSource>(_ => NpgsqlDataSource.Create(appConnectionString));
            services.AddRazQL(builder => builder
                .UsingExecutionAdapter<DapperExecutionAdapter>()
                .AddTemplateSourceLoader<IntegrationTemplateSourceLoader>()
                .AddMappersFromAssembly(typeof(ITestArtistMapper).Assembly));
            _services = services.BuildServiceProvider(validateScopes: true);
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        if (_services is not null)
        {
            await _services.DisposeAsync();
        }

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }

        if (_image is not null)
        {
            await _image.DisposeAsync();
        }
    }
}
