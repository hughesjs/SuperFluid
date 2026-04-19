using SuperFluid;

namespace SuperFluid.Examples.Attributes;

/// <summary>A car actor demonstrating compile-time-enforced fluent call sequences.</summary>
// `Grammar` suffix is stripped from the interface name — the generator emits `ICarActor`.
// This interface is a declaration vehicle for the source generator; nothing implements it.
[FluidApiGrammar]
[StateName("ICarDriving", "Stop", "Build")]
internal interface ICarActorGrammar
{
	/// <summary>Constructs a new car in its locked state.</summary>
	[Initial, TransitionsTo(nameof(Unlock))]
	void Initialize();

	/// <summary>Unlocks the car, allowing the driver to enter or lock it again.</summary>
	[TransitionsTo(nameof(Lock), nameof(Enter))]
	void Unlock();

	/// <summary>Locks the car, returning it to its initial sealed state.</summary>
	[TransitionsTo(nameof(Unlock))]
	void Lock();

	/// <summary>Enters the car, readying it for starting or exit.</summary>
	[TransitionsTo(nameof(Start), nameof(Exit))]
	void Enter();

	/// <summary>Exits the car, leaving it unlocked but empty.</summary>
	[TransitionsTo(nameof(Lock), nameof(Enter))]
	void Exit();

	/// <summary>Starts the engine. T encodes the ignition-key type; speed is required and direction/hotwire have defaults.</summary>
	[TransitionsTo(nameof(Stop), nameof(Build))]
	void Start<T>(int speed, string direction = "Forward", bool hotwire = false) where T : notnull;

	/// <summary>Stops the engine, returning to the entered-but-stationary state.</summary>
	[TransitionsTo(nameof(Start), nameof(Exit))]
	void Stop();

	/// <summary>Builds a description of the configured car. Terminal method with an explicit return type.</summary>
	[TransitionsTo, ReturnType(typeof(string))]
	void Build(string color);
}
