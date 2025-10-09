using Scalar.Aspire;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("wnab_aspire_data")
    .WithLifetime(ContainerLifetime.Persistent);

var db = postgres.AddDatabase("wnabdb");

var mailpit = builder.AddMailPit("mailpit")
    .WithLifetime(ContainerLifetime.Persistent);

var api = builder.AddProject<Projects.WNAB_API>("wnab-api")
    .WithReference(db)
    .WithReference(mailpit);

var web = builder.AddProject<Projects.WNAB_Web>("wnab-web")
    .WithReference(api);

builder.AddProject<Projects.WNAB_Maui>("wnab-maui")
    .WithReference(api);

builder.AddDevTunnel("wnab-tunnel")
    .WithAnonymousAccess()
    .WithReference(web.GetEndpoint("http"));

builder.AddScalarApiReference()
    .WithApiReference(api)
    .WithLifetime(ContainerLifetime.Persistent);

builder.Build().Run();
