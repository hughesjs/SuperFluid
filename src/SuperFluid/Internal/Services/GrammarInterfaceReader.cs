using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using SuperFluid.Internal.Definitions;
using SuperFluid.Internal.Exceptions;

namespace SuperFluid.Internal.Services;

/// <summary>
/// Translates a Roslyn <see cref="INamedTypeSymbol"/> representing an interface decorated with
/// <c>[SuperFluid.FluidApiGrammarAttribute]</c> into a <see cref="FluidApiDefinition"/> DTO.
/// The downstream pipeline (parser, model, state naming, code emission) consumes the DTO exactly
/// as it does for the YAML path.
/// </summary>
internal class GrammarInterfaceReader
{
    private const string GrammarSuffix = "Grammar";
    private const string FluidApiGrammarAttributeName = "SuperFluid.FluidApiGrammarAttribute";
    private const string InitialAttributeName = "SuperFluid.InitialAttribute";
    private const string TransitionsToAttributeName = "SuperFluid.TransitionsToAttribute";
    private const string ReturnTypeAttributeName = "SuperFluid.ReturnTypeAttribute";

    /// <summary>
    /// Reads the grammar interface symbol and produces a <see cref="FluidApiDefinition"/>.
    /// </summary>
    /// <param name="grammarInterface">
    /// An interface symbol decorated with <c>[FluidApiGrammarAttribute]</c>.
    /// </param>
    /// <returns>A fully populated <see cref="FluidApiDefinition"/> ready for the parser pipeline.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="grammarInterface"/> is null.</exception>
    /// <exception cref="MissingInitialMethodException">
    /// Thrown when the interface has no method marked <c>[Initial]</c>.
    /// </exception>
    public FluidApiDefinition Read(INamedTypeSymbol grammarInterface)
    {
        if (grammarInterface is null)
        {
            throw new ArgumentNullException(nameof(grammarInterface));
        }

        string name = DeriveDefinitionName(grammarInterface.Name);
        string namespaceName = grammarInterface.ContainingNamespace.ToDisplayString();
        string description = ExtractSummary(grammarInterface.GetDocumentationCommentXml());

        List<IMethodSymbol> methods = grammarInterface.GetMembers()
            .OfType<IMethodSymbol>()
            .ToList();

        List<IMethodSymbol> initialMethodSymbols = methods
            .Where(m => HasAttribute(m, InitialAttributeName))
            .ToList();

        if (initialMethodSymbols.Count == 0)
        {
            throw new MissingInitialMethodException(grammarInterface.Name);
        }

        if (initialMethodSymbols.Count > 1)
        {
            throw new MultipleInitialMethodsException(
                grammarInterface.Name,
                initialMethodSymbols.Select(m => m.Name).ToList());
        }

        IMethodSymbol initialMethodSymbol = initialMethodSymbols[0];

        FluidApiMethodDefinition initialState = ReadMethod(initialMethodSymbol);
        List<FluidApiMethodDefinition> remainingMethods = methods
            .Where(m => !SymbolEqualityComparer.Default.Equals(m, initialMethodSymbol))
            .Select(ReadMethod)
            .ToList();

        return new()
        {
            Name = name,
            Namespace = namespaceName,
            Description = description,
            InitialState = initialState,
            Methods = remainingMethods
        };
    }

    // Map from fully-qualified BCL type names to C# primitive keywords.
    // This ensures the grammar-interface reader emits the same short keyword types as the YAML path,
    // which stores type names as plain strings such as "int", "string", "bool".
    private static readonly Dictionary<string, string> PrimitiveKeywords = new(StringComparer.Ordinal)
    {
        ["global::System.Boolean"]  = "bool",
        ["global::System.Byte"]     = "byte",
        ["global::System.SByte"]    = "sbyte",
        ["global::System.Int16"]    = "short",
        ["global::System.UInt16"]   = "ushort",
        ["global::System.Int32"]    = "int",
        ["global::System.UInt32"]   = "uint",
        ["global::System.Int64"]    = "long",
        ["global::System.UInt64"]   = "ulong",
        ["global::System.Single"]   = "float",
        ["global::System.Double"]   = "double",
        ["global::System.Decimal"]  = "decimal",
        ["global::System.Char"]     = "char",
        ["global::System.String"]   = "string",
        ["global::System.Object"]   = "object"
    };

    /// <summary>
    /// Returns the C# primitive keyword for well-known BCL types (e.g. <c>int</c> for
    /// <c>System.Int32</c>); for all other types, returns the fully-qualified name stripped of the
    /// <c>global::</c> prefix (e.g. <c>INumber</c> for a top-level interface, or
    /// <c>System.Collections.Generic.List&lt;T&gt;</c> for a BCL generic type).
    /// This keeps generated code consistent with the YAML path, which stores type names as plain
    /// strings without the Roslyn global alias prefix.
    /// </summary>
    private static string ToTypeString(ITypeSymbol type)
    {
        string fullyQualified = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (PrimitiveKeywords.TryGetValue(fullyQualified, out string? keyword))
        {
            return keyword;
        }

        // Strip the "global::" alias prefix that Roslyn adds to fully-qualified names.
        // The resulting string is still unambiguous in generated C# source files.
        const string GlobalPrefix = "global::";
        return fullyQualified.StartsWith(GlobalPrefix, StringComparison.Ordinal)
            ? fullyQualified.Substring(GlobalPrefix.Length)
            : fullyQualified;
    }

    /// <summary>
    /// Strips the "Grammar" suffix from an interface name if present, yielding the actor name.
    /// Exposed as internal so the source generator can reuse the same logic when building
    /// the collision-detection pipeline without re-instantiating a reader.
    /// </summary>
    internal static string DeriveActorName(string interfaceName) => DeriveDefinitionName(interfaceName);

    /// <summary>Strips the "Grammar" suffix from the interface name if present.</summary>
    private static string DeriveDefinitionName(string interfaceName)
    {
        if (interfaceName.EndsWith(GrammarSuffix, StringComparison.Ordinal) && interfaceName.Length > GrammarSuffix.Length)
        {
            return interfaceName.Substring(0, interfaceName.Length - GrammarSuffix.Length);
        }

        return interfaceName;
    }

    /// <summary>Converts a single <see cref="IMethodSymbol"/> to its DTO representation.</summary>
    private static FluidApiMethodDefinition ReadMethod(IMethodSymbol method)
    {
        string description = ExtractSummary(method.GetDocumentationCommentXml());
        List<string> transitions = ReadTransitions(method);
        string? returnType = ReadReturnType(method);
        List<FluidApiArgumentDefinition> arguments = method.Parameters.Select(ReadArgument).ToList();
        List<FluidGenericArgumentDefinition> genericArguments = method.TypeParameters.Select(ReadGenericArgument).ToList();

        return new()
        {
            Name = method.Name,
            Description = description,
            CanTransitionTo = transitions,
            ReturnType = returnType,
            Arguments = arguments,
            GenericArguments = genericArguments
        };
    }

    /// <summary>Reads the string array from a <c>[TransitionsTo]</c> attribute, or returns an empty list.</summary>
    private static List<string> ReadTransitions(IMethodSymbol method)
    {
        AttributeData? attr = method.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == TransitionsToAttributeName);

        if (attr is null || attr.ConstructorArguments.Length == 0)
        {
            return [];
        }

        // The attribute declares params string[] names; the first constructor argument is a params array.
        TypedConstant arg = attr.ConstructorArguments[0];

        if (arg.Kind == TypedConstantKind.Array)
        {
            return arg.Values
                .Where(v => v.Value is string)
                .Select(v => (string)v.Value!)
                .ToList();
        }

        // Single value (compiler may pass a primitive instead of an array for a single element)
        if (arg.Value is string singleName)
        {
            return [singleName];
        }

        return [];
    }

    /// <summary>
    /// Reads the <c>[ReturnType(typeof(X))]</c> attribute and returns the fully-qualified display
    /// string of the type, or null if the attribute is absent.
    /// </summary>
    private static string? ReadReturnType(IMethodSymbol method)
    {
        AttributeData? attr = method.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == ReturnTypeAttributeName);

        if (attr is null || attr.ConstructorArguments.Length == 0)
        {
            return null;
        }

        TypedConstant arg = attr.ConstructorArguments[0];
        if (arg.Value is INamedTypeSymbol typeSymbol)
        {
            return ToTypeString(typeSymbol);
        }

        return null;
    }

    /// <summary>Converts an <see cref="IParameterSymbol"/> to its DTO representation.</summary>
    private static FluidApiArgumentDefinition ReadArgument(IParameterSymbol parameter)
    {
        string type = ToTypeString(parameter.Type);
        string? defaultValue = null;

        if (parameter.HasExplicitDefaultValue)
        {
            defaultValue = CSharpLiteralFormatter.Format(parameter.ExplicitDefaultValue, parameter.Type);
        }

        return new()
        {
            Name = parameter.Name,
            Type = type,
            DefaultValue = defaultValue
        };
    }

    /// <summary>Converts an <see cref="ITypeParameterSymbol"/> to its DTO representation.</summary>
    private static FluidGenericArgumentDefinition ReadGenericArgument(ITypeParameterSymbol typeParam)
    {
        List<string> constraints = AssembleConstraints(typeParam);

        return new()
        {
            Name = typeParam.Name,
            Constraints = constraints
        };
    }

    /// <summary>
    /// Assembles the constraint list for a type parameter in the order required by C# syntax:
    /// class/struct/notnull/unmanaged first, then type constraints, then new() last.
    /// </summary>
    private static List<string> AssembleConstraints(ITypeParameterSymbol typeParam)
    {
        List<string> constraints = [];

        // Special constraints come first, in the order C# requires
        if (typeParam.HasReferenceTypeConstraint)
        {
            constraints.Add("class");
        }

        // `where T : unmanaged` sets both HasValueTypeConstraint and HasUnmanagedTypeConstraint
        // in Roslyn (because unmanaged implies struct). Only emit "struct" for the plain case;
        // "struct, unmanaged" is a C# compile error (CS8331).
        if (typeParam.HasValueTypeConstraint && !typeParam.HasUnmanagedTypeConstraint)
        {
            constraints.Add("struct");
        }

        if (typeParam.HasNotNullConstraint)
        {
            constraints.Add("notnull");
        }

        if (typeParam.HasUnmanagedTypeConstraint)
        {
            constraints.Add("unmanaged");
        }

        // Type constraints follow
        foreach (ITypeSymbol constraintType in typeParam.ConstraintTypes)
        {
            constraints.Add(ToTypeString(constraintType));
        }

        // Constructor constraint is always last per C# syntax rules
        if (typeParam.HasConstructorConstraint)
        {
            constraints.Add("new()");
        }

        return constraints;
    }

    /// <summary>
    /// Returns true when the given method symbol has an attribute whose fully-qualified class name
    /// matches <paramref name="fullyQualifiedAttributeName"/>.
    /// </summary>
    private static bool HasAttribute(IMethodSymbol method, string fullyQualifiedAttributeName)
    {
        return method.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == fullyQualifiedAttributeName);
    }

    // Extracts the inner text of the first <summary> element from a Roslyn-produced XML doc comment.
    // Returns "" when there is no summary (or the doc comment itself is absent), so callers can
    // treat empty as "no description" without a null check.
    // netstandard2.0 workaround: range-indexer syntax requires newer BCL support, so we use LINQ.
    private static string ExtractSummary(string? xmlDoc)
    {
        if (string.IsNullOrWhiteSpace(xmlDoc))
        {
            return "";
        }

        try
        {
            XDocument doc = XDocument.Parse(xmlDoc!);
            XElement? summaryElement = doc.Descendants("summary").FirstOrDefault();

            if (summaryElement is null)
            {
                return "";
            }

            string raw = summaryElement.Value;

            string[] lines = raw.Split('\n');
            int first = 0;
            int last = lines.Length - 1;

            while (first <= last && string.IsNullOrWhiteSpace(lines[first]))
            {
                first++;
            }

            while (last >= first && string.IsNullOrWhiteSpace(lines[last]))
            {
                last--;
            }

            if (first > last)
            {
                return "";
            }

            IEnumerable<string> trimmedLines = lines.Skip(first).Take(last - first + 1).Select(l => l.TrimStart());
            string result = string.Join("\n", trimmedLines).TrimEnd();

            return result;
        }
        catch (Exception)
        {
            // If XML parsing fails for any reason, treat as no description rather than throwing
            return "";
        }
    }
}
