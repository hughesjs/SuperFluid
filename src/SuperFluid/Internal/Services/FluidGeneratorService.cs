using SuperFluid.Internal.Definitions;
using YamlDotNet.Serialization;

namespace SuperFluid.Internal.Services;

internal class FluidGeneratorService
{
	private readonly IDeserializer _yamlDeserializer;

	public FluidGeneratorService(IDeserializer yamlDeserializer)
	{
		_yamlDeserializer = yamlDeserializer;
	}

	public Dictionary<string, string> Generate(string projectRoot, List<string> apiDefinitionFiles)
	{
		string[]                 fullPaths   = apiDefinitionFiles.Select(p => Path.Combine(projectRoot, p)).ToArray();
		List<FluidApiDefinition> definitions = LoadDefinitions(fullPaths);
		throw new NotImplementedException();
	}
	
	private List<FluidApiDefinition> LoadDefinitions(string[] fullPaths)
		=> fullPaths.Select(File.ReadAllText).Select(text => _yamlDeserializer.Deserialize<FluidApiDefinition>(text)).ToList();
}
