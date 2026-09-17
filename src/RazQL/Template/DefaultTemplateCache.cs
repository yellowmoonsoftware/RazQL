using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RazorEngineCore;

namespace RazQL.Template;

/// <summary>Compiles Razor query templates once per descriptor, retaining successful tasks and evicting failures.</summary>
/// <param name="razorEngine">The Razor engine used to compile template source.</param>
/// <param name="resolver">The resolver used to select a source loader.</param>
/// <param name="logger">The logger used for compilation diagnostics.</param>
public sealed class DefaultTemplateCache(IRazorEngine razorEngine, ITemplateSourceLoaderResolver resolver, ILogger<DefaultTemplateCache> logger) : ITemplateCache
{
    private readonly ConcurrentDictionary<QueryDescriptor, Task<object>> _templates = new();

    /// <inheritdoc />
    public async Task<IRazorEngineCompiledTemplate<RazQLModel<TCriteria>>> GetTemplateAsync<TMapper, TCriteria, TResult>(QueryDescriptor<TMapper, TCriteria, TResult> descriptor, CancellationToken cancellationToken)
    {
        var cachedTemplate = _templates.GetOrAdd(descriptor, static async (qryDesc, args) =>
        {
            var (engine, res, log, token) = args;
            var sourceLoader = res.Resolve(qryDesc);
            var templateSource = await sourceLoader.LoadAsync(qryDesc, token);
            var compiledTemplate = await engine.CompileAsync<RazQLModel<TCriteria>>(templateSource, b =>
            {
                b.AddUsing("RazQL.Binding");
            }, token);
            log.LogInformation("Compiled Template Source: [{QueryMethodGroup}.{QueryMethodName}]", qryDesc.MapperType.Name, qryDesc.QueryMethod.Name);
            return compiledTemplate;
        }, (razorEngine, resolver, logger, cancellationToken));

        // Await the cached template task to catch any failed load/compile tasks (from the factory method)
        // and remove that exact failed descriptor/task from the cache
        try
        {
            return (IRazorEngineCompiledTemplate<RazQLModel<TCriteria>>)await cachedTemplate;
        }
        catch
        {
            _templates.TryRemove(new(descriptor, cachedTemplate));
            throw;
        }
    }
}
