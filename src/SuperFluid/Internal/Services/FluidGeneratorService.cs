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

		FluidApiDefinitionParser parser = new(definition);
		FluidApiModel            model  = parser.Parse();
		
		Dictionary<string, string> newSourceFiles = model.States.ToDictionary(s => $"{model.Name}.fluid.g.cs", s => GenerateStateSource(s, model));
		
		return newSourceFiles;
	}

	private string GenerateStateSource(FluidApiState fluidApiState, FluidApiModel model)
	{
		string source = $$"""
						namespace {{model.Namespace}};
						
						public interface {{fluidApiState.Name}}
						{
							{{""/*string.Join(Environment.NewLine, methodDeclarations)*/}}
						}
						""";
		
		throw new NotImplementedException();
	}


}
