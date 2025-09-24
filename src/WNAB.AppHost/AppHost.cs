var builder = DistributedApplication.CreateBuilder(args);

// Provision a PostgreSQL server and a database for the app
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("wnab_aspire_data");

var db = postgres.AddDatabase("wnabdb");

// Wire the API to the database so it receives a connection string via configuration
var api = builder.AddProject<Projects.WNAB_API>("wnab-api")
    .WithReference(db)
    // LLM-Dev: Define an explicit HTTP endpoint so Aspire can surface the port and enable service discovery.
    // Avoid forcing ASPNETCORE_URLS; let AppHost own the endpoint configuration.
    .WithHttpEndpoint(name: "http", port: 5290);

// LLM-Dev: Launch the Web project from AppHost and pass the API base URL so Web doesn't need a hardcoded port.
// Prefer http. Pass the endpoint reference directly; Aspire resolves it to a URL at runtime.
// LLM-Dev: Avoid calling .Url on EndpointReference (not available at compile time).

builder.AddProject<Projects.WNAB_Web>("wnab-web")
    .WithReference(api)
    .WithEnvironment("ApiBaseUrl", api.GetEndpoint("http"));

builder.Build().Run();
