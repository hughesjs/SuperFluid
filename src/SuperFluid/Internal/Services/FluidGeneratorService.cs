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

		return newSourceFiles;
	}

	private string GenerateStateSource(FluidApiState fluidApiState, FluidApiModel model)
	{
		IEnumerable<string> methodDeclarations = fluidApiState.MethodTransitions.Select(kvp
																							=> $"""
																									public {kvp.Value.Name} {kvp.Key.Name}();
																								""");

		string source = $$"""
						namespace {{model.Namespace}};
						
						public interface {{fluidApiState.Name}}
						{
						{{string.Join('\n', methodDeclarations)}}
						}
						""";

		return source;
	}


}
