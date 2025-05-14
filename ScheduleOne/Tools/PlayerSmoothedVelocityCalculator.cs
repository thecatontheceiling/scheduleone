using ScheduleOne.PlayerScripts;
using ScheduleOne.Vehicles;

namespace ScheduleOne.Tools;

public class PlayerSmoothedVelocityCalculator : SmoothedVelocityCalculator
{
	public Player Player;

	protected override void FixedUpdate()
	{
		base.FixedUpdate();
		if (Player.CurrentVehicle != null)
		{
			Velocity = Player.CurrentVehicle.GetComponent<LandVehicle>().VelocityCalculator.Velocity;
		}
	}
}
