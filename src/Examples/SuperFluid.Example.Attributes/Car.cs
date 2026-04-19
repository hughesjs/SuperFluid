namespace SuperFluid.Examples.Attributes;

// Concrete implementation of the generated `ICarActor` compound interface.
// Every state-interface method returns `this`; the state interfaces themselves constrain
// which methods are callable at each point in the chain.
public class Car : ICarActor
{
	private int _speed;
	private string _direction = "Forward";
	private bool _hotwire;

	public static ICanUnlock Initialize() => new Car();

	public ICanEnterOrLock Unlock() => this;
	public ICanUnlock Lock() => this;
	public ICanExitOrStart Enter() => this;
	public ICanEnterOrLock Exit() => this;

	public ICarDriving Start<T>(int speed, string direction = "Forward", bool hotwire = false) where T : notnull
	{
		_speed = speed;
		_direction = direction;
		_hotwire = hotwire;
		return this;
	}

	public ICanExitOrStart Stop() => this;

	public string Build(string color)
	{
		string hotwireSuffix = _hotwire ? " (hotwired)" : "";
		return $"Built a {color} car going {_direction} at {_speed} mph{hotwireSuffix}";
	}
}
