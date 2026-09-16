namespace RazQL.Binding;

/// <summary>Configures SQL clause mappings for data binders.</summary>
public interface IDataBinderOptionsBuilder
{
    /// <summary>Sets the SQL fragment for one sort direction.</summary>
    /// <param name="direction">The sort direction to configure.</param>
    /// <param name="clause">The trusted SQL fragment.</param>
    /// <returns>This builder.</returns>
    IDataBinderOptionsBuilder WithOrderByDirectionClause(OrderByDirection direction, string clause);

    /// <summary>Adds or replaces the supplied sort-direction mappings.</summary>
    /// <param name="mapping">The mappings to apply.</param>
    /// <returns>This builder.</returns>
    IDataBinderOptionsBuilder WithOrderByDirection(IReadOnlyDictionary<OrderByDirection, string> mapping);

    /// <summary>Sets the SQL fragment for one null-placement choice.</summary>
    /// <param name="nullsOrder">The null-placement choice to configure.</param>
    /// <param name="clause">The trusted SQL fragment.</param>
    /// <returns>This builder.</returns>
    IDataBinderOptionsBuilder WithOrderByNullsClause(OrderByNulls nullsOrder, string clause);

    /// <summary>Adds or replaces the supplied null-placement mappings.</summary>
    /// <param name="mapping">The mappings to apply.</param>
    /// <returns>This builder.</returns>
    IDataBinderOptionsBuilder WithOrderByNulls(IReadOnlyDictionary<OrderByNulls, string> mapping);

    /// <summary>Replaces all mappings with those from an existing options value.</summary>
    /// <param name="options">The options whose mappings are applied.</param>
    /// <returns>This builder.</returns>
    IDataBinderOptionsBuilder WithOptions(DataBinderOptions options);
}
