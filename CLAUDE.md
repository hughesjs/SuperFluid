# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SuperFluid is a C# Roslyn source generator library that generates fluent API interfaces with enforced grammar from YAML definitions. It allows developers to define state machines via YAML and automatically generates the necessary interfaces to enforce valid method call sequences at compile time.

## Build and Test Commands

### Building
```bash
dotnet build src/SuperFluid.sln
```

### Running Tests
```bash
# Run all tests
dotnet test src/SuperFluid.sln

# Run tests with GitHub Actions logger (used in CI)
dotnet test --logger GitHubActions src/SuperFluid.sln

# Run a specific test
dotnet test src/SuperFluid.Tests/SuperFluid.Tests.csproj --filter "FullyQualifiedName~TestMethodName"
```

### Package Generation
The project automatically generates a NuGet package on build (configured via `GeneratePackageOnBuild` in the .csproj).

## Architecture

### Source Generation Pipeline

The source generation happens in this sequence:

1. **FluidApiSourceGenerator** (`Internal/SourceGenerators/FluidApiSourceGenerator.cs`) - Entry point
   - Roslyn incremental source generator that hooks into the compilation process
   - Filters for `.fluid.yml` files from AdditionalFiles
   - Delegates to FluidGeneratorService for code generation

2. **FluidGeneratorService** (`Internal/Services/FluidGeneratorService.cs`) - Orchestration
   - Deserialises YAML into FluidApiDefinition objects (using YamlDotNet)
   - Parses definitions into FluidApiModel via FluidApiDefinitionParser
   - Generates C# interface source code from the model
   - Creates two types of interfaces:
     - State interfaces (e.g., `ICanUnlockOrEnter`) - one per unique state
     - Compound root interface (e.g., `ICarActor`) - inherits all state interfaces

3. **FluidApiDefinitionParser** (`Internal/Parsers/FluidApiDefinitionParser.cs`) - Core logic
   - Converts YAML definitions into internal model representation
   - Builds state machine by computing minimal set of unique states
   - A "state" is defined by the set of methods that can be called (transition set)
   - Uses HashSetSetEqualityComparer to identify duplicate transition sets
   - State names are auto-generated as `ICan{Method1}Or{Method2}...`

### Key Data Structures

- **Definitions** (`Internal/Definitions/`) - Direct YAML schema mappings
  - FluidApiDefinition, FluidApiMethodDefinition, FluidApiArgumentDefinition, FluidGenericArgumentDefinition

- **Model** (`Internal/Model/`) - Internal representation after parsing
  - FluidApiModel - Root model containing all states and methods
  - FluidApiState - Represents a unique combination of available method transitions
  - FluidApiMethod - Method with return type, arguments, generic constraints, and valid transitions
  - FluidApiArgument, FluidGenericArgument

### State Generation Algorithm

The core algorithm (in FluidApiDefinitionParser.GetMinimalStates):
1. Extract all unique transition sets from methods
2. Create one FluidApiState per unique transition set
3. For each state, map methods to their destination states
4. Filter out empty states (with no available methods)

This ensures compile-time enforcement: each state interface only exposes methods that are valid from that state.

## YAML Grammar Format

Grammar files must:
- End with `.fluid.yml` suffix
- Be registered in csproj as `<AdditionalFiles Include="path/to/file.fluid.yml" />`
- Follow the FluidApiDefinition schema (see README.md for full example)

Key YAML structure:
- `Name` - Root interface name
- `Namespace` - Generated interface namespace
- `InitialState` - Entry point method and its transitions
- `Methods` - List of all available methods with:
  - `CanTransitionTo` - Array of valid next method names
  - `Arguments` - Optional parameters (with default values)
  - `GenericArguments` - Optional generic type parameters with constraints
  - `ReturnType` - Optional explicit return type (defaults to next state interface)

## Project Structure

- `src/SuperFluid/` - Main source generator library (.NET Standard 2.0)
  - Targets netstandard2.0 for broad compatibility with Roslyn
  - Uses YamlDotNet dependency (bundled with analyzer via custom MSBuild target)
  - Packaged as analyzer/source generator NuGet package

- `src/SuperFluid.Tests/` - xUnit test project (.NET 9.0)
  - Contains example grammar: `DemoApiDefinition.fluid.yml`
  - Tests use Shouldly for assertions
  - Uses GitHubActionsTestLogger for CI integration

## Development Notes

### Target Framework
- Main library uses netstandard2.0 (required for Roslyn source generators)
- Tests use net9.0 (current .NET version)

### Source Generator Debugging
There's a commented-out debugger attachment line in FluidApiSourceGenerator.cs:
```csharp
//SpinWait.SpinUntil(() => Debugger.IsAttached);
```
Uncomment this to manually attach a debugger when developing the generator.

### Roslyn Feedback Limitations
Roslyn source generation errors are notoriously hard to surface. In JetBrains Rider, check `Problems > Toolset, Environment` for generation errors. An analyzer for better feedback is planned but not yet implemented.

### YamlDotNet Integration
YamlDotNet is included as a dependency but must be bundled with the analyzer. This is handled by the custom MSBuild target `GetDependencyTargetPaths` which includes YamlDotNet.dll in the analyzer package path.

## Contributing Workflow

Per CONTRIBUTING.md:
1. Raise an issue first to discuss the change
2. Fork and create branch from `master`
3. Add tests for new code
4. Ensure tests pass and code follows standard C# style
5. Submit PR

## CI/CD

- CI: `.github/workflows/dotnet-ci.yml` - Runs `dotnet test` on PR and push to master
- CD: `.github/workflows/dotnet-cd.yml` - Publishes to NuGet using semantic versioning via GitHub tags
- Uses .NET 9.0.x SDK in GitHub Actions

## Known Modernisation Opportunities

This project was created when Roslyn source generators were new. A comprehensive analysis (December 2025) identified several modernisation opportunities. Most have since been addressed; see `MODERNISATION_CHECKLIST.md` for the authoritative status.

### Remaining items
- **Missing analyzers** - No compile-time validation of YAML syntax despite plans mentioned in README
- **Generated code uses traditional namespace blocks** rather than file-scoped namespaces
- **No XML documentation** - Generated interfaces lack summary comments
- State-name generation can still produce very long names for complex state machines (deterministic but verbose)

### April 2026 follow-up fixes (issues 1–5 from review)
1. ✅ **FluidApiState.Name determinism** — method names inside `ICan…Or…` state names are now sorted alphabetically (ordinal). **Breaking change:** previously-generated interfaces are renamed (e.g., `ICanLockOrEnter` → `ICanEnterOrLock`).
2. ✅ **FluidApiMethod.Arguments / GenericArguments** — now `ImmutableArray<T>` instead of `HashSet<T>` to preserve ordering in generated method signatures.
3. ✅ **HashSetSetEqualityComparer GetHashCode** — rewritten as an XOR reduction seeded with `Count`, so the hash is truly order-independent regardless of element hash collisions.
4. ✅ **Typed exceptions for diagnostic mapping** — `MethodNotFoundException`, `DuplicateMethodNameException`, `EmptyConstraintsException` replace message-string matching in `FluidGeneratorService`. Explicit null checks cover all required definition fields.
5. ✅ **YamlContentComparer removed** — default `ValueTuple<string,string>` equality is equivalent; the comparer was redundant.