using Scalar.Aspire;

var builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<IResourceWithConnectionString> db;
//IResourceBuilder<IResourceWithConnectionString> mailResource;

if (builder.ExecutionContext.IsPublishMode)
{
    builder.AddAzureContainerAppEnvironment("env");

    // Use Azure PostgreSQL Flexible Server for production
    var postgres = builder.AddAzurePostgresFlexibleServer("postgres");
    db = postgres.AddDatabase("wnabdb");

    // Use Azure Communication Services for email in production
    // Or configure SendGrid or another email service
    // For now, we'll use a placeholder connection string that the API can handle
    //mailResource = builder.AddConnectionString("mailpit");
}
else
{
    // Local development uses containerized PostgreSQL with admin tools
    var postgres = builder.AddPostgres("postgres")
        .WithDataVolume("wnab_aspire_data")
        .WithPgWeb()
        .WithLifetime(ContainerLifetime.Persistent)
        .WithPgAdmin(pgAdmin =>
        {
            pgAdmin.WithHostPort(5050);
            pgAdmin.WithLifetime(ContainerLifetime.Persistent);
        });

    db = postgres.AddDatabase("wnabdb");

    // Local development uses Mailpit for email testing
    //mailResource = builder.AddMailPit("mailpit")
    //    .WithLifetime(ContainerLifetime.Persistent);
}

var api = builder.AddProject<Projects.WNAB_API>("wnab-api")
    .WithExternalHttpEndpoints()
    .WithReference(db)
    //.WithReference(mailResource)
    .WaitFor(db);

var keycloakSecret = builder.AddParameter("keycloakclientsecret", secret: true);

var web = builder.AddProject<Projects.WNAB_Web>("wnab-web")
    .WithExternalHttpEndpoints()
    .WithReference(api)
    .WithEnvironment("Keycloak__ClientSecret", keycloakSecret);

// Only add MAUI project during local development, not during Azure deployment
// MAUI is a client-side framework that cannot be containerized for cloud deployment
if (!builder.ExecutionContext.IsPublishMode)
{
    builder.AddProject<Projects.WNAB_Maui>("wnab-maui")
        .WithReference(api);

    var tunnel = builder.AddDevTunnel("wnab-tunnel")
        .WithReference(web.GetEndpoint("http"));

    builder.AddScalarApiReference()
        .WithApiReference(api)
        .WithLifetime(ContainerLifetime.Persistent);
}

builder.Build().Run();
