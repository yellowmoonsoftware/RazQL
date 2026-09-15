using Microsoft.CodeAnalysis;
using RazQL.Generators.Models;

namespace RazQL.Generators.Validation;

internal static class MapperMethodValidator
{
    public static IEnumerable<Diagnostic> Validate(
        IMethodSymbol method,
        KnownTypeSymbols knownTypes)
    {
        if (method.IsStatic)
        {
            yield return CreateDiagnostic(RazQLDiagnostics.StaticMapperMethod, method);
        }

        if (method.IsGenericMethod)
        {
            yield return CreateDiagnostic(RazQLDiagnostics.GenericMapperMethod, method);
        }

        if (!method.IsAbstract)
        {
            yield return CreateDiagnostic(RazQLDiagnostics.ImplementedMapperMethod, method);
        }

        if (!method.ReturnsByRef &&
            !method.ReturnsByRefReadonly &&
            method.ReturnType is INamedTypeSymbol returnType &&
            SymbolEqualityComparer.Default.Equals(returnType.OriginalDefinition, knownTypes.GenericTask))
        {
            var resultDiagnostic = MapperMethodResultValidator.Validate(
                method,
                returnType.TypeArguments[0],
                knownTypes);

            if (resultDiagnostic is not null)
            {
                yield return resultDiagnostic;
            }
        }
        else
        {
            yield return CreateDiagnostic(RazQLDiagnostics.InvalidMapperMethodReturnType, method);
        }

        if (!HasValidParameterShape(method, knownTypes.CancellationToken))
        {
            yield return CreateDiagnostic(RazQLDiagnostics.InvalidMapperMethodParameterShape, method);
        }
        else if (!IsValidGenericTypeArgument(method.Parameters[0].Type))
        {
            yield return Diagnostic.Create(
                RazQLDiagnostics.InvalidMapperCriteriaType,
                method.Parameters[0].Locations.FirstOrDefault() ?? method.Locations.FirstOrDefault(),
                method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
        }
    }

    private static bool IsValidGenericTypeArgument(ITypeSymbol type)
    {
        return !type.IsRefLikeType &&
               type.TypeKind is not (TypeKind.Pointer or TypeKind.FunctionPointer);
    }

    private static bool HasValidParameterShape(
        IMethodSymbol method,
        INamedTypeSymbol? cancellationTokenType)
    {
        if (method.Parameters.Length != 2)
        {
            return false;
        }

        var criteriaParameter = method.Parameters[0];
        var cancellationTokenParameter = method.Parameters[1];

        return criteriaParameter.RefKind == RefKind.None &&
               cancellationTokenParameter.RefKind == RefKind.None &&
               !SymbolEqualityComparer.Default.Equals(criteriaParameter.Type, cancellationTokenType) &&
               SymbolEqualityComparer.Default.Equals(cancellationTokenParameter.Type, cancellationTokenType);
    }

    private static Diagnostic CreateDiagnostic(DiagnosticDescriptor descriptor, IMethodSymbol method)
    {
        return Diagnostic.Create(
            descriptor,
            method.Locations.FirstOrDefault(),
            method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
    }
}
