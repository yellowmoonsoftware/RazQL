# RazQL.Dapper

`RazQL.Dapper` executes RazQL's generated SQL through Dapper. It depends on `RazQL.Core` and is included in the standard `RazQL` package. Consumers using a different execution adapter can reference `RazQL.Core` and `RazQL.Generators` without bringing in Dapper.

When using `RazQL.DependencyInjection`, select the adapter explicitly:

```csharp
using RazQL.Dapper;

services.AddRazQL(builder => builder
    .UsingExecutionAdapter<DapperExecutionAdapter>()
    .AddMappersFromAssembly(typeof(IArtistMapper).Assembly));
```

The application must also register a provider-specific `DbDataSource`.
