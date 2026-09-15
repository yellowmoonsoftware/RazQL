# RazQL.Generators

`RazQL.Generators` is the incremental source generator and compile-time validator for RazQL mapper interfaces.

## Mapper Contract

Apply `RazQLMapperAttribute` to an interface. Every member must be a public instance method without a body. Each method must:

- Be non-generic.
- Accept exactly one by-value criteria parameter followed by a by-value `CancellationToken`.
- Return `Task<T>` or `Task<IEnumerable<T>>`.
- Avoid nested task, async-enumerable, enumerator, and concrete collection result shapes.
- Produce an unambiguous conventional or explicitly configured template name.

For every valid mapper, the generator emits an internal sealed implementation in `{MapperNamespace}.Generated`. The implementation delegates execution to `IQueryExecutor`, exposes its descriptors for query calls, implements `IMapperTemplatePreloader`, and emits an assembly-level mapper-registration attribute for dependency injection.

## Template Validation

The generator validates source existence and build metadata for the built-in resource and file-system loaders. Embedded templates should use:

```xml
<ItemGroup>
  <EmbeddedResource Include="SqlTemplates/**/*.sql.cshtml" />
</ItemGroup>
```

File-system templates must be `Content`, copied to output, and retain a target path matching the RazQL convention. Inline `RazQLQueryAttribute` templates do not require a file. Custom loaders are not source-validated because their location semantics are application-defined.

The NuGet package installs the generator automatically as an analyzer. Mark the package as private when it should remain an implementation detail of the consuming project:

```xml
<PackageReference Include="RazQL.Generators" Version="..." PrivateAssets="all" />
```

The packaged `buildTransitive/RazQL.Generators.targets` file exposes matching `.sql.cshtml` items to the generator as `AdditionalFiles`. When referencing this project directly from a source checkout, configure it as an analyzer and import the target explicitly:

```xml
<ProjectReference Include="../RazQL.Generators/RazQL.Generators.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
<Import Project="../RazQL.Generators/buildTransitive/RazQL.Generators.targets" />
```

Compiler diagnostics use the `RAZQL` prefix and are errors when a mapper cannot be generated safely or its built-in template source cannot be resolved unambiguously.
