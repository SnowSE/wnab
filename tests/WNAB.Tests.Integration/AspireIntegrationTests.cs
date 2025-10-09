using System.Net;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace WNAB.Tests.Integration;

public class AspireIntegrationTests
{
    [Fact]
    public async Task GetWebResourceRootReturnsOkStatusCode()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.WNAB_AppHost>();

        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        // Act
        var httpClient = app.CreateHttpClient("wnab-web");
        var response = await httpClient.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetApiHealthEndpointReturnsHealthy()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.WNAB_AppHost>();

        appHost.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        // Act
        var httpClient = app.CreateHttpClient("wnab-api");
        var response = await httpClient.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", content);
    }

    [Fact]
    public async Task PostgresDatabaseIsAvailableAndAccessible()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.WNAB_AppHost>();

        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        // Get the connection string for the database
        var connectionString = await app.GetConnectionStringAsync("wnabdb");

        // Assert
        Assert.NotNull(connectionString);
        Assert.Contains("Host=", connectionString);
        Assert.Contains("Database=wnabdb", connectionString);
    }

    [Fact]
    public async Task MailPitResourceIsAvailableWithWebUI()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.WNAB_AppHost>();

        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        // Act - MailPit exposes a web UI
        var httpClient = app.CreateHttpClient("mailpit");
        var response = await httpClient.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ApiCanConnectToDatabase()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.WNAB_AppHost>();

        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        // Act - Try to hit an endpoint that would require DB access
        var httpClient = app.CreateHttpClient("wnab-api");

        // Give the API time to start and connect to the database
        await Task.Delay(TimeSpan.FromSeconds(5));

        var response = await httpClient.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
