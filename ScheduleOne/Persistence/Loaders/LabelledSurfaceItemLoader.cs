using ScheduleOne.EntityFramework;
using ScheduleOne.Persistence.Datas;

namespace ScheduleOne.Persistence.Loaders;

public class LabelledSurfaceItemLoader : SurfaceItemLoader
{
	public override string ItemType => typeof(LabelledSurfaceItemData).Name;

	public override void Load(string mainPath)
	{
		SurfaceItem surfaceItem = LoadAndCreate(mainPath);
		if (surfaceItem == null)
		{
			Console.LogWarning("Failed to load surface item");
			return;
		}
		LabelledSurfaceItemData data = GetData<LabelledSurfaceItemData>(mainPath);
		if (data == null)
		{
			Console.LogWarning("Failed to load LabelledSurfaceItemData");
			return;
		}
		LabelledSurfaceItem labelledSurfaceItem = surfaceItem as LabelledSurfaceItem;
		if (labelledSurfaceItem != null)
		{
			labelledSurfaceItem.SetMessage(null, data.Message);
		}
	}
}
