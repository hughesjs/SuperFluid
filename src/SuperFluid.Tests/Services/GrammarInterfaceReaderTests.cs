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
    public void ReadUnmanagedConstraintDoesNotAlsoEmitStruct()
    {
        // Roslyn sets both HasValueTypeConstraint and HasUnmanagedTypeConstraint for
        // `where T : unmanaged` (since unmanaged implies struct). Emitting both keywords
        // would produce `where T : struct, unmanaged`, which is a C# compile error (CS8331).
        string source = @"
using SuperFluid;
namespace Test
{
    [FluidApiGrammar]
    internal interface IUnmanagedGrammar
    {
        [Initial, TransitionsTo(nameof(Process))]
        void Start();

        [TransitionsTo]
        void Process<T>(T item) where T : unmanaged;
    }
}";

        INamedTypeSymbol symbol = GetInterfaceSymbol(source, "Test.IUnmanagedGrammar");
        GrammarInterfaceReader reader = new();

        FluidApiDefinition definition = reader.Read(symbol);

        FluidApiMethodDefinition processMethod = definition.Methods.Single(m => m.Name == "Process");
        FluidGenericArgumentDefinition tParam = processMethod.GenericArguments[0];

        tParam.Constraints.ShouldContain("unmanaged");
        tParam.Constraints.ShouldNotContain("struct");
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

        // Primitive keyword mapping produces the C# keyword form, not the global:: alias
        buildMethod!.ReturnType.ShouldBe("string");
    }

    [Fact]
    public void ReadThrowsMultipleInitialMethodsExceptionWhenMoreThanOneInitialPresent()
    {
        string source = @"
using SuperFluid;
namespace Test
{
    [FluidApiGrammar]
    internal interface IAmbiguousGrammar
    {
        [Initial, TransitionsTo(nameof(Go))]
        void Start();

        [Initial, TransitionsTo(nameof(Go))]
        void Begin();

        [TransitionsTo]
        void Go();
    }
}";

        INamedTypeSymbol symbol = GetInterfaceSymbol(source, "Test.IAmbiguousGrammar");
        GrammarInterfaceReader reader = new();

        MultipleInitialMethodsException ex = Should.Throw<MultipleInitialMethodsException>(() => reader.Read(symbol));
        ex.GrammarInterfaceName.ShouldBe("IAmbiguousGrammar");
        ex.MethodNames.ShouldBe(new[] { "Start", "Begin" });
    }

    [Fact]
    public void ReadEmitsTypedLiteralSuffixesForNumericDefaults()
    {
        string source = @"
using SuperFluid;
namespace Test
{
    [FluidApiGrammar]
    internal interface INumericGrammar
    {
        [Initial, TransitionsTo(nameof(Process))]
        void Start();

        [TransitionsTo]
        void Process(
            float f = 3.14f,
            double d = 2.5,
            decimal m = 9.99m,
            long big = 5000000000L,
            ulong huge = 18000000000000000000UL,
            uint positive = 4000000000U);
    }
}";

        INamedTypeSymbol symbol = GetInterfaceSymbol(source, "Test.INumericGrammar");
        GrammarInterfaceReader reader = new();

        FluidApiDefinition definition = reader.Read(symbol);
        FluidApiMethodDefinition processMethod = definition.Methods.Single(m => m.Name == "Process");

        // Arguments are reordered defaults-last inside FluidApiMethod; the DTO list here is declaration order
        string FindDefault(string name) =>
            processMethod.Arguments.Single(a => a.Name == name).DefaultValue!;

        FindDefault("f").ShouldEndWith("F");
        FindDefault("d").ShouldEndWith("D");
        FindDefault("m").ShouldEndWith("M");
        FindDefault("big").ShouldEndWith("L");
        FindDefault("huge").ShouldEndWith("UL");
        FindDefault("positive").ShouldEndWith("U");

        // Spot-check exact values — round-trip format preserves precision
        FindDefault("m").ShouldBe("9.99M");
        FindDefault("big").ShouldBe("5000000000L");
        FindDefault("huge").ShouldBe("18000000000000000000UL");
        FindDefault("positive").ShouldBe("4000000000U");
    }

    [Fact]
    public void ReadResolvesEnumMemberNameForUnsignedLongEnumAboveInt64Max()
    {
        string source = @"
using SuperFluid;
namespace Test
{
    public enum BigFlags : ulong
    {
        None = 0,
        HighBit = 9223372036854775808UL  // 2^63, above Int64.MaxValue
    }

    [FluidApiGrammar]
    internal interface IEnumGrammar
    {
        [Initial, TransitionsTo(nameof(Use))]
        void Start();

        [TransitionsTo]
        void Use(BigFlags flag = BigFlags.HighBit);
    }
}";

        INamedTypeSymbol symbol = GetInterfaceSymbol(source, "Test.IEnumGrammar");
        GrammarInterfaceReader reader = new();

        FluidApiDefinition definition = reader.Read(symbol);
        FluidApiMethodDefinition useMethod = definition.Methods.Single(m => m.Name == "Use");
        string flagDefault = useMethod.Arguments.Single(a => a.Name == "flag").DefaultValue!;

        flagDefault.ShouldEndWith(".HighBit");
        flagDefault.ShouldNotContain("9223372036854775808");
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
