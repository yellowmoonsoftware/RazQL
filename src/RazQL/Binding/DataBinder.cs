using System.Linq.Expressions;
using System.Text;
using RazQL.Internal;
using Dapper;

namespace RazQL.Binding;

/// <summary>Default implementation of template model inspection and SQL parameter binding.</summary>
/// <typeparam name="TModel">The type of criteria model being bound.</typeparam>
/// <param name="model">The criteria model.</param>
/// <param name="parameters">The Dapper parameter collection populated by binding operations.</param>
/// <param name="paramNameProvider">The parameter-name provider for this binding scope.</param>
/// <param name="exprCache">The cache used to compile selector expressions.</param>
/// <param name="options">Options controlling generated SQL fragments.</param>
public class DataBinder<TModel>(
    TModel model,
    DynamicParameters parameters,
    IParameterNameProvider paramNameProvider,
    IExpressionCache exprCache,
    DataBinderOptions options) : IDataBinder<TModel>
{
    /// <summary>Creates a binder using the default expression cache.</summary>
    /// <param name="model">The criteria model.</param>
    /// <param name="parameters">The Dapper parameter collection populated by binding operations.</param>
    /// <param name="paramNameProvider">The parameter-name provider for this binding scope.</param>
    /// <param name="options">Options controlling generated SQL fragments, or <see langword="null"/> for defaults.</param>
    public DataBinder(TModel model, DynamicParameters parameters, IParameterNameProvider paramNameProvider,
        DataBinderOptions? options = null) : this(model, parameters, paramNameProvider, new ExpressionCache(),
        options ?? DataBinderOptions.Default)
    {
    }

    /// <inheritdoc />
    public string BindAsArray<TValue>(Expression<Func<TModel, ICollection<TValue>?>> selector,
        NullCollectionBinding nullCollectionBinding = NullCollectionBinding.AsNull)
    {
        var (name, getter) = exprCache.GetMemberAndDelegate(selector);
        var paramName = paramNameProvider.GetStableName(nullCollectionBinding == NullCollectionBinding.AsEmptyArray
            ? $"{name}_orempty"
            : name);

        if (!parameters.ParameterNames.Contains(paramName))
        {
            var boundValue = getter(model)?.ToArray();
            parameters.Add(paramName,
                boundValue is null && nullCollectionBinding == NullCollectionBinding.AsEmptyArray ? [] : boundValue);
        }
        return $"@{paramName}";
    }

    /// <inheritdoc />
    public string Bind<TValue>(Expression<Func<TModel, TValue>> selector)
    {
        var (name, getter) = exprCache.GetMemberAndDelegate(selector);
        var paramName = paramNameProvider.GetStableName(name);
        if (!parameters.ParameterNames.Contains(paramName))
        {
            parameters.Add(paramName, getter(model));
        }
        return $"@{paramName}";
    }

    /// <inheritdoc />
    public string Bind<TValue, TTransform>(Expression<Func<TModel, TValue>> selector, Expression<Func<TValue, TTransform>> transformerExpr)
    {
        ArgumentNullException.ThrowIfNull(transformerExpr);

        var (name, getter) = exprCache.GetMemberAndDelegate(selector);
        var modelValue = getter(model);

        var (transformer, _) = exprCache.UnwrapExpression(transformerExpr);
        var paramValue = transformer(modelValue);
        var paramName = paramNameProvider.GetUniqueName($"{name}x");
        parameters.Add(paramName, paramValue);
        return $"@{paramName}";
    }

    /// <inheritdoc />
    public bool Test(Func<TModel, bool> evaluator) => evaluator(model);

    /// <inheritdoc />
    public bool IsNull<TValue>(Expression<Func<TModel, TValue>> selector)
    {
        var (_, getter) = exprCache.GetMemberAndDelegate(selector);
        return getter(model) is null;
    }

    /// <inheritdoc />
    public bool IsNullOrEmpty(Expression<Func<TModel, string?>> selector)
    {
        var (_, getter) =exprCache.GetMemberAndDelegate(selector);
        return string.IsNullOrEmpty(getter(model));
    }

    /// <inheritdoc />
    public bool IsNullOrEmpty<T>(Expression<Func<TModel, ICollection<T>?>> selector)
    {
        var (_, getter) = exprCache.GetMemberAndDelegate(selector);
        var collection = getter(model);
        return collection is null || collection.Count == 0;
    }

    /// <inheritdoc />
    public bool IsTrue(Expression<Func<TModel, bool?>> selector)
    {
        var (_, getter) = exprCache.GetMemberAndDelegate(selector);
        return getter(model) == true;
    }

    /// <inheritdoc />
    public bool IsFalse(Expression<Func<TModel, bool?>> selector)
    {
        return !IsTrue(selector);
    }

    /// <inheritdoc />
    public IEnumerable<(IDataBinder<TValue>, EnumeratingContext)> Select<TValue>(Expression<Func<TModel, ICollection<TValue>>> selectorExpr)
    {
        var (name, selector) = exprCache.GetMemberAndDelegate(selectorExpr);
        var collection = selector(model);
        var count = collection.Count;

        return collection
            .Select((value, idx) =>
            {
                IDataBinder<TValue> childBinder = new DataBinder<TValue>(value, parameters,
                    paramNameProvider.WithPrefix(name, new IParameterNameProvider.EnumeratingContext(idx)),
                    options);
                var context = new EnumeratingContext(idx, idx == 0, idx == count - 1, count);

                return  (childBinder, context);
            });
    }

    private IEnumerable<string> ToOrderByClauses<TEnum>(ICollection<OrderSpec<TEnum>> orderBySpec, Func<TEnum, string?> columnTargetProvider)
        where TEnum : struct, Enum
    {
        foreach (var spec in orderBySpec)
        {
            var colExpr = columnTargetProvider(spec.Column);
            if (colExpr is null)
                continue;

            var bld = new StringBuilder(colExpr);
            options.OrderByDirectionClause.DoIfExists(spec.Direction, (_, clause, b) => b.Append($" {clause}"), bld);
            options.OrderByNullsClause.DoIfExists(spec.NullsOrder, (_, clause, b) => b.Append($" {clause}"), bld);

            yield return bld.ToString();
        }
    }

    /// <inheritdoc />
    public string OrderBy<TEnum>(Func<TModel, ICollection<OrderSpec<TEnum>>?> selector, Func<TEnum, string?> columnTargetProvider) where TEnum : struct, Enum
    {
        var orderSpec = selector(model);
        if (orderSpec is null || orderSpec.Count == 0)
            return "";

        var orderByClauses = string.Join(",", ToOrderByClauses(orderSpec, columnTargetProvider));

        return string.IsNullOrWhiteSpace(orderByClauses) ? "" : $"ORDER BY {orderByClauses}";
    }

}
