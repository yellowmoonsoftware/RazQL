using Microsoft.CodeAnalysis;

namespace RazQL.Generators.Internal;

internal static class AttributeDataExtensions
{
    public static string? GetNamedString(this AttributeData attribute, string name)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key == name)
            {
                return argument.Value.Value as string;
            }
        }

        return null;
    }

    public static Location? GetApplicationLocation(this AttributeData attribute)
    {
        return attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation();
    }
}
