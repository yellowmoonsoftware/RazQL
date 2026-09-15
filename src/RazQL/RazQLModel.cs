using System.Linq.Expressions;
using RazorEngineCore;
using RazQL.Binding;

namespace RazQL;

/// <summary>Base class required for every Razor template compiled by RazQL.</summary>
public abstract class RazQLModel : RazorEngineTemplateBase
{
    /// <summary>Provides the standard SQL <c>LIKE</c> transformation that surrounds text with percent wildcards.</summary>
    protected static readonly Expression<Func<string, string>> Like = s => $"%{s}%";
}

/// <summary>Provides a typed data binder as the model available to a RazQL template.</summary>
/// <typeparam name="TModel">The mapper method's criteria type.</typeparam>
public class RazQLModel<TModel> : RazQLModel, IDataBinder<TModel>
{
    /// <summary>Gets or sets the data binder exposed as <c>Model</c> within the Razor template.</summary>
    public new required IDataBinder<TModel> Model { get; set; }

    /// <inheritdoc />
    public string BindAsArray<TValue>(Expression<Func<TModel, ICollection<TValue>?>> selector,
        NullCollectionBinding nullCollectionBinding = NullCollectionBinding.AsNull) =>
        Model.BindAsArray(selector, nullCollectionBinding);

    /// <inheritdoc />
    public string Bind<TValue>(Expression<Func<TModel, TValue>> selector) => Model.Bind(selector);

    /// <inheritdoc />
    public string Bind<TValue, TTransform>(Expression<Func<TModel, TValue>> selector,
        Expression<Func<TValue, TTransform>> transformerExpr) => Model.Bind(selector, transformerExpr);

    /// <inheritdoc />
    public bool Test(Func<TModel, bool> evaluator) =>  Model.Test(evaluator);

    /// <inheritdoc />
    public bool IsNull<TValue>(Expression<Func<TModel, TValue>> selector) => Model.IsNull(selector);

    /// <inheritdoc />
    public bool IsNullOrEmpty(Expression<Func<TModel, string?>> selector) => Model.IsNullOrEmpty(selector);

    /// <inheritdoc />
    public bool IsNullOrEmpty<T>(Expression<Func<TModel, ICollection<T>?>> selector) => Model.IsNullOrEmpty(selector);

    /// <inheritdoc />
    public bool IsTrue(Expression<Func<TModel, bool?>> selector) => Model.IsTrue(selector);

    /// <inheritdoc />
    public bool IsFalse(Expression<Func<TModel, bool?>> selector) => Model.IsFalse(selector);

    /// <inheritdoc />
    public IEnumerable<(IDataBinder<TValue>, EnumeratingContext)> Select<TValue>(
        Expression<Func<TModel, ICollection<TValue>>> selector) => Model.Select(selector);

    /// <inheritdoc />
    public string OrderBy<TEnum>(Func<TModel, ICollection<OrderSpec<TEnum>>?> selector,
        Func<TEnum, string?> columnTargetProvider) where TEnum : struct, Enum =>
        Model.OrderBy(selector, columnTargetProvider);
}
