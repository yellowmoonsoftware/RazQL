using System.Linq.Expressions;
using System.Reflection;

namespace RazQL.Internal;

internal static class ExpressionAndTypeExtensions
{
    public static PropertyInfo GetPropertyInfo<TInstance, TProperty>(this Expression<Func<TInstance, TProperty>> propertySelector)
    {
        var memberExp = propertySelector.Body as MemberExpression;
        var propertyInfo = memberExp?.Member as PropertyInfo;
        if (propertyInfo == null)
            throw new ArgumentException("The provided expression does not represent a property.");

        return propertyInfo;
    }
    
    public static (PropertyInfo, MethodInfo) GetPropertyAddMethod<TInstance, TProperty>(this Expression<Func<TInstance, ICollection<TProperty>>> propertySelector)
    {
        var propertyInfo = GetPropertyInfo(propertySelector);

        var methodInfo =
            propertyInfo.PropertyType.FindMethod("Add", BindingFlags.Instance | BindingFlags.Public,
                [typeof(TProperty)]);
        
        if (methodInfo == null)
            throw new ArgumentException("The provided property is not a collection with an 'Add' method.");

        return (propertyInfo, methodInfo);
    }

    public static Action<TInstance, TProperty> GetPropertySetter<TInstance, TProperty>(this Expression<Func<TInstance, TProperty>> propertySelector)
    {
        var memberExp = propertySelector.Body as MemberExpression;
        var propertyInfo = memberExp?.Member as PropertyInfo;
        
        if (propertyInfo == null || propertyInfo.SetMethod == null)
            throw new ArgumentException("The provided expression does not represent a property with a valid setter.");
        
        var instance = Expression.Parameter(typeof(TInstance), "instance");
        var prop = Expression.Parameter(typeof(TProperty), "value");
        var propertySetExpression = Expression.Call(instance, propertyInfo.SetMethod, prop);
        var compile = Expression.Lambda<Action<TInstance, TProperty>>(propertySetExpression, instance, prop).Compile();
        
        return compile;
    }
    
    public static Func<TInstance, TProperty> GetPropertyGetter<TInstance, TProperty>(this Expression<Func<TInstance, TProperty>> propertySelector)
    {
        var memberExp = propertySelector.Body as MemberExpression;
        var propertyInfo = memberExp?.Member as PropertyInfo;
        
        if (propertyInfo == null || propertyInfo.GetMethod == null)
            throw new ArgumentException("The provided expression does not represent a property with a valid getter."); 

        return propertySelector.Compile();
    }
    
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

        public MethodInfo? FindMethod(string name)
        {
            return candidate.FindMethod(t => t.GetMethod(name));
        }

        public MethodInfo? FindMethod(string name, BindingFlags bindingAttr)
        {
            return candidate.FindMethod(t => t.GetMethod(name, bindingAttr));
        }

        public MethodInfo? FindMethod(string name, BindingFlags bindingAttr, Type[] types)
        {
            return candidate.FindMethod(t => t.GetMethod(name, bindingAttr, types));
        }

        private MethodInfo? FindMethod(Func<Type, MethodInfo?> getMethod)
        {
            if (candidate is null)
            {
                return null;
            }

            return getMethod(candidate) ?? candidate.GetInterfaces().Select(getMethod).FirstOrDefault(m => m != null);
        }
    }
}
