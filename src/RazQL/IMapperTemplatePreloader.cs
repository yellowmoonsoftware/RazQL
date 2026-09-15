using RazQL.Template;

namespace RazQL;

/// <summary>Exposes the known templates associated with a mapper for coordinated preloading.</summary>
public interface IMapperTemplatePreloader
{
    /// <summary>Starts loading and compiling every template used by the mapper.</summary>
    /// <param name="templateCache">The template cache to prime.</param>
    /// <param name="cancellationToken">A token that cancels loading or compilation.</param>
    /// <returns>The individual preload tasks for coordination by the caller.</returns>
    IEnumerable<Task> PreloadTemplates(
        ITemplateCache templateCache,
        CancellationToken cancellationToken = default);
}
