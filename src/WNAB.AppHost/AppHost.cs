using Scalar.Aspire;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("wnab_aspire_data");

var db = postgres.AddDatabase("wnabdb");

var mailpit = builder.AddMailPit("mailpit");

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
    .WithApiReference(api);

builder.Build().Run();
