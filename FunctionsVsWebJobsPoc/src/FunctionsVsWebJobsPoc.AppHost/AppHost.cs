var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("webjobPoc")
    .WithDataVolume();

var sqldb = sql.AddDatabase("sqldb", "PocDb");

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(container =>
    {
        container.WithArgs("--skipApiVersionCheck");
    });

var blobs = storage.AddBlobs("AzureStorage");
var functionContainer = storage.AddBlobContainer("function");
var webjobContainer = storage.AddBlobContainer("webjob");

var serviceBus = builder.AddAzureServiceBus("servicebus")
    .RunAsEmulator();

var functionQueue = serviceBus.AddServiceBusQueue("function-queue");
var webjobQueue = serviceBus.AddServiceBusQueue("webjob-queue");

var functionApp = builder.AddAzureFunctionsProject<Projects.FunctionsVsWebJobsPoc_FunctionApp>("functionapp")
    .WithReference(sqldb, "sqldb")
    .WithReference(blobs, "AzureStorage")
    .WithReference(functionQueue, "ServiceBusConnection")
    .WaitFor(sqldb)
    .WaitFor(storage)
    .WaitFor(serviceBus);

var webJobApp = builder.AddProject<Projects.FunctionsVsWebJobsPoc_WebJobApp>("webjobapp")
    .WithReference(sqldb, "sqldb")
    .WithReference(blobs, "AzureStorage")
    .WithReference(webjobQueue, "ServiceBusConnection")
    .WaitFor(sqldb)
    .WaitFor(storage)
    .WaitFor(serviceBus);

builder.Build().Run();
