var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("wnab_aspire_data");

var db = postgres.AddDatabase("wnabdb");

var mailpit = builder.AddMailPit("mailpit");

var api = builder.AddProject<Projects.WNAB_API>("wnab-api")
    .WithReference(db)
    .WithReference(mailpit);

// LLM-Dev: Launch the Web project from AppHost and pass the API base URL so Web doesn't need a hardcoded port.
// Prefer http. Pass the endpoint reference directly; Aspire resolves it to a URL at runtime.
// LLM-Dev: Avoid calling .Url on EndpointReference (not available at compile time).

var web = builder.AddProject<Projects.WNAB_Web>("wnab-web")
    .WithReference(api);

builder.AddProject<Projects.WNAB_Maui>("wnab-maui")
    .WithReference(api);

var tunnel = builder.AddDevTunnel("wnab-tunnel")
    .WithAnonymousAccess()
    .WithReference(web.GetEndpoint("http"));

builder.Build().Run();
