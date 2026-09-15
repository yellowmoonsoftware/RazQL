using System.Linq.Expressions;

namespace RazQL.Internal;

internal static class ExpressionAndTypeExtensions
{
    public static IEnumerable<MemberExpression> SelectMemberPath(this MemberExpression expression, Action<Expression?>? onRootExpression = null)
    {
        var path = new Stack<MemberExpression>();
        Expression? current = expression;

        while (current is MemberExpression currentMember)
        {
            path.Push(currentMember);
            current = currentMember.Expression;
        }

        onRootExpression?.Invoke(current);

        return path;
    }

    public static bool IsOrImplements<TInterface>(this Type? candidate) where TInterface : class
    {
        return IsOrImplements(candidate, typeof(TInterface));
    }

    public static bool IsOrImplements(this Type? candidate, Type? interfaceType)
    {
        if (candidate is null || interfaceType is null || !interfaceType.IsInterface)
        {
            return false;
        }

        if (!interfaceType.IsGenericTypeDefinition)
        {
            return interfaceType.IsAssignableFrom(candidate);
        }

        return candidate.IsGenericType &&
               candidate.GetGenericTypeDefinition() == interfaceType
               ||
               candidate.GetInterfaces().Any(type =>
                   type.IsGenericType &&
                   type.GetGenericTypeDefinition() == interfaceType);
    }
}
