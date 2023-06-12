using System.Diagnostics;
using Microsoft.CodeAnalysis;
using SuperFluid.Internal.Services;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SuperFluid.Internal.SourceGenerators;

[Generator]
internal class FluidApiSourceGenerator : IIncrementalGenerator
{
	private readonly FluidGeneratorService _generatorService;

	public FluidApiSourceGenerator()
	{
		IDeserializer deserializer = new DeserializerBuilder().WithNamingConvention(NullNamingConvention.Instance).Build();
		_generatorService = new(deserializer);
	}

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{

		//SpinWait.SpinUntil(() => Debugger.IsAttached); // Manually attach debugger here

		IncrementalValuesProvider<AdditionalText> extraTexts = context.AdditionalTextsProvider.Where(f => f.Path.EndsWith(".fluid.yml"));
		IncrementalValuesProvider<(string Name, string Content)> namesAndContents = extraTexts.Select((text, cancellationToken)
																										  => (Name: Path.GetFileNameWithoutExtension(text.Path),
																											  Content: text.GetText(cancellationToken)!.ToString()));

		context.RegisterSourceOutput(namesAndContents, (spc, nameAndContent) =>
													   {
														   string generatedSource = _generatorService.Generate(nameAndContent.Content);
														   spc.AddSource($"{nameAndContent.Name}.g.cs", generatedSource);
													   });
	}
}
