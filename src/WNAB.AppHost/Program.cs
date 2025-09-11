var builder = DistributedApplication.CreateBuilder(args);

// Add SQL Server database
var sqlServer = builder.AddSqlServer("sqlserver")
    .WithLifetime(ContainerLifetime.Persistent);

var database = sqlServer.AddDatabase("wnabdb");

// Add the API project with database reference
var api = builder.AddProject<Projects.WNAB_API>("wnab-api")
    .WithReference(database);

// Add the Web project with API reference  
var web = builder.AddProject<Projects.WNAB_Web>("wnab-web")
    .WithReference(api);

builder.Build().Run();
