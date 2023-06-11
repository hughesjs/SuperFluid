using System.Diagnostics;
using System.Xml;
using Microsoft.CodeAnalysis;
using SuperFluid.Internal.Extensions;
using SuperFluid.Internal.Services;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SuperFluid.Internal.SourceGenerators;

[Generator]
internal class FluidApiSourceGenerator : ISourceGenerator
{
	private FluidGeneratorService _generatorService;
	public void Initialize(GeneratorInitializationContext context)
	{
		#if DEBUG
		SpinWait.SpinUntil(() => Debugger.IsAttached); // Manually attach debugger here
		#endif
		IDeserializer deserializer = new DeserializerBuilder().WithNamingConvention(NullNamingConvention.Instance).Build();
		_generatorService = new(deserializer);
	}

	public void Execute(GeneratorExecutionContext context)
	{
		List<string> apiDefinitionFiles = GetApiDefinitionFiles(context);
		string projectRoot = context.GetProjectDirectory();
		
		Dictionary<string, string> newSource = _generatorService.Generate(projectRoot, apiDefinitionFiles);
		
		foreach ((string fileName, string sourceCode) in newSource)
		{
			context.AddSource(fileName, sourceCode);
		}
	}

	private List<string> GetApiDefinitionFiles(GeneratorExecutionContext context)
	{
		string        csprojFile = context.GetProjectFile();
		XmlTextReader reader     = new(csprojFile);

		List<string> apiDefinitionFiles = new();
		while (reader.Read())
		{
			switch (reader.NodeType)
			{
				case XmlNodeType.Element:
				{
					if (reader.Name == "SuperFluidDefinition")
					{
						string apiDefinitionFile = reader.GetAttribute("Include") 
												?? throw new ("SuperFluidDefintion elements must contain an Include attribute");
						apiDefinitionFiles.Add(apiDefinitionFile);
					}
					continue;
				}
				case XmlNodeType.None:
				case XmlNodeType.Attribute:
				case XmlNodeType.Text:
				case XmlNodeType.CDATA:
				case XmlNodeType.EntityReference:
				case XmlNodeType.Entity:
				case XmlNodeType.ProcessingInstruction:
				case XmlNodeType.Comment:
				case XmlNodeType.Document:
				case XmlNodeType.DocumentType:
				case XmlNodeType.DocumentFragment:
				case XmlNodeType.Notation:
				case XmlNodeType.Whitespace:
				case XmlNodeType.SignificantWhitespace:
				case XmlNodeType.EndElement:
				case XmlNodeType.EndEntity:
				case XmlNodeType.XmlDeclaration:
				default: continue;
			}
		}

		return apiDefinitionFiles;
	}
}
