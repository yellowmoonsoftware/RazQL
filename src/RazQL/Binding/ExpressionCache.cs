using System.Collections.Concurrent;
using System.Linq.Expressions;
using RazQL.Internal;

namespace RazQL.Binding;

/// <summary>Provides the default thread-safe cache for compiled model expressions.</summary>
public sealed class ExpressionCache : IExpressionCache
{
    private record CachedMemberSelector(string MemberName, Delegate CompiledSelector);

    private readonly ConcurrentDictionary<LambdaExpression, CachedMemberSelector> _selectorCache = new();

    /// <summary>Determines whether an expression returns its input parameter unchanged.</summary>
    /// <typeparam name="TObject">The expression input type.</typeparam>
    /// <typeparam name="TMember">The expression result type.</typeparam>
    /// <param name="expression">The expression to inspect.</param>
    /// <returns><see langword="true"/> when the expression is an identity expression.</returns>
    public static bool IsIdentity<TObject, TMember>(Expression<Func<TObject, TMember>>? expression) =>
        expression != null && expression.Body.Equals(expression.Parameters[0]);

    private static string GetName(LambdaExpression lambda)
    {
        if (lambda.Parameters.Count != 1)
        {
            throw new ArgumentException("Selector expression must declare exactly one parameter.", nameof(lambda));
        }

        return lambda.Body switch
        {
            MemberExpression member => string.Join("_",
                member.SelectMemberPath(root => Guard.ThrowIf(root, r => r != lambda.Parameters[0],
                        "Lambda member selector must be rooted in the model parameter.", nameof(lambda)))
                .Select(m => m.Member.Name)),
            ParameterExpression parameter when parameter == lambda.Parameters[0] => "this",
            ParameterExpression => throw new NotSupportedException("Parameter selector must reference the model parameter."),
            _ => throw new NotSupportedException($"Expression type '{lambda.Body.NodeType}' is not supported.")
        };
    }

    /// <inheritdoc />
    public (string, Func<TModel, TValue>) GetMemberAndDelegate<TModel, TValue>(Expression<Func<TModel, TValue>> selector)
    {
        var (name, compiledSelector) = _selectorCache.GetOrAdd(selector, s =>
        {
            var name = GetName(s);
            var @delegate = s.Compile();
            return new CachedMemberSelector(name, @delegate);
        });

        return (name, (Func<TModel, TValue>)compiledSelector);
    }

    /// <inheritdoc />
    public (Func<TModel, TValue> @delegate, bool isIdentity) UnwrapExpression<TModel, TValue>(Expression<Func<TModel, TValue>> expression)
    {
        var isIdentity = IsIdentity(expression);
        var @delegate = expression.Compile();
        return (@delegate, isIdentity);
    }
}
