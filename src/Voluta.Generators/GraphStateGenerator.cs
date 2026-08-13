using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Voluta.Generators;

/// <summary>
///     Generates Schema / Update / ToWrites for types annotated with [GraphState].
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class GraphStateGenerator : IIncrementalGenerator
{
    private const string GraphStateAttributeMetadataName = "Voluta.Abstractions.State.GraphStateAttribute";
    private const string ChannelAttributeMetadataName = "Voluta.Abstractions.State.ChannelAttribute";

    private static readonly SymbolDisplayFormat TypeDisplayFormat = new(
        SymbolDisplayGlobalNamespaceStyle.Included,
        SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
                              | SymbolDisplayMiscellaneousOptions.UseSpecialTypes
                              | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var stateClasses = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                GraphStateAttributeMetadataName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (context, _) => (INamedTypeSymbol)context.TargetSymbol)
            .Where(static symbol => symbol is not null);

        context.RegisterSourceOutput(stateClasses, static (production, symbol) => { Execute(production, symbol); });
    }

    private static void Execute(SourceProductionContext context, INamedTypeSymbol stateType)
    {
        if (!stateType.IsPartial())
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Descriptors.MustBePartial,
                    stateType.Locations.FirstOrDefault(),
                    stateType.Name));
            return;
        }

        var channels = CollectChannels(stateType, context);
        if (channels.IsDefaultOrEmpty)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Descriptors.NoChannels,
                    stateType.Locations.FirstOrDefault(),
                    stateType.Name));
            return;
        }

        var source = GenerateSource(stateType, channels);
        var hintName = $"{stateType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", string.Empty)
            .Replace('.', '_')
            .Replace('<', '_')
            .Replace('>', '_')}.g.cs";
        context.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
    }

    private static ImmutableArray<ChannelMember> CollectChannels(
        INamedTypeSymbol stateType,
        SourceProductionContext context)
    {
        var builder = ImmutableArray.CreateBuilder<ChannelMember>();

        foreach (var member in stateType.GetMembers())
        {
            if (member is not IPropertySymbol property
                || property.IsStatic
                || property.DeclaredAccessibility != Accessibility.Public)
            {
                continue;
            }

            var channelAttribute = property
                .GetAttributes()
                .FirstOrDefault(attribute =>
                    attribute.AttributeClass?.ToDisplayString() == ChannelAttributeMetadataName);

            if (channelAttribute is null)
            {
                continue;
            }

            if (channelAttribute.ConstructorArguments.Length == 0)
            {
                continue;
            }

            var kindArgument = channelAttribute.ConstructorArguments[0];
            if (kindArgument.Value is null || kindArgument.Type is not INamedTypeSymbol kindEnum)
            {
                continue;
            }

            var kindName = kindEnum
                .GetMembers()
                .OfType<IFieldSymbol>()
                .FirstOrDefault(field =>
                    field.HasConstantValue && Equals(field.ConstantValue, kindArgument.Value))
                ?.Name;

            if (kindName is null)
            {
                continue;
            }

            string? customName = null;
            foreach (var named in channelAttribute.NamedArguments)
            {
                if (named.Key == "Name" && named.Value.Value is string nameValue)
                {
                    customName = nameValue;
                }
            }

            var channelName = string.IsNullOrWhiteSpace(customName) ? property.Name : customName!;
            builder.Add(
                new ChannelMember(
                    property.Name,
                    channelName,
                    kindName,
                    property.Type.ToDisplayString(TypeDisplayFormat)));
        }

        return builder.ToImmutable();
    }

    private static string GenerateSource(INamedTypeSymbol stateType, ImmutableArray<ChannelMember> channels)
    {
        var ns = stateType.ContainingNamespace.IsGlobalNamespace
            ? null
            : stateType.ContainingNamespace.ToDisplayString();
        var typeName = stateType.Name;
        var updateTypeName = $"{typeName}Update";
        var accessibility = stateType.DeclaredAccessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            Accessibility.NotApplicable => throw new NotImplementedException(),
            Accessibility.Private => throw new NotImplementedException(),
            Accessibility.ProtectedAndInternal => throw new NotImplementedException(),
            Accessibility.Protected => throw new NotImplementedException(),
            Accessibility.ProtectedOrInternal => throw new NotImplementedException(),
            _ => "public"
        };

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using Voluta.Abstractions.Channels;");
        sb.AppendLine("using Voluta.Abstractions.State;");
        sb.AppendLine();

        if (ns is not null)
        {
            sb.Append("namespace ").Append(ns).AppendLine(";");
            sb.AppendLine();
        }

        sb.Append(accessibility).Append(" partial class ").AppendLine(typeName);
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Creates the channel schema for this state type.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static GraphChannelSchema CreateSchema()");
        sb.AppendLine("    {");
        sb.AppendLine("        return new GraphChannelSchema.Builder()");

        foreach (var channel in channels)
        {
            sb.Append("            .Add(\"")
                .Append(EscapeString(channel.ChannelName))
                .Append("\", ChannelKind.")
                .Append(channel.KindName)
                .AppendLine(")");
        }

        sb.AppendLine("            .Build();");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine(
            "    /// Partial update type: unset properties emit no write; set (including null) emits a write.");
        sb.AppendLine("    /// </summary>");
        sb.Append("    ").Append(accessibility).Append(" sealed class ").AppendLine(updateTypeName);
        sb.AppendLine("    {");

        foreach (var channel in channels)
        {
            sb.AppendLine("        /// <summary>");
            sb.Append("        /// Partial update for channel \"").Append(EscapeString(channel.ChannelName))
                .AppendLine("\".");
            sb.AppendLine("        /// </summary>");
            sb.Append("        public OptionalValue<")
                .Append(channel.TypeDisplay)
                .Append("> ")
                .Append(channel.PropertyName)
                .AppendLine(" { get; init; }");
            sb.AppendLine();
        }

        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Converts set properties into channel writes. Unset properties are omitted.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        public IReadOnlyList<ChannelWrite> ToWrites()");
        sb.AppendLine("        {");
        sb.AppendLine("            var writes = new List<ChannelWrite>();");

        foreach (var channel in channels)
        {
            sb.Append("            if (").Append(channel.PropertyName).AppendLine(".IsSet)");
            sb.AppendLine("            {");
            sb.Append("                writes.Add(new ChannelWrite(\"")
                .Append(EscapeString(channel.ChannelName))
                .Append("\", ")
                .Append(channel.PropertyName)
                .AppendLine(".Value));");
            sb.AppendLine("            }");
            sb.AppendLine();
        }

        sb.AppendLine("            return writes;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string EscapeString(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private sealed class ChannelMember(
        string propertyName,
        string channelName,
        string kindName,
        string typeDisplay)
    {
        public string PropertyName { get; } = propertyName;

        public string ChannelName { get; } = channelName;

        public string KindName { get; } = kindName;

        public string TypeDisplay { get; } = typeDisplay;
    }

    private static class Descriptors
    {
        public static readonly DiagnosticDescriptor MustBePartial = new(
            "VOLUTA001",
            "GraphState type must be partial",
            "Type '{0}' is marked with [GraphState] but is not partial",
            "Voluta.Generators",
            DiagnosticSeverity.Error,
            true);

        public static readonly DiagnosticDescriptor NoChannels = new(
            "VOLUTA002",
            "GraphState type has no channel properties",
            "Type '{0}' is marked with [GraphState] but has no [Channel] properties",
            "Voluta.Generators",
            DiagnosticSeverity.Error,
            true);
    }
}

file static class SymbolExtensions
{
    public static bool IsPartial(this INamedTypeSymbol symbol)
    {
        foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
        {
            if (syntaxReference.GetSyntax() is ClassDeclarationSyntax classDeclaration
                && classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                return true;
            }
        }

        return false;
    }
}
