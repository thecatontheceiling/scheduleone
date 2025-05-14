using ScheduleOne.DevUtilities;
using ScheduleOne.EntityFramework;
using ScheduleOne.ItemFramework;
using ScheduleOne.UI;
using UnityEngine;

namespace ScheduleOne.Building;

public class BuildStart_Surface : BuildStart_Base
{
	public override void StartBuilding(ItemInstance itemInstance)
	{
		SurfaceItem surfaceItem = CreateGhostModel(itemInstance.Definition as BuildableItemDefinition);
		if (!(surfaceItem == null))
		{
			Singleton<HUD>.Instance.SetCrosshairVisible(vis: false);
			Singleton<InputPromptsCanvas>.Instance.LoadModule("building");
			base.gameObject.GetComponent<BuildUpdate_Surface>().GhostModel = surfaceItem.gameObject;
			base.gameObject.GetComponent<BuildUpdate_Surface>().BuildableItemClass = surfaceItem;
			base.gameObject.GetComponent<BuildUpdate_Surface>().ItemInstance = itemInstance;
		}
	}

	protected virtual SurfaceItem CreateGhostModel(BuildableItemDefinition itemDefinition)
	{
		itemDefinition.BuiltItem.isGhost = true;
		GameObject gameObject = Object.Instantiate(itemDefinition.BuiltItem.gameObject, base.transform);
		itemDefinition.BuiltItem.isGhost = false;
		SurfaceItem component = gameObject.GetComponent<SurfaceItem>();
		if (component == null)
		{
			Console.LogWarning("CreateGhostModel: asset path is not a SurfaceItem!");
			return null;
		}
		component.enabled = false;
		component.isGhost = true;
		Singleton<BuildManager>.Instance.DisableColliders(gameObject);
		Singleton<BuildManager>.Instance.DisableNavigation(gameObject);
		Singleton<BuildManager>.Instance.DisableNetworking(gameObject);
		Singleton<BuildManager>.Instance.DisableCanvases(gameObject);
		ActivateDuringBuild[] componentsInChildren = gameObject.GetComponentsInChildren<ActivateDuringBuild>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].gameObject.SetActive(value: true);
		}
		return component;
	}
}
