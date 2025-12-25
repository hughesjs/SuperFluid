using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using SuperFluid.Internal.Comparers;
using SuperFluid.Internal.Services;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SuperFluid.Internal.SourceGenerators;

[Generator]
internal class FluidApiSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //SpinWait.SpinUntil(() => Debugger.IsAttached); // Manually attach debugger here

        IncrementalValuesProvider<AdditionalText> extraTexts = context.AdditionalTextsProvider.Where(f => f.Path.EndsWith(".fluid.yml"));
        IncrementalValuesProvider<(string Name, string Content)> namesAndContents = extraTexts
            .Select((text, cancellationToken)
                => (Name: Path.GetFileNameWithoutExtension(text.Path),
                    Content: text.GetText(cancellationToken)!.ToString()))
            .WithComparer(new YamlContentComparer());

        context.RegisterSourceOutput(namesAndContents, (spc, nameAndContent) =>
        {
            IDeserializer deserializer = new DeserializerBuilder().WithNamingConvention(NullNamingConvention.Instance).Build();
            FluidGeneratorService generatorService = new(deserializer, new());

            Dictionary<string, string> generatedSource = generatorService.Generate(nameAndContent.Content);
            foreach (KeyValuePair<string, string> kvp in generatedSource)
            {
                spc.AddSource(kvp.Key, SourceText.From(kvp.Value, Encoding.UTF8));
            }
        });
    }
}