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
		
		// Should this be in the parser?
		List<List<FluidApiMethod>> minimalTransitionSets = GetMinimalTransitionSets(model);
		List<FluidApiState>        states                = minimalTransitionSets.Select(ts => new FluidApiState(ts)).ToList();
		return "";
	}

	private List<List<FluidApiMethod>> GetMinimalTransitionSets(FluidApiModel definition)
	{
		List<List<FluidApiMethod>> transitionSets = definition.States.Select(m => m.CanTransitionTo)
															  .DistinctBy(transitions => string.Join(',', transitions.Select(t => t.Name).Order()))
															  .ToList();
		return transitionSets;
	}

}
