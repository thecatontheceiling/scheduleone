using ScheduleOne.EntityFramework;
using ScheduleOne.Persistence.Datas;

namespace ScheduleOne.Persistence.Loaders;

public class ToggleableSurfaceItemLoader : SurfaceItemLoader
{
	public override string ItemType => typeof(ToggleableSurfaceItemData).Name;

	public override void Load(string mainPath)
	{
		SurfaceItem surfaceItem = LoadAndCreate(mainPath);
		if (surfaceItem == null)
		{
			Console.LogWarning("Failed to load grid item");
			return;
		}
		ToggleableSurfaceItemData data = GetData<ToggleableSurfaceItemData>(mainPath);
		if (data == null)
		{
			Console.LogWarning("Failed to load ToggleableSurfaceItemData");
			return;
		}
		ToggleableSurfaceItem toggleableSurfaceItem = surfaceItem as ToggleableSurfaceItem;
		if (toggleableSurfaceItem != null && data.IsOn)
		{
			toggleableSurfaceItem.TurnOn();
		}
	}
}
