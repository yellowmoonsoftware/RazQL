using System.Linq.Expressions;

namespace RazQL.Binding;

/// <summary>Compiles and caches expressions used to read values from template models.</summary>
public interface IExpressionCache
{
    /// <summary>Returns a stable member-path name and compiled selector.</summary>
    /// <typeparam name="TModel">The source model type.</typeparam>
    /// <typeparam name="TValue">The selected value type.</typeparam>
    /// <param name="selector">The member or identity expression to cache.</param>
    /// <returns>The parameter base name and compiled selector.</returns>
    (string, Func<TModel, TValue>) GetMemberAndDelegate<TModel, TValue>(
        Expression<Func<TModel, TValue>> selector);

    /// <summary>Compiles an expression and reports whether it is an identity expression.</summary>
    /// <typeparam name="TModel">The input type.</typeparam>
    /// <typeparam name="TValue">The output type.</typeparam>
    /// <param name="expression">The expression to compile.</param>
    /// <returns>The compiled delegate and an identity-expression indicator.</returns>
    (Func<TModel, TValue> @delegate, bool isIdentity) UnwrapExpression<TModel, TValue>(
        Expression<Func<TModel, TValue>> expression);
}
