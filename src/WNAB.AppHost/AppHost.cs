var builder = DistributedApplication.CreateBuilder(args);

// Provision a PostgreSQL server and a database for the app
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var db = postgres.AddDatabase("wnabdb");

// Wire the API to the database so it receives a connection string via configuration
builder.AddProject<Projects.WNAB_API>("wnab-api")
    .WithReference(db);

builder.Build().Run();
