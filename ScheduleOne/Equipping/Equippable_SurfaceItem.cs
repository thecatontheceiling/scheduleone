using ScheduleOne.Building;
using ScheduleOne.DevUtilities;

namespace ScheduleOne.Equipping;

public class Equippable_SurfaceItem : Equippable_StorableItem
{
	protected bool isBuilding;

	protected override void Update()
	{
		CheckLookingAtStorageObject();
		if (lookingAtStorageObject && isBuilding)
		{
			isBuilding = false;
			if (Singleton<BuildManager>.Instance.currentBuildHandler.GetComponent<BuildUpdate_Surface>() != null)
			{
				rotation = Singleton<BuildManager>.Instance.currentBuildHandler.GetComponent<BuildUpdate_Surface>().CurrentRotation;
			}
		}
		base.Update();
		if (!lookingAtStorageObject && !isBuilding)
		{
			isBuilding = true;
			Singleton<BuildManager>.Instance.StartBuilding(itemInstance);
			if (Singleton<BuildManager>.Instance.currentBuildHandler.GetComponent<BuildUpdate_Surface>() != null)
			{
				Singleton<BuildManager>.Instance.currentBuildHandler.GetComponent<BuildUpdate_Surface>().CurrentRotation = rotation;
			}
		}
	}

	public override void Unequip()
	{
		if (isBuilding)
		{
			Singleton<BuildManager>.Instance.StopBuilding();
		}
		base.Unequip();
	}
}
