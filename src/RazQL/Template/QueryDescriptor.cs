using System.Collections;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using RazQL.Internal;

namespace RazQL.Template;

/// <summary>
/// Provides non-generic metadata for a mapper query and the conventions used to locate its template.
/// </summary>
public abstract record QueryDescriptor
{
    const string QueryMethodAsyncSuffix = "Async";

    private static readonly ReadOnlySet<Type> EnumerableAllowList = new HashSet<Type>
    {
        typeof(string),
        typeof(byte[])
    }.AsReadOnly();

    /// <summary>Initializes the metadata shared by typed query descriptors.</summary>
    /// <param name="MapperType">The decorated mapper interface.</param>
    /// <param name="QueryMethod">The mapped query method.</param>
    /// <param name="CriteriaType">The criteria type declared by the method.</param>
    /// <param name="ResultType">The value type returned by the query method's task.</param>
    protected QueryDescriptor(
        Type MapperType,
        MethodInfo QueryMethod,
        Type CriteriaType,
        Type ResultType)
    {
        this.MapperType = MapperType;
        this.QueryMethod = QueryMethod;
        this.CriteriaType = CriteriaType;
        this.ResultType = ResultType;

        var mapperTemplateSource = GetTemplateSourceAttribute(MapperType);
        var methodTemplateSource = GetTemplateSourceAttribute(QueryMethod);
        var mapperExternalTemplateSource = mapperTemplateSource as RazQLTemplateSourceAttribute;
        var methodExternalTemplateSource = methodTemplateSource as RazQLQueryTemplateSourceAttribute;

        QuerySourceLoaderType = methodTemplateSource?.LoaderType ??
                                mapperTemplateSource?.LoaderType ??
                                typeof(ResourceTemplateSourceLoader);
        TemplateLocation = NormalizeTemplateLocation(methodExternalTemplateSource?.TemplateLocation) ??
                           NormalizeTemplateLocation(mapperExternalTemplateSource?.TemplateLocation);
        TemplateName = methodExternalTemplateSource?.TemplateName;
    }

    /// <summary>Gets the decorated mapper interface.</summary>
    public Type MapperType { get; }

    /// <summary>Gets the mapper method represented by this descriptor.</summary>
    public MethodInfo QueryMethod { get; }

    /// <summary>Gets the method's criteria type.</summary>
    public Type CriteriaType { get; }

    /// <summary>Gets the value type returned by the query method's task, including a sequence type when applicable.</summary>
    public Type ResultType { get; }

    /// <summary>Gets the concrete source-loader type selected for the query.</summary>
    public Type QuerySourceLoaderType { get; }

    /// <summary>Gets the optional normalized location inserted before the conventional <c>SqlTemplates</c> path.</summary>
    public string? TemplateLocation { get; }

    /// <summary>Gets the explicit template name, or <see langword="null"/> when method-name conventions apply.</summary>
    public string? TemplateName { get; }

    /// <summary>Gets mapper-group names in convention matching order.</summary>
    /// <returns>The configured name, or conventional mapper interface name candidates.</returns>
    public string[] GetQueryGroupCandidates() =>
        MapperType.GetCustomAttribute<RazQLMapperAttribute>(inherit: false)?.Name != null
        ? [MapperType.GetCustomAttribute<RazQLMapperAttribute>(inherit: false)?.Name!]
        : Regex.IsMatch(MapperType.Name, @"^I[A-Z]")
            ? [MapperType.Name[1..], MapperType.Name]
            : [MapperType.Name];

    /// <summary>Gets query names in convention matching order.</summary>
    /// <returns>The configured template name, or method names with and without a conventional <c>Async</c> suffix.</returns>
    public string[] GetQueryNameCandidates() =>
        TemplateName is not null
        ? [TemplateName]
        : QueryMethod.Name.EndsWith(QueryMethodAsyncSuffix)
            ? [QueryMethod.Name[..^QueryMethodAsyncSuffix.Length], QueryMethod.Name]
            : [QueryMethod.Name];

    /// <summary>Creates a descriptor for a mapper method returning a sequence.</summary>
    /// <typeparam name="TMapper">The decorated, closed mapper interface.</typeparam>
    /// <typeparam name="TCriteria">The method criteria type.</typeparam>
    /// <typeparam name="TElement">The mapped sequence element type.</typeparam>
    /// <param name="expression">An expression that directly selects the mapper method group.</param>
    /// <returns>A validated, strongly typed query descriptor.</returns>
    /// <exception cref="ArgumentException">The mapper, method, parameter, result, or expression shape is invalid.</exception>
    public static QueryDescriptor<TMapper, TCriteria, IEnumerable<TElement>> ForExpression<TMapper, TCriteria, TElement>(
        Expression<Func<TMapper, Func<TCriteria, CancellationToken, Task<IEnumerable<TElement>>>>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        return ForMethodGroup<TMapper, TCriteria, IEnumerable<TElement>>(expression, expectsSequence: true);
    }

    /// <summary>Creates a descriptor for a mapper method returning zero or one value.</summary>
    /// <typeparam name="TMapper">The decorated, closed mapper interface.</typeparam>
    /// <typeparam name="TCriteria">The method criteria type.</typeparam>
    /// <typeparam name="TResult">The query method's task result type.</typeparam>
    /// <param name="expression">An expression that directly selects the mapper method group.</param>
    /// <returns>A validated, strongly typed query descriptor.</returns>
    /// <exception cref="ArgumentException">The mapper, method, parameter, result, or expression shape is invalid.</exception>
    public static QueryDescriptor<TMapper, TCriteria, TResult> ForExpression<TMapper, TCriteria, TResult>(
        Expression<Func<TMapper, Func<TCriteria, CancellationToken, Task<TResult>>>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        return ForMethodGroup<TMapper, TCriteria, TResult>(expression, expectsSequence: false);
    }

    private static QueryDescriptor<TMapper, TCriteria, TResult> ForMethodGroup<TMapper, TCriteria, TResult>(
        LambdaExpression expression, bool expectsSequence)
    {
        var mapperType = typeof(TMapper);
        ValidateMapperType(mapperType);

        if (expression.Parameters.Count == 1 &&
            expression.Body is UnaryExpression
            {
                NodeType: ExpressionType.Convert,
                Operand: MethodCallExpression methodCallExpression
            } &&
            methodCallExpression.Method.DeclaringType == typeof(MethodInfo) &&
            methodCallExpression.Method.Name == nameof(MethodInfo.CreateDelegate) &&
            methodCallExpression.Object is ConstantExpression { Value: MethodInfo targetMethod } &&
            methodCallExpression.Arguments.Count == 2 &&
            methodCallExpression.Arguments[1] == expression.Parameters[0] &&
            !targetMethod.IsStatic)
        {
            var (criteriaType, resultType, returnsSequence) = ForMethod(mapperType, targetMethod);
            if (criteriaType != typeof(TCriteria) || resultType != typeof(TResult) || returnsSequence != expectsSequence)
            {
                throw new ArgumentException(
                    "Expression method signature does not match the descriptor criteria, result type, or sequence shape.",
                    nameof(expression));
            }

            return new QueryDescriptor<TMapper, TCriteria, TResult>(targetMethod);
        }

        throw new ArgumentException(
            "Expression must select an instance mapper method directly from the mapper parameter.",
            nameof(expression));
    }

    private static void ValidateMapperType(Type mapperType)
    {
        if (!mapperType.IsInterface)
        {
            throw new ArgumentException($"Mapper type {mapperType.Name} must be an interface.", nameof(mapperType));
        }

        if (mapperType.ContainsGenericParameters)
        {
            throw new ArgumentException($"Mapper type {mapperType.Name} must be a closed type.", nameof(mapperType));
        }

        if (mapperType.GetCustomAttribute<RazQLMapperAttribute>(inherit: false) is null)
        {
            throw new ArgumentException(
                $"Mapper interface {mapperType.Name} must be decorated with {nameof(RazQLMapperAttribute)}.",
                nameof(mapperType));
        }
    }

    private static (Type CriteriaType, Type ResultType, bool ReturnsSequence) ForMethod(
        Type mapperType, MethodInfo queryMethod)
    {
        ArgumentNullException.ThrowIfNull(queryMethod);

        var declaringType = queryMethod.DeclaringType;
        if (declaringType is null)
        {
            throw new ArgumentException($"Query Method {queryMethod.Name} must have a declaring type",
                nameof(queryMethod));
        }

        if (!declaringType.IsInterface || !declaringType.IsAssignableFrom(mapperType))
        {
            throw new ArgumentException(
                $"Query method {queryMethod.Name} must be declared by mapper interface {mapperType.Name} or one of its base interfaces.",
                nameof(queryMethod));
        }

        var criteriaType = GetCriteriaType(queryMethod);

        var (resultType, returnsSequence) = GetResultType(queryMethod);

        return (criteriaType, resultType, returnsSequence);
    }

    private static Type GetCriteriaType(MethodInfo queryMethod)
    {
        var parameters = queryMethod.GetParameters();
        if (parameters is not [var criteriaParam, var tokenParam])
        {
            throw new ArgumentException($"Query method {queryMethod.Name} must have exactly two parameters",
                nameof(queryMethod));
        }

        if (tokenParam.ParameterType != typeof(CancellationToken))
        {
            throw new ArgumentException(
                $"Query method {queryMethod.Name} must specify CancellationToken as second parameter",
                nameof(queryMethod));
        }

        if (criteriaParam.ParameterType == typeof(CancellationToken))
        {
            throw new ArgumentException(
                $"Query method {queryMethod.Name} must specify criteria type as a first parameter and CancellationToken as a second parameter",
                nameof(queryMethod));
        }

        return criteriaParam.ParameterType;
    }

    private static (Type ResultType, bool ReturnsSequence) GetResultType(MethodInfo queryMethod)
    {
        if (!queryMethod.ReturnType.IsGenericType ||
            queryMethod.ReturnType.GetGenericTypeDefinition() != typeof(Task<>))
        {
            throw new ArgumentException($"Query method {queryMethod.Name} must return Task<T>", nameof(queryMethod));
        }

        var taskResultType = queryMethod.ReturnType.GetGenericArguments()[0];

        // Validate the mapped element, but retain the complete Task<T> value type.
        if (taskResultType.IsGenericType && taskResultType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            var mappedResultType = taskResultType.GetGenericArguments()[0];
            ValidateMappedResultType(queryMethod, mappedResultType);

            return (taskResultType, true);
        }

        ValidateMappedResultType(queryMethod, taskResultType);

        return (taskResultType, false);
    }

    private static void ValidateMappedResultType(MethodInfo queryMethod, Type mappedResultType)
    {
        if (IsTaskLike(mappedResultType))
        {
            throw new ArgumentException(
                $"Mapped result type for query method {queryMethod.Name} cannot be Task, Task<T>, ValueTask, or ValueTask<T>",
                nameof(queryMethod));
        }

        // Explicitly allowed types that implement IEnumerable
        if (EnumerableAllowList.Contains(mappedResultType))
        {
            return;
        }

        // IEnumerable-implementing types are not supported (except for explicitly allowed types)
        if (mappedResultType.IsOrImplements<IEnumerable>())
        {
            throw new ArgumentException(
                $"Query method {queryMethod.Name} must return an IEnumerable type in the form of Task<IEnumerable<T>>.  Types implementing IEnumerable<>/IEnumerable are not supported",
                nameof(queryMethod));
        }

        // IAsyncEnumerable<T> is not supported
        if (mappedResultType.IsOrImplements(typeof(IAsyncEnumerable<>)))
        {
            throw new ArgumentException(
                $"IAsyncEnumerable<T> and implementing types are not currently supported as a return type for query method {queryMethod.Name}",
                nameof(queryMethod));
        }

        // IAsyncEnumerator<T> is not supported
        if (mappedResultType.IsOrImplements(typeof(IAsyncEnumerator<>)))
        {
            throw new ArgumentException(
                $"IAsyncEnumerator<T> and implementing types are not currently supported as a return type for query method {queryMethod.Name}",
                nameof(queryMethod));
        }

        // IEnumerator/IEnumerator<T> is not supported
        if (mappedResultType.IsOrImplements<IEnumerator>())
        {
            throw new ArgumentException(
                $"IEnumerator implementing types are not currently supported as a return type for query method {queryMethod.Name}",
                nameof(queryMethod));
        }
    }

    private static bool IsTaskLike(Type type)
    {
        return typeof(Task).IsAssignableFrom(type) ||
               type == typeof(ValueTask) ||
               type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTask<>);
    }

    private static TemplateSourceLoaderAttribute? GetTemplateSourceAttribute(MemberInfo member)
    {
        var attributes = member
            .GetCustomAttributes<TemplateSourceLoaderAttribute>(inherit: false)
            .ToArray();

        if (attributes.Length > 1)
        {
            var memberDescription = member switch
            {
                Type type => $"mapper type {type.FullName}",
                MethodInfo method => $"mapper method {method.DeclaringType?.FullName}.{method.Name}",
                _ => $"mapper member {member.Name}"
            };

            throw new InvalidOperationException(
                $"More than one {nameof(TemplateSourceLoaderAttribute)} or derived attribute is applied to {memberDescription}.");
        }

        return attributes.SingleOrDefault();
    }

    private static string? NormalizeTemplateLocation(string? templateLocation)
    {
        return string.IsNullOrWhiteSpace(templateLocation)
            ? null
            : templateLocation.Trim();
    }
}

/// <summary>Describes a mapper query with compile-time mapper, criteria, and task result types.</summary>
/// <typeparam name="TMapper">The decorated mapper interface that owns the query.</typeparam>
/// <typeparam name="TCriteria">The query criteria type.</typeparam>
/// <typeparam name="TResult">The query method's task result type.</typeparam>
public sealed record QueryDescriptor<TMapper, TCriteria, TResult> : QueryDescriptor
{
    internal QueryDescriptor(MethodInfo queryMethod)
        : base(typeof(TMapper), queryMethod, typeof(TCriteria), typeof(TResult))
    {
    }
}
