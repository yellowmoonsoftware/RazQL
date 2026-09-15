using System.Linq.Expressions;

namespace RazQL.Binding;

/// <summary>
/// Exposes safe SQL-template helpers for inspecting a criteria model and adding values to the query parameter set.
/// </summary>
/// <typeparam name="TData">The type of value available to the template.</typeparam>
public interface IDataBinder<TData>
{
    /// <summary>Binds a selected collection as an array-valued SQL parameter.</summary>
    /// <typeparam name="TValue">The collection element type.</typeparam>
    /// <param name="selector">An expression selecting the collection to bind.</param>
    /// <param name="nullCollectionBinding">The value to bind when the selected collection is <see langword="null"/>.</param>
    /// <returns>The SQL parameter reference, including the <c>@</c> prefix.</returns>
    string BindAsArray<TValue>(Expression<Func<TData, ICollection<TValue>?>> selector,
        NullCollectionBinding nullCollectionBinding = NullCollectionBinding.AsNull);

    /// <summary>Binds a selected value as a SQL parameter.</summary>
    /// <typeparam name="TValue">The selected value type.</typeparam>
    /// <param name="selector">An expression selecting the value to bind.</param>
    /// <returns>The SQL parameter reference, including the <c>@</c> prefix.</returns>
    string Bind<TValue>(Expression<Func<TData, TValue>> selector);

    /// <summary>Transforms a selected value and binds the result as a uniquely named SQL parameter.</summary>
    /// <typeparam name="TValue">The selected value type.</typeparam>
    /// <typeparam name="TTransform">The transformed parameter type.</typeparam>
    /// <param name="selector">An expression selecting the source value.</param>
    /// <param name="transformerExpr">An expression that transforms the selected value before binding.</param>
    /// <returns>The SQL parameter reference, including the <c>@</c> prefix.</returns>
    string Bind<TValue, TTransform>(Expression<Func<TData, TValue>> selector, Expression<Func<TValue, TTransform>>
        transformerExpr);

    /// <summary>Evaluates a predicate against the template model.</summary>
    /// <param name="evaluator">The predicate to evaluate.</param>
    /// <returns><see langword="true"/> when the predicate succeeds.</returns>
    bool Test(Func<TData, bool> evaluator);

    /// <summary>Determines whether a selected value is <see langword="null"/>.</summary>
    /// <typeparam name="TValue">The selected value type.</typeparam>
    /// <param name="selector">An expression selecting the value.</param>
    /// <returns><see langword="true"/> when the selected value is <see langword="null"/>.</returns>
    bool IsNull<TValue>(Expression<Func<TData, TValue>> selector);

    /// <summary>Determines whether a selected string is <see langword="null"/> or empty.</summary>
    /// <param name="selector">An expression selecting the string.</param>
    /// <returns><see langword="true"/> when the selected string is <see langword="null"/> or empty.</returns>
    bool IsNullOrEmpty(Expression<Func<TData, string?>> selector);

    /// <summary>Determines whether a selected collection is <see langword="null"/> or empty.</summary>
    /// <typeparam name="T">The collection element type.</typeparam>
    /// <param name="selector">An expression selecting the collection.</param>
    /// <returns><see langword="true"/> when the selected collection is <see langword="null"/> or empty.</returns>
    bool IsNullOrEmpty<T>(Expression<Func<TData, ICollection<T>?>> selector);

    /// <summary>Determines whether a selected nullable Boolean is explicitly <see langword="true"/>.</summary>
    /// <param name="selector">An expression selecting the Boolean value.</param>
    /// <returns><see langword="true"/> only when the selected value is <see langword="true"/>.</returns>
    bool IsTrue(Expression<Func<TData, bool?>> selector);

    /// <summary>Determines whether a selected nullable Boolean is not explicitly <see langword="true"/>.</summary>
    /// <param name="selector">An expression selecting the Boolean value.</param>
    /// <returns><see langword="true"/> when the selected value is <see langword="false"/> or <see langword="null"/>.</returns>
    bool IsFalse(Expression<Func<TData, bool?>> selector);

    /// <summary>Creates child binders for the items in a selected collection.</summary>
    /// <typeparam name="TValue">The collection element type.</typeparam>
    /// <param name="selector">An expression selecting the collection.</param>
    /// <returns>Child binders paired with their position within the collection.</returns>
    IEnumerable<(IDataBinder<TValue>, EnumeratingContext)> Select<TValue>(Expression<Func<TData, ICollection<TValue>>> selector);

    /// <summary>Builds a validated <c>ORDER BY</c> clause from enum-based sort specifications.</summary>
    /// <typeparam name="TEnum">The enum used to identify permitted columns or expressions.</typeparam>
    /// <param name="selector">A function selecting the requested ordering.</param>
    /// <param name="columnTargetProvider">Maps each enum value to a trusted SQL column or expression.</param>
    /// <returns>An <c>ORDER BY</c> clause, or an empty string when no valid ordering is supplied.</returns>
    string OrderBy<TEnum>(Func<TData, ICollection<OrderSpec<TEnum>>?> selector,
        Func<TEnum, string?> columnTargetProvider) where TEnum : struct, Enum;
}
