using SuperFluid.Tests.Cars;

namespace SuperFluid.Tests.SourceGenerators;

public class GeneratedBuilderTests
{
    /// <summary>
    /// The real test is if this builds
    /// </summary>
    [Fact]
    public void CanGenerateDemoApi()
    {
        string states = CarActor.Initialize()
            .Unlock()
            .Enter()
            .Start(10, "right")
            .Stop()
            .Start(20, "left")
            .Stop()
            .Exit()
            .Enter()
            .Start(30, "right")
            .Stop()
            .Exit()
            .Lock()
            .Build("red");
        
        states.ShouldBe("Unlock Enter Start 10 right Stop Start 20 left Stop Exit Enter Start 30 right Stop Exit Lock Build red");
    }


    private class CarActor : ICarActor
    {
        private readonly List<string> _states = [];
        
        public ICanUnlockOrBuild Lock()
        {
            _states.Add("Lock");
            return this;
        }

        public ICanStartOrExit Enter()
        {
            _states.Add("Enter");
            return this;
        }

        ICanLockOrEnter ICanUnlockOrBuild.Unlock()
        { 
            _states.Add("Unlock");
            return this;
        }

        public static ICanUnlock Initialize()
        {
            return new CarActor();
        }

        public string Build(string colour)
        {
            _states.Add($"Build {colour}");
            return string.Join(" ", _states);
        }

        public ICanStop Start(int speed, string direction)
        {
            _states.Add($"Start {speed} {direction}");
            return this;
        }

        public ICanLockOrEnter Exit()
        {
            _states.Add("Exit");
            return this;
        }

        public ICanStartOrExit Stop()
        {
            _states.Add("Stop");
            return this;
        }

        ICanLockOrEnter ICanUnlock.Unlock()
        {
            _states.Add("Unlock");
            return this;
        }
    }
}
