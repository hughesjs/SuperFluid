using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace SuperFluid.Tests.TestHelpers;

internal static class CompilationHelper
{
	/// <summary>
	/// Creates a minimal CSharpCompilation with required metadata references.
	/// Includes dummy types needed by the demo YAML (like INumber).
	/// </summary>
	public static CSharpCompilation CreateCompilation(string? source = null)
	{
		// Add dummy types that the demo YAML references as generic constraints
		string dummyTypes = """
			public interface INumber { }
			""";

		List<SyntaxTree> trees = [CSharpSyntaxTree.ParseText(dummyTypes)];

		if (source != null)
		{
			trees.Add(CSharpSyntaxTree.ParseText(source));
		}

		return CSharpCompilation.Create(
			assemblyName: "TestAssembly",
			syntaxTrees: trees,
			references: GetMetadataReferences(),
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
	}

	/// <summary>
	/// Creates an AdditionalText for testing .fluid.yml files.
	/// </summary>
	public static AdditionalText CreateAdditionalText(string fileName, string content)
	{
		return new InMemoryAdditionalText(fileName, content);
	}

	private static IEnumerable<MetadataReference> GetMetadataReferences()
	{
		// Core runtime assemblies
		yield return MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
		yield return MetadataReference.CreateFromFile(typeof(Console).Assembly.Location);
		yield return MetadataReference.CreateFromFile(typeof(System.Runtime.CompilerServices.DynamicAttribute).Assembly.Location);

		// Get all referenced assemblies from the current app domain
		string runtimePath = Path.GetDirectoryName(typeof(object).Assembly.Location) ?? "";

		// Add essential .NET assemblies
		foreach (string assemblyName in new[]
		{
			"System.Runtime.dll",
			"System.Collections.dll",
			"System.Linq.dll",
			"System.Console.dll",
			"netstandard.dll"
		})
		{
			string assemblyPath = Path.Combine(runtimePath, assemblyName);
			if (File.Exists(assemblyPath))
			{
				yield return MetadataReference.CreateFromFile(assemblyPath);
			}
		}
	}

	// Simple in-memory AdditionalText implementation
	private sealed class InMemoryAdditionalText(string path, string text) : AdditionalText
	{
		public override string Path => path;

		public override SourceText GetText(CancellationToken cancellationToken = default)
		{
			return SourceText.From(text, Encoding.UTF8);
		}
	}
}
