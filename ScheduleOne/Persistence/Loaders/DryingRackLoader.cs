using System.IO;
using ScheduleOne.DevUtilities;
using ScheduleOne.EntityFramework;
using ScheduleOne.ItemFramework;
using ScheduleOne.Management;
using ScheduleOne.ObjectScripts;
using ScheduleOne.Persistence.Datas;
using UnityEngine;

namespace ScheduleOne.Persistence.Loaders;

public class DryingRackLoader : GridItemLoader
{
	public override string ItemType => typeof(DryingRackData).Name;

	public override void Load(string mainPath)
	{
		GridItem gridItem = LoadAndCreate(mainPath);
		if (gridItem == null)
		{
			Console.LogWarning("Failed to load grid item");
			return;
		}
		DryingRack station = gridItem as DryingRack;
		if (station == null)
		{
			Console.LogWarning("Failed to cast grid item to DryingRack");
			return;
		}
		DryingRackData data = GetData<DryingRackData>(mainPath);
		if (data == null)
		{
			Console.LogWarning("Failed to load DryingRack data");
			return;
		}
		ItemInstance instance = ItemDeserializer.LoadItem(data.Input.Items[0]);
		station.InputSlot.SetStoredItem(instance);
		ItemInstance instance2 = ItemDeserializer.LoadItem(data.Output.Items[0]);
		station.OutputSlot.SetStoredItem(instance2);
		for (int i = 0; i < data.DryingOperations.Length; i++)
		{
			if (data.DryingOperations[i] != null && data.DryingOperations[i].Quantity > 0 && !string.IsNullOrEmpty(data.DryingOperations[i].ItemID))
			{
				station.DryingOperations.Add(data.DryingOperations[i]);
			}
		}
		station.RefreshHangingVisuals();
		DryingRackConfigurationData configData;
		if (File.Exists(Path.Combine(mainPath, "Configuration.json")) && TryLoadFile(mainPath, "Configuration", out var contents))
		{
			configData = JsonUtility.FromJson<DryingRackConfigurationData>(contents);
			if (configData != null)
			{
				Singleton<LoadManager>.Instance.onLoadComplete.AddListener(LoadConfiguration);
			}
		}
		void LoadConfiguration()
		{
			Singleton<LoadManager>.Instance.onLoadComplete.RemoveListener(LoadConfiguration);
			(station.Configuration as DryingRackConfiguration).TargetQuality.Load(configData.TargetQuality);
			(station.Configuration as DryingRackConfiguration).Destination.Load(configData.Destination);
		}
	}
}
