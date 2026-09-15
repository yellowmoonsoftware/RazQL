namespace RazQL.Binding;

/// <summary>Provides identity-model shortcuts for use in Razor query templates.</summary>
public static class DataBinderExtensions
{
    /// <summary>Binds the entire model as a SQL parameter.</summary>
    /// <typeparam name="TData">The model type.</typeparam>
    /// <param name="binder">The template data binder.</param>
    /// <returns>The SQL parameter reference, including the <c>@</c> prefix.</returns>
    public static string Bind<TData>(this IDataBinder<TData> binder) => binder.Bind(m => m);

    /// <summary>Determines whether the entire model is <see langword="null"/>.</summary>
    /// <typeparam name="TData">The model type.</typeparam>
    /// <param name="binder">The template data binder.</param>
    /// <returns><see langword="true"/> when the model is <see langword="null"/>.</returns>
    public static bool IsNull<TData>(this IDataBinder<TData> binder) => binder.IsNull(m => m);

    // Disable to allow support for both nullable and non-nullable types
    #nullable disable annotations
    /// <summary>Binds the entire collection model as an array-valued SQL parameter.</summary>
    /// <typeparam name="TValue">The collection element type.</typeparam>
    /// <param name="binder">The template data binder.</param>
    /// <param name="nullCollectionBinding">The value to bind when the model is <see langword="null"/>.</param>
    /// <returns>The SQL parameter reference, including the <c>@</c> prefix.</returns>
    public static string BindAsArray<TValue>(
        this IDataBinder<ICollection<TValue>> binder,
        NullCollectionBinding nullCollectionBinding = NullCollectionBinding.AsNull
    ) => binder.BindAsArray(m => m, nullCollectionBinding);

    /// <summary>Determines whether the string model is <see langword="null"/> or empty.</summary>
    /// <param name="binder">The template data binder.</param>
    /// <returns><see langword="true"/> when the model is <see langword="null"/> or empty.</returns>
    public static bool IsNullOrEmpty(this IDataBinder<string> binder) => binder.IsNullOrEmpty(m => m);

    /// <summary>Determines whether the collection model is <see langword="null"/> or empty.</summary>
    /// <typeparam name="TValue">The collection element type.</typeparam>
    /// <param name="binder">The template data binder.</param>
    /// <returns><see langword="true"/> when the model is <see langword="null"/> or empty.</returns>
    public static bool IsNullOrEmpty<TValue>(this IDataBinder<ICollection<TValue>> binder) => binder.IsNullOrEmpty(m => m);
    #nullable restore annotations

    /// <summary>Creates child binders for every item in the collection model.</summary>
    /// <typeparam name="TValue">The collection element type.</typeparam>
    /// <param name="binder">The template data binder.</param>
    /// <returns>Child binders paired with their position within the collection.</returns>
    public static IEnumerable<(IDataBinder<TValue>, EnumeratingContext)> Select<TValue>(
        this IDataBinder<ICollection<TValue>> binder) => binder.Select(m => m);
}
