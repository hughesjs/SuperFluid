using System;
using System.Collections.Generic;
using System.Linq;
using SuperFluid.Internal.Definitions;
using SuperFluid.Internal.Diagnostics;
using SuperFluid.Internal.Exceptions;
using SuperFluid.Internal.Model;
using SuperFluid.Internal.Parsers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using YamlDotNet.Serialization;

namespace SuperFluid.Internal.Services;

internal class FluidGeneratorService
{
	private readonly IDeserializer            _yamlDeserializer;
	private readonly FluidApiDefinitionParser _definitionParser;

	public FluidGeneratorService(IDeserializer yamlDeserializer, FluidApiDefinitionParser definitionParser)
	{
		_yamlDeserializer = yamlDeserializer;
		_definitionParser = definitionParser;
	}

	public GenerationResult Generate(string rawYml, string filePath)
	{
		// Validate input
		if (string.IsNullOrWhiteSpace(rawYml))
		{
			Diagnostic diagnostic = Diagnostic.Create(
				DiagnosticDescriptors.EmptyYamlContent,
				Location.None,
				filePath);
			return GenerationResult.Failure(diagnostic);
		}

		// Deserialize with error handling
		FluidApiDefinition? definition;
		try
		{
			definition = _yamlDeserializer.Deserialize<FluidApiDefinition>(rawYml);
		}
		catch (YamlDotNet.Core.YamlException ex)
		{
			Diagnostic diagnostic = Diagnostic.Create(
				DiagnosticDescriptors.InvalidYamlSyntax,
				Location.None,
				filePath,
				ex.Message);
			return GenerationResult.Failure(diagnostic);
		}

		if (definition is null)
		{
			Diagnostic diagnostic = Diagnostic.Create(
				DiagnosticDescriptors.EmptyYamlContent,
				Location.None,
				filePath);
			return GenerationResult.Failure(diagnostic);
		}

		// Check for null/empty required fields first (before validating as identifiers)
		if (string.IsNullOrWhiteSpace(definition.Name))
		{
			Diagnostic diagnostic = Diagnostic.Create(
				DiagnosticDescriptors.MissingRequiredField,
				Location.None,
				"Name",
				filePath);
			return GenerationResult.Failure(diagnostic);
		}

		if (string.IsNullOrWhiteSpace(definition.Namespace))
		{
			Diagnostic diagnostic = Diagnostic.Create(
				DiagnosticDescriptors.MissingRequiredField,
				Location.None,
				"Namespace",
				filePath);
			return GenerationResult.Failure(diagnostic);
		}

		if (definition.InitialState is null)
		{
			Diagnostic diagnostic = Diagnostic.Create(
				DiagnosticDescriptors.MissingRequiredField,
				Location.None,
				"InitialState",
				filePath);
			return GenerationResult.Failure(diagnostic);
		}

		if (definition.Methods is null)
		{
			Diagnostic diagnostic = Diagnostic.Create(
				DiagnosticDescriptors.MissingRequiredField,
				Location.None,
				"Methods",
				filePath);
			return GenerationResult.Failure(diagnostic);
		}

		// Validate C# identifiers
		if (!SyntaxFacts.IsValidIdentifier(definition.Name))
		{
			Diagnostic diagnostic = Diagnostic.Create(
				DiagnosticDescriptors.InvalidCSharpIdentifier,
				Location.None,
				definition.Name,
				"Name");
			return GenerationResult.Failure(diagnostic);
		}

		if (!IsValidNamespace(definition.Namespace))
		{
			Diagnostic diagnostic = Diagnostic.Create(
				DiagnosticDescriptors.InvalidCSharpIdentifier,
				Location.None,
				definition.Namespace,
				"Namespace");
			return GenerationResult.Failure(diagnostic);
		}

		// Validate method names
		if (definition.Methods != null)
		{
			foreach (FluidApiMethodDefinition method in definition.Methods)
			{
				if (!SyntaxFacts.IsValidIdentifier(method.Name))
				{
					Diagnostic diagnostic = Diagnostic.Create(
						DiagnosticDescriptors.InvalidCSharpIdentifier,
						Location.None,
						method.Name,
						$"Method name");
					return GenerationResult.Failure(diagnostic);
				}
			}
		}

		// Parse definition into model
		FluidApiModel model;
		try
		{
			model = _definitionParser.Parse(definition);
		}
		catch (MethodNotFoundException ex)
		{
			Diagnostic diagnostic = Diagnostic.Create(
				DiagnosticDescriptors.InvalidTransitionReference,
				Location.None,
				ex.ReferencingMethod,
				ex.MissingMethod);
			return GenerationResult.Failure(diagnostic);
		}
		catch (DuplicateMethodNameException ex)
		{
			Diagnostic diagnostic = Diagnostic.Create(
				DiagnosticDescriptors.DuplicateMethodName,
				Location.None,
				ex.MethodName);
			return GenerationResult.Failure(diagnostic);
		}
		catch (EmptyConstraintsException ex)
		{
			Diagnostic diagnostic = Diagnostic.Create(
				DiagnosticDescriptors.EmptyConstraintsList,
				Location.None,
				ex.GenericArgumentName);
			return GenerationResult.Failure(diagnostic);
		}
		catch (InvalidStateNameIdentifierException ex)
		{
			Diagnostic diagnostic = Diagnostic.Create(
				DiagnosticDescriptors.InvalidStateNameIdentifier,
				Location.None,
				ex.DeclaredName);
			return GenerationResult.Failure(diagnostic);
		}
		catch (AmbiguousStateNameDeclarationException ex)
		{
			Diagnostic diagnostic = Diagnostic.Create(
				DiagnosticDescriptors.AmbiguousStateNameDeclaration,
				Location.None,
				ex.FirstName,
				ex.SecondName);
			return GenerationResult.Failure(diagnostic);
		}
		catch (Exception ex)
		{
			Diagnostic diagnostic = Diagnostic.Create(
				DiagnosticDescriptors.UnexpectedGenerationError,
				Location.None,
				ex.Message);
			return GenerationResult.Failure(diagnostic);
		}

		// Emit SF0014 warnings for user-declared state names that matched no synthesised state
		List<Diagnostic> warnings = new();
		foreach (string unmatchedName in model.UnmatchedStateNameWarnings)
		{
			warnings.Add(Diagnostic.Create(
				DiagnosticDescriptors.UnmatchedStateNameDeclaration,
				Location.None,
				unmatchedName));
		}

		// Check for empty states
		if (model.States.Count == 0)
		{
			Diagnostic diagnostic = Diagnostic.Create(
				DiagnosticDescriptors.EmptyStatesGenerated,
				Location.None);
			return GenerationResult.Failure(diagnostic);
		}

		// Check for state name collisions
		Dictionary<string, string> newSourceFiles;
		try
		{
			newSourceFiles = model.States.ToDictionary(
				s => $"{model.StateNames[s]}.fluid.g.cs",
				s => GenerateStateSource(s, model));
		}
		catch (ArgumentException ex) when (ex.Message.Contains("same key"))
		{
			Diagnostic diagnostic = Diagnostic.Create(
				DiagnosticDescriptors.StateNameCollision,
				Location.None,
				"unknown");
			return GenerationResult.Failure(diagnostic);
		}

		newSourceFiles.Add($"{model.Name}.fluid.g.cs", GenerateCompoundInterface(model));

		return warnings.Count > 0
			? GenerationResult.SuccessWithWarnings(newSourceFiles, warnings)
			: GenerationResult.Success(newSourceFiles);
	}

	private bool IsValidNamespace(string namespaceName)
	{
		if (string.IsNullOrWhiteSpace(namespaceName))
			return false;

		string[] parts = namespaceName.Split('.');
		return parts.All(part => SyntaxFacts.IsValidIdentifier(part));
	}

	private string GenerateCompoundInterface(FluidApiModel model)
	{
		string interfaceDoc = FormatXmlDoc(model.Description, "");
		string initMethodDoc = FormatXmlDoc(model.InitialMethod.Description, "\t");
		string source = $$"""
							namespace {{model.Namespace}};

							{{interfaceDoc}}public interface {{model.Name}}: {{string.Join(",", model.States.Select(s => model.StateNames[s]).OrderBy(n => n, StringComparer.Ordinal))}}
							{
							{{initMethodDoc}}	public static abstract {{model.StateNames[model.InitializerMethodReturnState]}} {{model.InitialMethod.Name}}({{string.Join(", ", model.InitialMethod.Arguments.Select(a =>$"{a.Type} {a.Name}"))}});
							}
							""";
		return source;
	}

	private string GenerateStateSource(FluidApiState fluidApiState, FluidApiModel model)
	{
		IEnumerable<string> methodDeclarations = fluidApiState.MethodTransitions
			.OrderBy(kvp => kvp.Key.Name, StringComparer.Ordinal)
			.Select(kvp => GenerateMethodSource(kvp.Key, kvp.Value, model));

		string source = $$"""
						namespace {{model.Namespace}};

						public interface {{model.StateNames[fluidApiState]}}
						{
						{{string.Join("\n", methodDeclarations)}}
						}
						""";

		return source;
	}

	private string GenerateMethodSource(FluidApiMethod method, FluidApiState state, FluidApiModel model)
	{
		string genericArgs = method.GenericArguments.Length > 0 ? $"<{string.Join(",", method.GenericArguments.Select(a => $"{a.Name}"))}>" : string.Empty;

		string constraints = method.GenericArguments.Length > 0 ? $" {string.Join(" ", method.GenericArguments.Select(GenerateGenericConstraintSource))}" : string.Empty;

		string doc = FormatXmlDoc(method.Description, "\t");

		return $"{doc}\tpublic {method.ReturnType ?? model.StateNames[state]} {method.Name}{genericArgs}({string.Join(", ", method.Arguments.Select(GenerateMethodArgsSource))}){constraints};";
	}

	/// <summary>
	/// Returns a formatted XML documentation block for the given description, with each line prefixed by
	/// <paramref name="indent"/> and "/// ". Returns an empty string when the description is null or whitespace.
	/// The returned string ends with a newline so it can be prepended directly before the declaration it documents.
	/// </summary>
	private static string FormatXmlDoc(string? description, string indent)
	{
		if (string.IsNullOrWhiteSpace(description))
			return string.Empty;

		string escaped = description!
			.Replace("&", "&amp;")
			.Replace("<", "&lt;")
			.Replace(">", "&gt;");

		string[] lines = escaped.Split('\n');

		System.Text.StringBuilder sb = new();
		sb.AppendLine($"{indent}/// <summary>");
		foreach (string line in lines)
		{
			string trimmed = line.TrimEnd();
			if (trimmed.Length > 0)
				sb.AppendLine($"{indent}/// {trimmed}");
			else
				sb.AppendLine($"{indent}///");
		}
		sb.AppendLine($"{indent}/// </summary>");
		return sb.ToString();
	}

	private string GenerateMethodArgsSource(FluidApiArgument a)
	{
		return $"{a.Type} {a.Name}{(a.DefaultValue is not null ? " = " + a.DefaultValue : string.Empty)}";
	}

	private string GenerateGenericConstraintSource(FluidGenericArgument a)
	{
		return $"where {a.Name} : {string.Join(", ", a.Constraints)}";
	}
}
