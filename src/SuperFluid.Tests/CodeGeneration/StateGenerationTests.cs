using SuperFluid.Public.SourceGeneration;

namespace SuperFluid.Tests.CodeGeneration;

public class StateGenerationTests
{
    [Fact]
    public void ICanGenerateExists()
    {
        typeof(ISmoke).ShouldNotBeNull();
    }
   
    [FluentApiDefinition]
    private class TestFluentApi: IFluentApiDefinition
    {
        public string BuilderClassName => "TestApiBuilder";
        
        public static void DefineApi()
        {
            throw new NotImplementedException();
        }
    }
}