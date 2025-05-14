using System;
using ScheduleOne.Building;
using ScheduleOne.DevUtilities;
using ScheduleOne.EntityFramework;
using ScheduleOne.ItemFramework;
using ScheduleOne.Persistence.Datas;
using UnityEngine;

namespace ScheduleOne.Persistence.Loaders;

public class SurfaceItemLoader : BuildableItemLoader
{
	public override string ItemType => typeof(SurfaceItemData).Name;

	public override void Load(string mainPath)
	{
		LoadAndCreate(mainPath);
	}

	protected SurfaceItem LoadAndCreate(string mainPath)
	{
		if (TryLoadFile(mainPath, "Data", out var contents))
		{
			SurfaceItemData surfaceItemData = null;
			try
			{
				surfaceItemData = JsonUtility.FromJson<SurfaceItemData>(contents);
			}
			catch (Exception ex)
			{
				Console.LogError(GetType()?.ToString() + " error reading data: " + ex);
			}
			if (surfaceItemData != null)
			{
				ItemInstance itemInstance = ItemDeserializer.LoadItem(surfaceItemData.ItemString);
				if (itemInstance == null)
				{
					return null;
				}
				Surface surface = GUIDManager.GetObject<Surface>(new Guid(surfaceItemData.ParentSurfaceGUID));
				if (surface == null)
				{
					Console.LogWarning("Failed to find parent surface for " + surfaceItemData.ParentSurfaceGUID);
					return null;
				}
				return Singleton<BuildManager>.Instance.CreateSurfaceItem(itemInstance, surface, surfaceItemData.RelativePosition, surfaceItemData.RelativeRotation, surfaceItemData.GUID);
			}
		}
		return null;
	}
}
