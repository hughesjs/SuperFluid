using System;
using System.Linq;
using SuperFluid.Internal.Definitions;
using SuperFluid.Internal.Diagnostics;
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
		catch (Exception ex) when (ex.Message.Contains("Property") || ex.Message.Contains("required"))
		{
			// Catches deserialization errors for missing required fields or type mismatches
			string fieldName = ExtractFieldName(ex.Message);
			Diagnostic diagnostic = Diagnostic.Create(
				DiagnosticDescriptors.MissingRequiredField,
				Location.None,
				fieldName,
				filePath);
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
		catch (InvalidOperationException ex) when (ex.Message.Contains("does not exist"))
		{
			string[] parts = ex.Message.Split('\'');
			string methodName = parts.Length > 1 ? parts[1] : "unknown";
			Diagnostic diagnostic = Diagnostic.Create(
				DiagnosticDescriptors.InvalidTransitionReference,
				Location.None,
				"unknown",
				methodName);
			return GenerationResult.Failure(diagnostic);
		}
		catch (InvalidOperationException ex) when (ex.Message.Contains("Duplicate method name"))
		{
			string[] parts = ex.Message.Split('\'');
			string methodName = parts.Length > 1 ? parts[1] : "unknown";
			Diagnostic diagnostic = Diagnostic.Create(
				DiagnosticDescriptors.DuplicateMethodName,
				Location.None,
				methodName);
			return GenerationResult.Failure(diagnostic);
		}
		catch (ArgumentException ex) when (ex.ParamName == "constraints")
		{
			Diagnostic diagnostic = Diagnostic.Create(
				DiagnosticDescriptors.EmptyConstraintsList,
				Location.None,
				"unknown");
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
				s => $"{s.Name}.fluid.g.cs",
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

		return GenerationResult.Success(newSourceFiles);
	}

	private string ExtractFieldName(string exceptionMessage)
	{
		if (exceptionMessage.Contains("Property 'Name'"))
			return "Name";
		if (exceptionMessage.Contains("Property 'Namespace'"))
			return "Namespace";
		if (exceptionMessage.Contains("Property 'InitialState'"))
			return "InitialState";
		if (exceptionMessage.Contains("Property 'Methods'"))
			return "Methods";

		return "unknown field";
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
		string source = $$"""
							namespace {{model.Namespace}};

							public interface {{model.Name}}: {{string.Join(",", model.States.Select(s => s.Name))}}
							{
								public static abstract {{model.InitializerMethodReturnState.Name}} {{model.InitialMethod.Name}}({{string.Join(", ", model.InitialMethod.Arguments.Select(a =>$"{a.Type} {a.Name}"))}});
							}
							""";
		return source;
	}

	private string GenerateStateSource(FluidApiState fluidApiState, FluidApiModel model) 
	{
		IEnumerable<string> methodDeclarations = fluidApiState.MethodTransitions.Select((kvp) => GenerateMethodSource(kvp.Key, kvp.Value));

		string source = $$"""
						namespace {{model.Namespace}};
						
						public interface {{fluidApiState.Name}}
						{
						{{string.Join("\n", methodDeclarations)}}
						}
						""";

		return source;
	}

	private string GenerateMethodSource(FluidApiMethod method, FluidApiState state)
	{
		string genericArgs = method.GenericArguments.Count > 0 ? $"<{string.Join(",", method.GenericArguments.Select(a => $"{a.Name}"))}>" : string.Empty;

		string constraints = method.GenericArguments.Count > 0 ? $" {string.Join(" ", method.GenericArguments.Select(GenerateGenericConstraintSource))}" : string.Empty;

		return $"""
		        	public {method.ReturnType ?? state.Name} {method.Name}{genericArgs}({string.Join(", ", method.Arguments.Select(GenerateMethodArgsSource))}){constraints};
		        """;
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
