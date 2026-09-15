using System.Collections.Immutable;

namespace RazQL.Binding;

/// <summary>Configures trusted SQL fragments emitted by <see cref="IDataBinder{TData}.OrderBy{TEnum}"/>.</summary>
public class DataBinderOptions
{
    /// <summary>Gets the default mapping from sort directions to SQL fragments.</summary>
    public static readonly IReadOnlyDictionary<OrderByDirection, string> DefaultOrderByDirectionClause =
        new Dictionary<OrderByDirection, string>
        {
            { OrderByDirection.Asc, "ASC" },
            { OrderByDirection.Desc, "DESC" }
        }.ToImmutableDictionary();

    /// <summary>Gets the default mapping from null-placement choices to SQL fragments.</summary>
    public static readonly IReadOnlyDictionary<OrderByNulls, string> DefaultOrderByNullsClause =
        new Dictionary<OrderByNulls, string>
        {
            { OrderByNulls.First, "NULLS FIRST" },
            { OrderByNulls.Last, "NULLS LAST" }
        }.ToImmutableDictionary();

    /// <summary>Gets the shared default options instance.</summary>
    public static readonly DataBinderOptions Default = new();

    /// <summary>Gets or sets the mapping from sort directions to trusted SQL fragments.</summary>
    public IReadOnlyDictionary<OrderByDirection, string> OrderByDirectionClause { get; set; } = DefaultOrderByDirectionClause;

    /// <summary>Gets or sets the mapping from null-placement choices to trusted SQL fragments.</summary>
    public IReadOnlyDictionary<OrderByNulls, string> OrderByNullsClause { get; set; } = DefaultOrderByNullsClause;
}
