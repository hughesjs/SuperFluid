using SuperFluid.Internal.Services;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SuperFluid.Tests.SourceGenerators;

public class FluidGeneratorServiceTests
{
	private readonly string _rawYml = File.ReadAllText("../../../DemoApiDefinition.fluid.yml");
	private readonly string _agh = File.ReadAllText("../../../agh.fluid.yml");
	
	[Fact]
	public void CanGenerateDemoApi()
	{
		IDeserializer              deserializer = new DeserializerBuilder().WithNamingConvention(NullNamingConvention.Instance).Build();
		FluidGeneratorService      service      = new(deserializer, new());
		Dictionary<string, string> result       = service.Generate(_rawYml);
		result["ICanLockOrEnter.fluid.g.cs"].ShouldBe(CanLockOrEnterSource);
		result["ICanUnlock.fluid.g.cs"].ShouldBe(CanUnlockSource);
		result["ICanStartOrExit.fluid.g.cs"].ShouldBe(CanStartOrExitSource);
		result["ICanStopOrBuild.fluid.g.cs"].ShouldBe(CanStopOrBuildSource);
		result["ICarActor.fluid.g.cs"].ShouldBe(CarActorSource);
	}
	
	[Fact]
	public void Agh()
	{
		IDeserializer              deserializer = new DeserializerBuilder().WithNamingConvention(NullNamingConvention.Instance).Build();
		FluidGeneratorService      service      = new(deserializer, new());
		Dictionary<string, string> result       = service.Generate(_agh);
	}

	private const string CanLockOrEnterSource = """
										namespace SuperFluid.Tests.Cars;

										public interface ICanLockOrEnter
										{
											public ICanUnlock Lock();
											public ICanStartOrExit Enter();
										}
										""";

	private const string CanUnlockSource = """
										namespace SuperFluid.Tests.Cars;

										public interface ICanUnlock
										{
											public ICanLockOrEnter Unlock();
										}
										""";
	
	private const string CanStartOrExitSource = """
										namespace SuperFluid.Tests.Cars;

										public interface ICanStartOrExit
										{
											public ICanStopOrBuild Start(int speed, string direction);
											public ICanLockOrEnter Exit();
										}
										""";

	private const string CanStopOrBuildSource = """
										namespace SuperFluid.Tests.Cars;

										public interface ICanStopOrBuild
										{
											public ICanStartOrExit Stop();
											public string Build(string color);
										}
										""";
	
	private const string CarActorSource = """
										namespace SuperFluid.Tests.Cars;

										public interface ICarActor: ICanLockOrEnter,ICanUnlock,ICanStartOrExit,ICanStopOrBuild
										{
											public static abstract ICanUnlock Initialize();
										}
										""";
}
