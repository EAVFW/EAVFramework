# EAVFramework.Extensions.Aspire.Hosting



```
using Aspire.Hosting;
using EAVFramework.Extensions.Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var sqlserver = builder.AddSqlServer("MCAspireSQL");

var db = sqlserver.AddDatabase("MC");

var eavmodel = builder.AddEAVFWModel<Projects.MC_Models>("mc-model")
    .WithNetTopologySuite()
    .PublishTo(db);

var management = builder.AddProject<Projects.MC_Portal>("mc-admin");

management
    .WithEnvironment("EAVFW_JOBSERVER_DISABLED","true")
    .WithEnvironment("SubscriberQueue__Disabled","true")
    .WithReference(db, "ApplicationDB")
    .Needs(eavmodel);


builder.Build().Run();

```

