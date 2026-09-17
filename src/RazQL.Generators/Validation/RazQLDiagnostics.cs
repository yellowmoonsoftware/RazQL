using Microsoft.CodeAnalysis;

namespace RazQL.Generators.Validation;

internal static class RazQLDiagnostics
{
    private const string HelpLinkBase =
        "https://github.com/yellowmoonsoftware/RazQL/blob/main/docs/diagnostics.md#";

    private static DiagnosticDescriptor Create(string id, string title, string messageFormat) =>
        new(
            id: id,
            title: title,
            messageFormat: messageFormat,
            category: "RazQL",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            helpLinkUri: HelpLinkBase + id.ToLowerInvariant());

    public static readonly DiagnosticDescriptor OpenGenericMapper = Create(
        id: "RAZQL001",
        title: "Mapper interfaces must be closed",
        messageFormat: "Mapper interface '{0}' cannot declare type parameters");

    public static readonly DiagnosticDescriptor StaticMapperMethod = Create(
        id: "RAZQL002",
        title: "Mapper methods must be instance methods",
        messageFormat: "Mapper method '{0}' must be an instance method");

    public static readonly DiagnosticDescriptor GenericMapperMethod = Create(
        id: "RAZQL003",
        title: "Mapper methods cannot be generic",
        messageFormat: "Mapper method '{0}' cannot declare type parameters");

    public static readonly DiagnosticDescriptor ImplementedMapperMethod = Create(
        id: "RAZQL004",
        title: "Mapper methods cannot provide implementations",
        messageFormat: "Mapper method '{0}' cannot provide a default implementation");

    public static readonly DiagnosticDescriptor InvalidMapperMethodReturnType = Create(
        id: "RAZQL005",
        title: "Mapper methods must return Task<T> by value",
        messageFormat: "Mapper method '{0}' must return System.Threading.Tasks.Task<T> by value");

    public static readonly DiagnosticDescriptor InvalidMapperMethodParameterShape = Create(
        id: "RAZQL006",
        title: "Mapper methods must use the supported parameter shape",
        messageFormat: "Mapper method '{0}' must declare one criteria parameter followed by System.Threading.CancellationToken");

    public static readonly DiagnosticDescriptor UnsupportedMapperMethodResultType = Create(
        id: "RAZQL007",
        title: "Mapper method result type is not supported",
        messageFormat: "Mapper method '{0}' has unsupported result type '{1}': {2}");

    public static readonly DiagnosticDescriptor DuplicateMapperQueryName = Create(
        id: "RAZQL008",
        title: "Mapper query name candidates must be unique",
        messageFormat: "Mapper method '{0}' produces query name candidate(s) [{1}] also produced by another method in mapper '{2}'");

    public static readonly DiagnosticDescriptor InvalidAttributeName = Create(
        id: "RAZQL009",
        title: "RazQL attribute names cannot be blank",
        messageFormat: "Name specified by {0} on '{1}' cannot be empty or whitespace");

    public static readonly DiagnosticDescriptor InvalidTemplateSourceLoaderType = Create(
        id: "RAZQL010",
        title: "Template source loader type is invalid",
        messageFormat: "Template source loader '{0}' selected for '{1}' must be a concrete, closed implementation of RazQL.Template.ITemplateSourceLoader");

    public static readonly DiagnosticDescriptor NonPublicMapperMember = Create(
        id: "RAZQL011",
        title: "Mapper members must be public",
        messageFormat: "Mapper member '{0}' on '{1}' must be public");

    public static readonly DiagnosticDescriptor UnsupportedMapperMember = Create(
        id: "RAZQL012",
        title: "Mapper interfaces can only contain methods",
        messageFormat: "Mapper interface '{0}' contains unsupported {1} member '{2}'; only methods are supported");

    public static readonly DiagnosticDescriptor ConflictingInheritedMapperMethod = Create(
        id: "RAZQL013",
        title: "Mapper method signatures must be unique across the interface hierarchy",
        messageFormat: "Mapper method '{0}' conflicts with method '{1}' inherited by mapper '{2}'");

    public static readonly DiagnosticDescriptor DuplicateMapperQueryGroupName = Create(
        id: "RAZQL014",
        title: "Mapper query group name candidates must be unique",
        messageFormat: "Mapper interface '{0}' produces query group name candidate(s) [{1}] also produced by another mapper interface");

    public static readonly DiagnosticDescriptor InaccessibleMapperInterface = Create(
        id: "RAZQL015",
        title: "Mapper interfaces must be accessible to generated code",
        messageFormat: "Mapper interface '{0}' must be accessible from a generated top-level class");

    public static readonly DiagnosticDescriptor InvalidMapperCriteriaType = Create(
        id: "RAZQL016",
        title: "Mapper criteria types must support generic execution",
        messageFormat: "Mapper method '{0}' uses criteria type '{1}', which cannot be used as a generic type argument");

    public static readonly DiagnosticDescriptor MissingMapperTemplateSource = Create(
        id: "RAZQL017",
        title: "Mapper template source was not found",
        messageFormat: "No template source was found for mapper method '{0}'; expected one of: {1}");

    public static readonly DiagnosticDescriptor AmbiguousMapperTemplateSource = Create(
        id: "RAZQL018",
        title: "Mapper template source is ambiguous",
        messageFormat: "Mapper method '{0}' matches multiple template sources: {1}");

    public static readonly DiagnosticDescriptor InvalidMapperTemplateSourceConfiguration = Create(
        id: "RAZQL019",
        title: "Mapper template source configuration is incompatible with its loader",
        messageFormat: "Template source '{0}' for mapper method '{1}' is not configured for {2}: {3}");

    public static readonly DiagnosticDescriptor ConflictingTemplateSourceAttributes = Create(
        id: "RAZQL020",
        title: "Template source attributes conflict",
        messageFormat: "'{0}' has more than one template source attribute");

    public static readonly DiagnosticDescriptor InvalidInlineQuerySource = Create(
        id: "RAZQL021",
        title: "Inline query source cannot be blank",
        messageFormat: "Inline query source specified for mapper method '{0}' cannot be null, empty, or whitespace");
}
