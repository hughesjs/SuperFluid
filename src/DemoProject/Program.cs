using SuperFluid.Tests.Cars;

string builtString = Test.Initialize()
							.Unlock()
							.Enter()
							.Start()
							.Stop()
							.Exit()
							.Lock()
							.Unlock()
							.Enter()
							.Start()
							.Build();

Console.WriteLine(builtString);

public class Test : ICarActor
{

	private readonly List<string> _calls = new();

	public ICanUnlock Lock()
	{
		_calls.Add("Lock!");
		return this;
	}

	public ICanStartOrExit Enter()
	{
		_calls.Add("Enter!");
		return this;
	}


	public ICanLockOrEnter Unlock()
	{
		_calls.Add("Unlock!");
		return this;
	}

	public ICanStopOrBuild Start()
	{
		_calls.Add("Start!");
		return this;
	}

	public ICanLockOrEnter Exit()
	{
		_calls.Add("Exit!");
		return this;
	}

	public ICanStartOrExit Stop()
	{
		_calls.Add("Stop!");
		return this;
	}

	public string Build() => string.Join('\n', _calls);

	public static ICanUnlock Initialize() => new Test();
}
