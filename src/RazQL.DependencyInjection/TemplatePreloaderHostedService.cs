using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RazQL.Template;

namespace RazQL.DependencyInjection;

/// <summary>Loads and compiles registered generated mapper templates during host startup.</summary>
/// <param name="templateCache">The shared template cache to prime.</param>
/// <param name="preloaders">The generated mapper template preloaders.</param>
/// <param name="logger">The logger used for preload diagnostics.</param>
public sealed partial class TemplatePreloaderHostedService(
    ITemplateCache templateCache,
    IEnumerable<IMapperTemplatePreloader> preloaders,
    ILogger<TemplatePreloaderHostedService> logger) : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Task? preloadTask = null;

        try
        {
            var tasks = preloaders
                .SelectMany(preloader =>
                {
                    LogPreloaderStarted(preloader.GetType());
                    return preloader.PreloadTemplates(templateCache, cancellationToken);
                })
                .ToArray();

            LogPreloadingStarted(tasks.Length);
            preloadTask = Task.WhenAll(tasks);
            await preloadTask;
            LogPreloadingCompleted(tasks.Length);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            LogPreloadingCancelled();
            throw;
        }
        catch (Exception exception)
        {
            var exceptions = preloadTask?.Exception?.Flatten().InnerExceptions ?? [exception];
            foreach (var preloadException in exceptions)
            {
                LogPreloadingFailed(preloadException);
            }

            throw;
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(LogLevel.Information, "Preloading RazQL templates from {PreloaderType}.")]
    private partial void LogPreloaderStarted(Type preloaderType);

    [LoggerMessage(LogLevel.Information, "Preloading {TemplateCount} RazQL templates.")]
    private partial void LogPreloadingStarted(int templateCount);

    [LoggerMessage(LogLevel.Information, "Preloaded {TemplateCount} RazQL templates.")]
    private partial void LogPreloadingCompleted(int templateCount);

    [LoggerMessage(LogLevel.Warning, "RazQL template preloading was cancelled during host startup.")]
    private partial void LogPreloadingCancelled();

    [LoggerMessage(LogLevel.Critical, "RazQL template preloading failed during host startup.")]
    private partial void LogPreloadingFailed(Exception exception);
}
