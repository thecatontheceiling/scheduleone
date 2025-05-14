using ScheduleOne.DevUtilities;
using UnityEngine;

namespace ScheduleOne.Map;

public class AutoshopAccessZone : NPCPresenceAccessZone
{
	public Animation RollerDoorAnim;

	public VehicleDetector VehicleDetection;

	private bool rollerDoorOpen = true;

	public override void SetIsOpen(bool open)
	{
		base.SetIsOpen(open);
		if (rollerDoorOpen != open)
		{
			rollerDoorOpen = open;
			RollerDoorAnim.Play(rollerDoorOpen ? "Roller door open" : "Roller door close");
		}
	}

	protected override void MinPass()
	{
		if (!(TargetNPC == null))
		{
			SetIsOpen(DetectionZone.bounds.Contains(TargetNPC.Avatar.CenterPoint) || VehicleDetection.closestVehicle != null);
		}
	}
}
