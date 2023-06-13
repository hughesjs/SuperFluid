using SuperFluid.Internal.Definitions;
using SuperFluid.Internal.Model;
using SuperFluid.Internal.Parsers;
using YamlDotNet.Serialization;

namespace SuperFluid.Internal.Services;

internal class FluidGeneratorService
{
	private readonly IDeserializer            _yamlDeserializer;

	public FluidGeneratorService(IDeserializer yamlDeserializer)
	{
		_yamlDeserializer = yamlDeserializer;
	}

	public Dictionary<string,string> Generate(string rawYml)
	{
		FluidApiDefinition definition = _yamlDeserializer.Deserialize<FluidApiDefinition>(rawYml);

		FluidApiDefinitionParser parser = new();
		FluidApiModel            model  = parser.Parse(definition);
		
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
