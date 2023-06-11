using Microsoft.CodeAnalysis;

namespace SuperFluid.Internal.Extensions;

internal static class GeneratorInitializationContextExtensions
{
	public static string GetProjectDirectory(this GeneratorExecutionContext context)
	{
		context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.ProjectDir", out string? projectDirectory);
		if (projectDirectory is null) throw new("Can't find project directory"); // TODO - Custom Exception
		return projectDirectory;
	}

	public static string GetProjectFile(this GeneratorExecutionContext context)
	{
		string   projectDirectory = context.GetProjectDirectory();
		string[] files            = Directory.GetFiles(projectDirectory);
		string   projectFile      = files.Single(f => f.EndsWith(".csproj"));
		return projectFile;
	}
}
