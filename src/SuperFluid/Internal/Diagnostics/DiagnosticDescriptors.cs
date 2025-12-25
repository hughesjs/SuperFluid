using Microsoft.CodeAnalysis;

namespace SuperFluid.Internal.Diagnostics;

internal static class DiagnosticDescriptors
{
	private const string Category = "SuperFluid.Generator";

	// YAML Errors
	public static readonly DiagnosticDescriptor InvalidYamlSyntax = new(
		id: "SF0001",
		title: "Invalid YAML syntax",
		messageFormat: "YAML syntax error in '{0}': {1}",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "The .fluid.yml file contains invalid YAML syntax.");

	public static readonly DiagnosticDescriptor MissingRequiredField = new(
		id: "SF0002",
		title: "Missing required field",
		messageFormat: "Required field '{0}' is missing in '{1}'",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "A required field (Name, Namespace, InitialState, or Methods) is missing from the YAML definition.");

	public static readonly DiagnosticDescriptor YamlTypeMismatch = new(
		id: "SF0003",
		title: "YAML type mismatch",
		messageFormat: "Type mismatch in '{0}': {1}",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor EmptyYamlContent = new(
		id: "SF0004",
		title: "Empty YAML file",
		messageFormat: "The .fluid.yml file '{0}' is empty or contains only whitespace",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	// State Machine Errors
	public static readonly DiagnosticDescriptor InvalidTransitionReference = new(
		id: "SF0005",
		title: "Invalid transition reference",
		messageFormat: "Method '{0}' references non-existent transition target '{1}'",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "A method's CanTransitionTo list contains a method name that doesn't exist in the Methods list.");

	public static readonly DiagnosticDescriptor DuplicateMethodName = new(
		id: "SF0006",
		title: "Duplicate method name",
		messageFormat: "Method name '{0}' is defined more than once",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor EmptyConstraintsList = new(
		id: "SF0007",
		title: "Empty generic constraints",
		messageFormat: "Generic argument '{0}' has an empty constraints list",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "Generic arguments must have at least one constraint.");

	public static readonly DiagnosticDescriptor StateNameCollision = new(
		id: "SF0008",
		title: "State name collision",
		messageFormat: "Multiple states would generate the same interface name '{0}'",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor EmptyStatesGenerated = new(
		id: "SF0009",
		title: "No states generated",
		messageFormat: "The state machine definition resulted in no valid states",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	// Code Generation Errors
	public static readonly DiagnosticDescriptor InvalidCSharpIdentifier = new(
		id: "SF0010",
		title: "Invalid C# identifier",
		messageFormat: "'{0}' is not a valid C# identifier in {1}",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor UnexpectedGenerationError = new(
		id: "SF0011",
		title: "Unexpected generation error",
		messageFormat: "An unexpected error occurred during code generation: {0}",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "An unexpected exception occurred. This may be a bug in SuperFluid.");

	// Info/Warnings
	public static readonly DiagnosticDescriptor NoFluidYamlFilesFound = new(
		id: "SF0012",
		title: "No .fluid.yml files found",
		messageFormat: "No .fluid.yml files found in AdditionalFiles",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		description: "Ensure .fluid.yml files are marked as AdditionalFiles in your .csproj.");

	public static readonly DiagnosticDescriptor UnusedMethod = new(
		id: "SF0013",
		title: "Unreachable method",
		messageFormat: "Method '{0}' is not reachable from the initial state",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);
}
