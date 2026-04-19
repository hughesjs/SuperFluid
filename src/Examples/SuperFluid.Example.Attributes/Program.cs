using SuperFluid.Examples.Attributes;

// A valid call sequence, compile-time-enforced by the generated state interfaces.
// At each `.` the IDE shows only methods valid from the current state.
string description = Car.Initialize()   // → ICanUnlock
	.Unlock()                           // → ICanEnterOrLock
	.Enter()                            // → ICanExitOrStart
	.Start<int>(speed: 30)              // → ICarDriving (renamed from ICanBuildOrStop via [StateName])
	.Build("red");                      // → string (terminal)

Console.WriteLine(description);

// The following lines would each be a compile error — uncomment one at a time to see the
// compiler enforce the state machine:
//
//   Car.Initialize().Lock();
//       // 'ICanUnlock' does not contain a definition for 'Lock'.
//
//   Car.Initialize().Unlock().Build("red");
//       // 'ICanEnterOrLock' does not contain a definition for 'Build'.
//
//   Car.Initialize().Unlock().Enter().Start<int>(30).Stop().Build("red");
//       // After Stop() you're back in ICanExitOrStart — Build is unreachable without
//       // Start()-ing again first.
