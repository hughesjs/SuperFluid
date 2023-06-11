using System.Reflection;
using System.Runtime.CompilerServices;
using SuperFluid.Internal.Services;

namespace SuperFluid.Tests.Conventions;

public class AccessModifierTests
{
	
	private const string InternalFragment = "SuperFluid.Internal";
    
	[Theory]
	[MemberData(nameof(PublicClassDataGenerator))]
	public void ClassesInInternalsNamespaceAreNotPublic(Type type)
	{
		type.IsPublic.ShouldBeFalse();
	}


	public static IEnumerable<object[]> PublicClassDataGenerator() => typeof(FluidGeneratorService).Assembly.GetTypes()
																								  .Where(t => t.Namespace != null && t.Namespace.Contains(InternalFragment))
																								  .Where(t => t.GetCustomAttribute<CompilerGeneratedAttribute>() is null)
																								  .Select(t => new object[] {t});
}
