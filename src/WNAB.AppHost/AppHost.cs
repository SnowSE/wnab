using Scalar.Aspire;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("wnabazure");

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("wnab_aspire_data")
    .WithPgWeb()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin(pgAdmin =>
    {
        pgAdmin.WithHostPort(5050);
        pgAdmin.WithLifetime(ContainerLifetime.Persistent);
    });

var db = postgres.AddDatabase("wnabdb");

var mailpit = builder.AddMailPit("mailpit")
    .WithLifetime(ContainerLifetime.Persistent);

var api = builder.AddProject<Projects.WNAB_API>("wnab-api")
    .WithExternalHttpEndpoints()
    .WithReference(db)
    .WithReference(mailpit)
    .WaitFor(db);

var web = builder.AddProject<Projects.WNAB_Web>("wnab-web")
    .WithExternalHttpEndpoints()
    .WithReference(api);

var devTunnel = builder.AddDevTunnel("wnab-tunnel")
    .WithAnonymousAccess()
    .WithReference(api.GetEndpoint("https"));

var mauiApp = builder.AddMauiProject("wnab-maui", "../WNAB.Maui/WNAB.Maui.csproj");

mauiApp.AddWindowsDevice()
    .WithReference(api);

mauiApp.AddAndroidDevice()
    .WithOtlpDevTunnel()
    .WithReference(api, devTunnel);

builder.AddScalarApiReference()
    .WithApiReference(api)
    .WithLifetime(ContainerLifetime.Persistent);

builder.Build().Run();
