using ScheduleOne.EntityFramework;
using ScheduleOne.ItemFramework;
using ScheduleOne.ObjectScripts;
using ScheduleOne.Persistence.Datas;

namespace ScheduleOne.Persistence.Loaders;

public class StorageSurfaceItemLoader : SurfaceItemLoader
{
	public override string ItemType => typeof(StorageSurfaceItemData).Name;

	public override void Load(string mainPath)
	{
		SurfaceItem surfaceItem = LoadAndCreate(mainPath);
		if (surfaceItem == null)
		{
			Console.LogWarning("Failed to load surface item");
			return;
		}
		SurfaceStorageEntity surfaceStorageEntity = surfaceItem as SurfaceStorageEntity;
		if (surfaceStorageEntity == null)
		{
			Console.LogWarning("Failed to cast surface item to storage entity");
			return;
		}
		StorageSurfaceItemData data = GetData<StorageSurfaceItemData>(mainPath);
		if (data == null)
		{
			Console.LogWarning("Failed to load storage surface item data");
			return;
		}
		for (int i = 0; i < data.Contents.Items.Length; i++)
		{
			ItemInstance instance = ItemDeserializer.LoadItem(data.Contents.Items[i]);
			if (surfaceStorageEntity.StorageEntity.ItemSlots.Count > i)
			{
				surfaceStorageEntity.StorageEntity.ItemSlots[i].SetStoredItem(instance);
			}
		}
	}
}
