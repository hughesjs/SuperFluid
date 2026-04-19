using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Shouldly;
using SuperFluid.Internal.Definitions;
using SuperFluid.Internal.Exceptions;
using SuperFluid.Internal.Services;

namespace SuperFluid.Tests.Services;

/// <summary>
/// Tests for <see cref="GrammarInterfaceReader"/>, which translates a Roslyn
/// <see cref="INamedTypeSymbol"/> into a <see cref="FluidApiDefinition"/> DTO.
/// </summary>
public class GrammarInterfaceReaderTests
{
    // Minimal attribute declarations needed by the reader — included in every test compilation
    // because the attributes are normally emitted by the source generator's post-initialisation
    // output, which is not running here.
    private const string AttributeSource = @"
namespace SuperFluid
{
    using System;
    [AttributeUsage(AttributeTargets.Interface)] public sealed class FluidApiGrammarAttribute : Attribute {}
    [AttributeUsage(AttributeTargets.Method)]    public sealed class InitialAttribute          : Attribute {}
    [AttributeUsage(AttributeTargets.Method)]    public sealed class TransitionsToAttribute    : Attribute { public TransitionsToAttribute(params string[] names) {} }
    [AttributeUsage(AttributeTargets.Method)]    public sealed class ReturnTypeAttribute        : Attribute { public ReturnTypeAttribute(Type t) {} }
}";

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds a <see cref="CSharpCompilation"/> that contains the attribute declarations plus
    /// the supplied grammar source, and returns the first interface symbol whose name matches
    /// <paramref name="interfaceName"/>.
    /// </summary>
    private static INamedTypeSymbol GetInterfaceSymbol(string grammarSource, string interfaceName)
    {
        List<SyntaxTree> trees =
        [
            CSharpSyntaxTree.ParseText(AttributeSource),
            CSharpSyntaxTree.ParseText(grammarSource)
        ];

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "GrammarReaderTestAssembly",
            syntaxTrees: trees,
            references: GetMetadataReferences(),
            options: new(OutputKind.DynamicallyLinkedLibrary));

        INamedTypeSymbol? symbol = compilation.GetTypeByMetadataName(interfaceName);
        symbol.ShouldNotBeNull($"Could not find type '{interfaceName}' in compilation.");
        return symbol!;
    }

    private static IEnumerable<MetadataReference> GetMetadataReferences()
    {
        yield return MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        yield return MetadataReference.CreateFromFile(typeof(Console).Assembly.Location);

        string runtimePath = System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location) ?? string.Empty;
        foreach (string assemblyName in new[] { "System.Runtime.dll", "System.Collections.dll", "netstandard.dll" })
        {
            string path = System.IO.Path.Combine(runtimePath, assemblyName);
            if (System.IO.File.Exists(path))
            {
                yield return MetadataReference.CreateFromFile(path);
            }
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [Fact]
    public void ReadProducesExpectedNameAndNamespace()
    {
        string source = @"
using SuperFluid;
namespace My.Grammars
{
    [FluidApiGrammar]
    internal interface IMinimalGrammar
    {
        [Initial, TransitionsTo]
        void Start();
    }
}";

        INamedTypeSymbol symbol = GetInterfaceSymbol(source, "My.Grammars.IMinimalGrammar");
        GrammarInterfaceReader reader = new();

        FluidApiDefinition definition = reader.Read(symbol);

        definition.Name.ShouldBe("IMinimal");
        definition.Namespace.ShouldBe("My.Grammars");
    }

    [Fact]
    public void ReadStripsGrammarSuffixFromInterfaceName()
    {
        string source = @"
using SuperFluid;
namespace Cars
{
    [FluidApiGrammar]
    internal interface ICarActorGrammar
    {
        [Initial, TransitionsTo(nameof(Unlock))]
        void Initialize();

        [TransitionsTo]
        void Unlock();
    }
}";

        INamedTypeSymbol symbol = GetInterfaceSymbol(source, "Cars.ICarActorGrammar");
        GrammarInterfaceReader reader = new();

        FluidApiDefinition definition = reader.Read(symbol);

        definition.Name.ShouldBe("ICarActor");
    }

    [Fact]
    public void ReadExtractsInitialStateMethod()
    {
        string source = @"
using SuperFluid;
namespace Test
{
    [FluidApiGrammar]
    internal interface IThingGrammar
    {
        [Initial, TransitionsTo(nameof(DoWork))]
        void Init();

        [TransitionsTo]
        void DoWork();

        [TransitionsTo]
        void AlsoDoWork();
    }
}";

        INamedTypeSymbol symbol = GetInterfaceSymbol(source, "Test.IThingGrammar");
        GrammarInterfaceReader reader = new();

        FluidApiDefinition definition = reader.Read(symbol);

        definition.InitialState.Name.ShouldBe("Init");
        definition.Methods.ShouldNotContain(m => m.Name == "Init");
        definition.Methods.Select(m => m.Name).ShouldContain("DoWork");
        definition.Methods.Select(m => m.Name).ShouldContain("AlsoDoWork");
    }

    [Fact]
    public void ReadExtractsTransitionsFromAttributeArguments()
    {
        string source = @"
using SuperFluid;
namespace Test
{
    [FluidApiGrammar]
    internal interface IOrderGrammar
    {
        [Initial, TransitionsTo(nameof(Pay), nameof(Cancel))]
        void Place();

        [TransitionsTo(nameof(Ship))]
        void Pay();

        [TransitionsTo]
        void Ship();

        [TransitionsTo]
        void Cancel();
    }
}";

        INamedTypeSymbol symbol = GetInterfaceSymbol(source, "Test.IOrderGrammar");
        GrammarInterfaceReader reader = new();

        FluidApiDefinition definition = reader.Read(symbol);

        definition.InitialState.Name.ShouldBe("Place");
        definition.InitialState.CanTransitionTo.ShouldBe(["Pay", "Cancel"]);

        FluidApiMethodDefinition? payMethod = definition.Methods.FirstOrDefault(m => m.Name == "Pay");
        payMethod.ShouldNotBeNull();
        payMethod!.CanTransitionTo.ShouldBe(["Ship"]);

        FluidApiMethodDefinition? shipMethod = definition.Methods.FirstOrDefault(m => m.Name == "Ship");
        shipMethod.ShouldNotBeNull();
        shipMethod!.CanTransitionTo.ShouldBeEmpty();
    }

    [Fact]
    public void ReadExtractsArgumentsWithDefaults()
    {
        string source = @"
using SuperFluid;
namespace Test
{
    [FluidApiGrammar]
    internal interface IDriveGrammar
    {
        [Initial, TransitionsTo(nameof(Drive))]
        void Start();

        [TransitionsTo]
        void Drive(int speed, string direction = ""Forward"", bool hotwire = false);
    }
}";

        INamedTypeSymbol symbol = GetInterfaceSymbol(source, "Test.IDriveGrammar");
        GrammarInterfaceReader reader = new();

        FluidApiDefinition definition = reader.Read(symbol);

        FluidApiMethodDefinition? driveMethod = definition.Methods.FirstOrDefault(m => m.Name == "Drive");
        driveMethod.ShouldNotBeNull();

        List<FluidApiArgumentDefinition> args = driveMethod!.Arguments;
        args.Count.ShouldBe(3);

        // Declaration order is preserved
        args[0].Name.ShouldBe("speed");
        args[0].Type.ShouldBe("int");
        args[0].DefaultValue.ShouldBeNull();

        args[1].Name.ShouldBe("direction");
        args[1].Type.ShouldBe("string");
        // Matches the YAML convention: default value stored as a C# code literal with surrounding quotes
        args[1].DefaultValue.ShouldBe("\"Forward\"");

        args[2].Name.ShouldBe("hotwire");
        args[2].Type.ShouldBe("bool");
        args[2].DefaultValue.ShouldBe("false");
    }

    [Fact]
    public void ReadExtractsGenericConstraintsInCorrectOrder()
    {
        // Using IDisposable instead of INumber<T> to keep the test compilation self-contained
        string source = @"
using System;
using SuperFluid;
namespace Test
{
    [FluidApiGrammar]
    internal interface IGenericGrammar
    {
        [Initial, TransitionsTo(nameof(Process))]
        void Start();

        [TransitionsTo]
        void Process<T>(T item) where T : class, IDisposable, new();
    }
}";

        INamedTypeSymbol symbol = GetInterfaceSymbol(source, "Test.IGenericGrammar");
        GrammarInterfaceReader reader = new();

        FluidApiDefinition definition = reader.Read(symbol);

        FluidApiMethodDefinition? processMethod = definition.Methods.FirstOrDefault(m => m.Name == "Process");
        processMethod.ShouldNotBeNull();
        processMethod!.GenericArguments.Count.ShouldBe(1);

        FluidGenericArgumentDefinition tParam = processMethod.GenericArguments[0];
        tParam.Name.ShouldBe("T");

        // Order: class first, then type constraints, then new() last
        tParam.Constraints[0].ShouldBe("class");
        tParam.Constraints[tParam.Constraints.Count - 1].ShouldBe("new()");
        tParam.Constraints.ShouldContain(c => c.Contains("IDisposable"));
    }

    [Fact]
    public void ReadExtractsXmlDocSummaryAsDescription()
    {
        string source = @"
using SuperFluid;
namespace Test
{
    /// <summary>Car actor grammar definition.</summary>
    [FluidApiGrammar]
    internal interface IDocGrammar
    {
        /// <summary>Initialises the workflow.</summary>
        [Initial, TransitionsTo(nameof(Finalise))]
        void Init();

        [TransitionsTo]
        void Finalise();
    }
}";

        // Doc comments require the GenerateDocumentationFile option — pass explicit parse options
        List<SyntaxTree> trees =
        [
            CSharpSyntaxTree.ParseText(AttributeSource),
            CSharpSyntaxTree.ParseText(source, new CSharpParseOptions())
        ];

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "DocTestAssembly",
            syntaxTrees: trees,
            references: GetMetadataReferences(),
            options: new CSharpCompilationOptions(
                outputKind: OutputKind.DynamicallyLinkedLibrary,
                xmlReferenceResolver: new XmlFileResolver(null)));

        INamedTypeSymbol? symbol = compilation.GetTypeByMetadataName("Test.IDocGrammar");
        symbol.ShouldNotBeNull();

        GrammarInterfaceReader reader = new();
        FluidApiDefinition definition = reader.Read(symbol!);

        definition.Description.ShouldBe("Car actor grammar definition.");
        definition.InitialState.Description.ShouldBe("Initialises the workflow.");
    }

    [Fact]
    public void ReadHandlesReturnTypeAttribute()
    {
        string source = @"
using SuperFluid;
namespace Test
{
    [FluidApiGrammar]
    internal interface IBuildGrammar
    {
        [Initial, TransitionsTo(nameof(Build))]
        void Start();

        [TransitionsTo, ReturnType(typeof(string))]
        void Build(string color);
    }
}";

        INamedTypeSymbol symbol = GetInterfaceSymbol(source, "Test.IBuildGrammar");
        GrammarInterfaceReader reader = new();

        FluidApiDefinition definition = reader.Read(symbol);

        FluidApiMethodDefinition? buildMethod = definition.Methods.FirstOrDefault(m => m.Name == "Build");
        buildMethod.ShouldNotBeNull();

        // The fully-qualified display string for System.String
        buildMethod!.ReturnType.ShouldNotBeNull();
        buildMethod.ReturnType!.ShouldContain("string");
    }

    [Fact]
    public void ReadThrowsMissingInitialMethodExceptionWhenNoInitialMethodPresent()
    {
        string source = @"
using SuperFluid;
namespace Test
{
    [FluidApiGrammar]
    internal interface IOrphanGrammar
    {
        [TransitionsTo(nameof(B))]
        void A();

        [TransitionsTo]
        void B();
    }
}";

        INamedTypeSymbol symbol = GetInterfaceSymbol(source, "Test.IOrphanGrammar");
        GrammarInterfaceReader reader = new();

        MissingInitialMethodException ex = Should.Throw<MissingInitialMethodException>(() => reader.Read(symbol));
        ex.GrammarInterfaceName.ShouldBe("IOrphanGrammar");
    }
}
