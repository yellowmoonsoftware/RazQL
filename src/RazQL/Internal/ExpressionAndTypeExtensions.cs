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

    extension(Type? candidate)
    {
        public bool IsOrImplements<TInterface>() where TInterface : class
        {
            return candidate.IsOrImplements(typeof(TInterface));
        }

        public bool IsOrImplements(Type? interfaceType)
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
}
