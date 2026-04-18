# SuperFluid Modernisation Checklist

This document tracks modernisation improvements for SuperFluid, identified December 2025.
Work through these in order, checking off items as completed.

## Priority 1: Critical Issues (Blocking/Reliability)

These affect core functionality and should be fixed first.

- [x] **1.1 Fix HashSetSetEqualityComparer hash code bug** ✅
  - File: `src/SuperFluid/Internal/EqualityComparers/HashSetSetEqualityComparer.cs:16`
  - Issue: Uses `set.Comparer.GetHashCode(t)` instead of `t.GetHashCode()`
  - Issue: Hash distribution could be poor
  - Fix: Use `HashCode` struct with deterministic ordering
  - Risk: Low (fix is straightforward, bug may not be causing issues yet)
  - **COMPLETED**: Implemented modern `HashCode` struct with deterministic ordering via `OrderBy()`. Added Microsoft.Bcl.HashCode package with proper analyzer bundling. Created 20 comprehensive unit tests.
  - **FOLLOW-UP (April 2026)**: The `OrderBy(c => c.GetHashCode())` approach was stable but operated on an unordered HashSet source, so hash-colliding elements could produce different sort orders for equal sets. Rewrote as XOR reduction seeded with `Count` — truly order-independent regardless of collisions. Existing tests still pass.

- [x] **1.2 Add diagnostic reporting to source generator** ✅
  - File: `src/SuperFluid/Internal/SourceGenerators/FluidApiSourceGenerator.cs`
  - Issue: No error handling - failures are silent or crash compilation
  - Fix: Define diagnostic descriptors (SF0001, SF0002, etc.)
  - Fix: Wrap generation in try-catch and report diagnostics
  - Risk: Medium (requires defining error cases and testing)
  - **COMPLETED**: Implemented 13 diagnostic codes (SF0001–SF0013) in `Internal/Diagnostics/DiagnosticDescriptors.cs`. `FluidGeneratorService` wraps deserialisation, validation, and parsing in try/catch, returning `GenerationResult` with structured diagnostics. Covered by `GenerateReportsSF…` tests.
  - **FOLLOW-UP (April 2026)**: Replaced exception-message string matching with typed exceptions (`MethodNotFoundException`, `DuplicateMethodNameException`, `EmptyConstraintsException`) in `Internal/Exceptions/`. Extended explicit null checks to all required definition fields (`InitialState`, `Methods`) so missing-field diagnostics survive YamlDotNet version changes.

- [x] **1.3 Add incremental generator caching** ✅
  - File: `src/SuperFluid/Internal/SourceGenerators/FluidApiSourceGenerator.cs:25`
  - Issue: No equivalence comparer on `.Select()` - regenerates on every keystroke
  - Fix: Create `YamlContentComparer` and add `.WithComparer()`
  - Risk: Low (straightforward implementation)
  - **COMPLETED**: Created `YamlContentComparer` implementing `IEqualityComparer<(string Name, string Content)>`. Uses `HashCode.Combine()` for hash generation. Added `.WithComparer()` to Select pipeline in source generator. Generator now only regenerates when YAML content actually changes.

## Priority 2: Important Improvements (Code Quality)

These improve maintainability and testing.

- [x] **2.1 Remove mutable state from source generator** ✅
  - File: `src/SuperFluid/Internal/SourceGenerators/FluidApiSourceGenerator.cs:12-17`
  - Issue: `_generatorService` field shared across invocations
  - Fix: Create service instance inside `RegisterSourceOutput` callback
  - Risk: Low (simple refactor)
  - **COMPLETED**: Removed `_generatorService` field and constructor. Moved deserializer and service creation inside `RegisterSourceOutput` callback. Each YAML file now gets fresh instances, ensuring thread-safety and statelessness.

- [x] **2.2 Convert FluidApiArgument to record** ✅
  - File: `src/SuperFluid/Internal/Model/FluidApiArgument.cs`
  - Issue: Uses class with manual constructor instead of modern record syntax
  - Fix: Convert to `record` with primary constructor
  - Risk: Low (data structure is simple)
  - **COMPLETED**: Converted to `internal record FluidApiArgument(string Name, string Type, string? DefaultValue)`. Maintained parameter order for backward compatibility. Added value equality semantics.

- [x] **2.3 Convert FluidGenericArgument to record** ✅
  - File: `src/SuperFluid/Internal/Model/FluidGenericArgument.cs`
  - Issue: Uses class with manual constructor
  - Fix: Convert to `record` with primary constructor and validation
  - Risk: Low (keep validation logic in secondary constructor)
  - **COMPLETED**: Converted to record with primary constructor `(string Name, HashSet<string> Constraints)`. Added secondary constructor and static `ValidateAndConvert` helper method to preserve validation logic. Throws `ArgumentException` for empty constraints as before.

- [x] **2.4 Modernise null checking in HashSetSetEqualityComparer** ✅
  - File: `src/SuperFluid/Internal/EqualityComparers/HashSetSetEqualityComparer.cs:7`
  - Issue: Uses `ReferenceEquals(x, null)` instead of pattern matching
  - Fix: Use `x is not null && y is not null` pattern
  - Risk: Very low (simple modernisation)
  - **COMPLETED**: Updated line 7 to use modern pattern matching (`x is not null && y is not null`) for consistency with rest of file.

- [x] **2.5 Add proper source generator testing infrastructure** ✅
  - Files: `src/SuperFluid.Tests/`
  - Issue: Uses string comparison instead of proper Roslyn testing
  - Fix: Add `Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing` package
  - Fix: Create proper incremental generator tests
  - Fix: Add diagnostic reporting tests
  - Risk: Medium (requires learning testing framework)
  - **COMPLETED**: Added comprehensive Roslyn testing infrastructure using direct `CSharpGeneratorDriver` approach (deprecated Testing packages not used). Created `CompilationHelper.cs` with factory methods, added 6 new tests in `FluidApiSourceGeneratorTests.cs` covering: basic generation, compilation verification, multiple runs, different YAML outputs, multiple files, and file filtering. Deleted stub file `GeneratorExecutionContextTests.cs`. All 30 tests pass. Diagnostic tests deferred until item 1.2 adds diagnostic reporting.

## Priority 3: Polish & Enhancements

Nice-to-have improvements that enhance developer experience.

- [x] **3.1 Use SourceText.From() with encoding** ✅
  - File: `src/SuperFluid/Internal/SourceGenerators/FluidApiSourceGenerator.cs:33`
  - Issue: Adding source as string instead of SourceText
  - Fix: `spc.AddSource(kvp.Key, SourceText.From(kvp.Value, Encoding.UTF8))`
  - Risk: Very low (minor performance improvement)
  - **COMPLETED**: Updated to use `SourceText.From(kvp.Value, Encoding.UTF8)`. Added required using statements for `System.Text` and `Microsoft.CodeAnalysis.Text`. Modern Roslyn best practice with explicit UTF-8 encoding.

- [ ] **3.2 Generate file-scoped namespaces**
  - File: `src/SuperFluid/Internal/Services/FluidGeneratorService.cs:34-69`
  - Issue: Generated code uses traditional namespace blocks
  - Fix: Change generated code to use `namespace X;` instead of `namespace X { }`
  - Risk: Very low (cosmetic change to generated code)

- [x] **3.3 Update package metadata** ✅
  - File: `src/SuperFluid/SuperFluid.csproj:14`
  - Issue: Uses deprecated `<PackageLicense>MIT</PackageLicense>`
  - Fix: Change to `<PackageLicenseExpression>MIT</PackageLicenseExpression>`
  - Risk: Very low (metadata only)
  - **COMPLETED**: Changed from deprecated `<PackageLicense>` to modern `<PackageLicenseExpression>`. Complies with modern NuGet packaging standards.

- [x] **3.4 Update Roslyn package versions** ✅
  - File: `src/SuperFluid/SuperFluid.csproj:38`
  - Issue: `Microsoft.CodeAnalysis.CSharp` is 4.12.0 (latest is 5.0.0)
  - Fix: Update to latest stable version
  - Risk: Low (check for breaking changes in release notes)
  - **COMPLETED**: Updated from version 4.12.0 to 5.0.0. No breaking API changes detected. All 26 tests pass. Compatible with netstandard2.0 target.

- [x] **3.5 Add .editorconfig** ✅
  - File: `.editorconfig` (created)
  - Issue: No consistent code style configuration
  - Fix: Add standard C# .editorconfig with project conventions
  - Risk: Very low (improves contributor experience)
  - **COMPLETED**: Created .editorconfig at repository root using ArchonAnalysers configuration. Includes file-scoped namespace enforcement, var prohibition, naming conventions (_camelCase for private fields, PascalCase for public), elevated nullable diagnostics, and comprehensive formatting rules.

- [x] **3.6 Remove or implement stub tests** ✅
  - File: `src/SuperFluid.Tests/Extensions/GeneratorExecutionContextTests.cs`
  - Issue: Contains empty test methods with `//TODO` comments
  - Fix: Either implement tests or delete the file
  - Risk: Very low (cleanup task)
  - **COMPLETED**: Deleted `GeneratorExecutionContextTests.cs` stub file as part of item 2.5 implementation.

- [x] **3.7 Add global usings to test project** ✅
  - File: `src/SuperFluid.Tests/Usings.cs` (already exists)
  - Issue: Repetitive using statements across test files
  - Fix: Create GlobalUsings.cs with common namespaces
  - Risk: Very low (convenience improvement)
  - **COMPLETED**: Reviewed existing setup - already optimal. Usings.cs has Xunit and Shouldly globalized, `<ImplicitUsings>enable</ImplicitUsings>` provides common System namespaces, and each test file imports only its specific SuperFluid.Internal namespaces. No changes needed.

## Priority 4: Future Enhancements

Larger features that can be tackled later.

- [ ] **4.1 Add analyzers for YAML validation**
  - New files in `src/SuperFluid/Analyzers/`
  - Feature: Compile-time validation of `.fluid.yml` syntax
  - Feature: Code fix to add missing `AdditionalFiles` entry
  - Benefit: Better developer experience (mentioned in README as planned)
  - Risk: Medium (new feature area)

- [ ] **4.2 Add XML documentation support**
  - Files: YAML schema and generator
  - Feature: Optional `Description` fields in YAML that generate `/// <summary>` comments
  - Benefit: Self-documenting generated interfaces
  - Risk: Low (additive feature)

- [ ] **4.3 Improve state name generation**
  - File: `src/SuperFluid/Internal/Model/FluidApiState.cs:8`
  - Issue: Auto-generated names can be very long for complex state machines
  - Feature: Optional custom state names in YAML
  - Feature: Maximum name length with ellipsis or hash-based names
  - Risk: Medium (changes core naming logic)

- [~] **4.4 Multi-target framework support** — WON'T DO
  - File: `src/SuperFluid/SuperFluid.csproj`
  - Feature: Target `net6.0` or `net8.0` alongside `netstandard2.0`
  - **Dropped (April 2026)**: Roslyn source generators must load as netstandard2.0 — that's what the Roslyn host picks up from `analyzers/dotnet/cs/`. A multi-targeted net8.0 assembly would sit in the nupkg unused, since `DevelopmentDependency=true` and `IncludeBuildOutput=false` explicitly exclude SuperFluid as a runtime library. Modern APIs would be unavailable at generator runtime regardless. No benefit for the current single-use (analyzer-only) packaging model.

---

## Working Notes

Use this section to track progress and decisions as we work through items.

### Session 1 - 24 December 2025

**Completed: 1.1 Fix HashSetSetEqualityComparer hash code bug**
- Discovered PolySharp doesn't include System.HashCode polyfill
- Added Microsoft.Bcl.HashCode v1.1.1 package with proper analyzer bundling (following YamlDotNet pattern)
- Updated GetHashCode() to use HashCode struct with deterministic ordering via OrderBy()
- Created comprehensive test suite: 20 tests covering equality, hash codes, and integration scenarios
- Tests include: collection expressions syntax (modern C#), null handling, order independence, dictionary usage, LINQ Distinct()
- All tests compile successfully

**Completed: 1.3 Add incremental generator caching**
- Created YamlContentComparer in new Internal/Comparers directory
- Implements IEqualityComparer<(string Name, string Content)> for tuple comparison
- Uses HashCode.Combine() for consistent hash generation based on both Name and Content
- Integrated into source generator pipeline via .WithComparer() on Select operator
- Generator now only regenerates when .fluid.yml content actually changes, not on every keystroke
- Verified build succeeds and comparer is properly wired up
- Note: Proper incremental generator testing deferred to item 2.5 (requires Microsoft.CodeAnalysis.Testing framework)

### Session 2 - 25 December 2025

**Completed: 2.4 Modernise null checking (trivial)**
- Updated HashSetSetEqualityComparer.cs line 7
- Changed from `!ReferenceEquals(x, null) && !ReferenceEquals(y, null)` to `x is not null && y is not null`
- Modern C# pattern matching for consistency with rest of file

**Completed: 2.2 Convert FluidApiArgument to record (easy)**
- Converted class to single-line record with primary constructor
- Final form: `internal record FluidApiArgument(string Name, string Type, string? DefaultValue)`
- Maintained parameter order (Name, Type, DefaultValue) to match existing calling code in FluidApiDefinitionParser
- Preserved DebuggerDisplay attribute
- Gained value equality semantics automatically

**Completed: 2.3 Convert FluidGenericArgument to record (medium)**
- Converted to record whilst preserving validation logic
- Primary constructor: `(string Name, HashSet<string> Constraints)`
- Secondary constructor delegates to primary via static `ValidateAndConvert` helper
- Helper validates constraints collection is non-empty and converts to HashSet
- Maintains original behaviour: throws ArgumentException for empty constraints
- All existing tests pass

**Completed: 2.1 Remove mutable state from source generator (medium)**
- Removed `_generatorService` field and constructor from FluidApiSourceGenerator
- Moved service creation inside `RegisterSourceOutput` callback
- Now creates fresh IDeserializer and FluidGeneratorService instances per YAML file
- Ensures thread-safety and statelessness in source generator
- Follows Roslyn best practices for incremental generators

**Verification:**
- All 4 changes completed in recommended order (2.4 → 2.2 → 2.3 → 2.1)
- Solution builds successfully: `dotnet build src/SuperFluid.sln`
- All 26 tests pass: existing 6 tests + 20 new HashSetSetEqualityComparer tests
- NuGet package generates successfully
- Test project now targets net10.0

### Session 3 - 25 December 2025

**Completed: 3.3 Update package metadata (trivial)**
- Changed line 14 in SuperFluid.csproj from deprecated `<PackageLicense>MIT</PackageLicense>` to modern `<PackageLicenseExpression>MIT</PackageLicenseExpression>`
- Complies with modern NuGet packaging standards
- Removes deprecation warning

**Completed: 3.5 Add .editorconfig (easy)**
- Created .editorconfig at repository root
- Fetched complete configuration from hughesjs/ArchonAnalysers repository (12,170 bytes)
- Key settings: file-scoped namespaces (warning), var prohibited (error), _camelCase for private fields
- Includes comprehensive C# formatting rules, naming conventions, and elevated nullable diagnostics
- Establishes consistent code style for contributors

**Completed: 3.7 Review global usings (trivial)**
- Reviewed existing Usings.cs and test project configuration
- Confirmed current setup is already optimal:
  - Xunit and Shouldly globalized in Usings.cs
  - `<ImplicitUsings>enable</ImplicitUsings>` provides common System namespaces automatically
  - Each test file imports only specific SuperFluid.Internal namespaces it needs
- No changes required

**Completed: 3.1 Use SourceText.From() with encoding (easy)**
- Updated FluidApiSourceGenerator.cs line 33 from `spc.AddSource(kvp.Key, kvp.Value)` to `spc.AddSource(kvp.Key, SourceText.From(kvp.Value, Encoding.UTF8))`
- Added required using statements: `System.Text` and `Microsoft.CodeAnalysis.Text`
- Modern Roslyn best practice with explicit UTF-8 encoding
- Minor performance improvement

**Completed: 3.4 Update Roslyn packages (low risk)**
- Updated Microsoft.CodeAnalysis.CSharp from version 4.12.0 to 5.0.0
- Roslyn 5.0.0 released alongside .NET 9
- No breaking API changes detected in source generator code
- SourceProductionContext.AddSource() signature unchanged and compatible

**Verification:**
- All 5 changes completed in order: 3.3 → 3.5 → 3.7 → 3.1 → 3.4
- Solution builds successfully: `dotnet build src/SuperFluid.sln` (1 pre-existing packaging warning)
- All 26 tests pass: `dotnet test src/SuperFluid.Tests/ --verbosity normal`
- NuGet package generates successfully: SuperFluid.0.0.1-alpha.nupkg
- Roslyn 5.0.0 compatible with netstandard2.0 target

### Session 4 - 18 April 2026

Follow-up fixes from the 1–5 review:

**Completed: Fix 5 — Remove redundant `YamlContentComparer`**
- Deleted `Internal/Comparers/YamlContentComparer.cs` and the `Comparers/` directory.
- Removed `.WithComparer(new YamlContentComparer())` and its using from `FluidApiSourceGenerator`. Default `ValueTuple<string,string>` equality gives the same caching behaviour.

**Completed: Fix 3 — Order-independent `HashSetSetEqualityComparer.GetHashCode`**
- Previous `OrderBy(c => c.GetHashCode())` was a stable sort operating on an unordered HashSet source, so hash-colliding elements could produce different sort orders for equal sets (violating the `Equals ⇒ same hash` contract).
- Replaced with an XOR reduction seeded with `set.Count`. XOR is commutative/associative, so iteration order cannot affect the result. All 20 existing order-independence tests still pass.

**Completed: Fix 1 — `FluidApiMethod.Arguments` / `GenericArguments` as `ImmutableArray<T>`**
- The constructor was ordering arguments (defaults last) and then discarding that ordering by storing in a `HashSet`. Generated method signatures only worked by coincidence of HashSet's implementation detail.
- Converted both collections to `ImmutableArray<T>` so ordering is preserved contractually. `CanTransitionTo` stays as `HashSet<FluidApiMethod>` because set semantics are still required for `Distinct` / dictionary keys.
- Updated `FluidGeneratorService` to use `.Length` instead of `.Count` for `ImmutableArray`.

**Completed: Fix 2 — Alphabetical sorting for deterministic state names**
- `FluidApiState.Name`, `GenerateStateSource`, and `GenerateCompoundInterface` now sort by name using `StringComparer.Ordinal` before aggregation/iteration. Equivalent transition sets authored in any YAML order produce identical interface names.
- **Breaking change**: demo interfaces renamed `ICanLockOrEnter` → `ICanEnterOrLock`, `ICanStartOrExit` → `ICanExitOrStart`, `ICanStopOrBuild` → `ICanBuildOrStop`. Compound base list now alphabetical.
- Test fixtures updated accordingly in `FluidGeneratorServiceTests` and `FluidApiSourceGeneratorTests`.

**Completed: Fix 4 — Typed exceptions replace message string matching**
- New `Internal/Exceptions/` directory with `MethodNotFoundException`, `DuplicateMethodNameException`, `EmptyConstraintsException`.
- Parser throws typed exceptions carrying structured data (`ReferencingMethod`, `MissingMethod`, `MethodName`, `GenericArgumentName`).
- `FluidGeneratorService` catches the typed exceptions instead of inspecting `ex.Message.Contains(...)`. SF0005 now reports the actual referencing method rather than `"unknown"`.
- Removed the fragile `catch (Exception) when (ex.Message.Contains("Property") || ex.Message.Contains("required"))` block and the obsolete `ExtractFieldName` helper.
- Extended explicit null checks to cover `InitialState` and `Methods`, so missing-required-field diagnostics survive any YamlDotNet behaviour change around `required` properties.
- `FluidGenericArgument.ValidateAndConvert` now threads the argument name through so the diagnostic reports which argument had empty constraints.

**Verification:**
- `dotnet build src/SuperFluid.sln` — 0 warnings, 0 errors.
- `dotnet test src/SuperFluid.sln` — all 47 tests pass.