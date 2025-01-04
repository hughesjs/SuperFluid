using SuperFluid.Internal.Definitions;
using SuperFluid.Internal.Model;
using SuperFluid.Internal.Parsers;
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

	public Dictionary<string, string> Generate(string rawYml)
	{
		FluidApiDefinition definition = _yamlDeserializer.Deserialize<FluidApiDefinition>(rawYml);

		FluidApiModel model = _definitionParser.Parse(definition);

		Dictionary<string, string> newSourceFiles = model.States.ToDictionary(s => $"{s.Name}.fluid.g.cs", s => GenerateStateSource(s, model));
		
		newSourceFiles.Add($"{model.Name}.fluid.g.cs", GenerateCompoundInterface(model));

		return newSourceFiles;
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
