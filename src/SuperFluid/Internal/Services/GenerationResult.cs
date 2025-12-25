using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace SuperFluid.Internal.Services;

internal record GenerationResult(
	Dictionary<string, string>? GeneratedFiles,
	ImmutableArray<Diagnostic> Diagnostics,
	bool IsSuccess)
{
	public static GenerationResult Success(Dictionary<string, string> files)
		=> new(files, ImmutableArray<Diagnostic>.Empty, true);

	public static GenerationResult Failure(params Diagnostic[] diagnostics)
		=> new(null, ImmutableArray.Create(diagnostics), false);
}
