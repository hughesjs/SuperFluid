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

	public string Generate(string rawYml)
	{
		FluidApiDefinition definition = _yamlDeserializer.Deserialize<FluidApiDefinition>(rawYml);

		FluidApiDefinitionParser parser = new(definition);
		FluidApiModel            model  = parser.Parse();
		
		return "";
	}



}
