using System;
using System.Collections.Generic;
using System.IO;
using ScheduleOne.DevUtilities;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.Vehicles;
using UnityEngine;

namespace ScheduleOne.Persistence.Loaders;

public class VehiclesLoader : Loader
{
	public override void Load(string mainPath)
	{
		if (TryLoadFile(mainPath, out var contents))
		{
			VehicleCollectionData vehicleCollectionData = null;
			try
			{
				vehicleCollectionData = JsonUtility.FromJson<VehicleCollectionData>(contents);
			}
			catch (Exception ex)
			{
				Debug.LogError("Error loading data: " + ex.Message);
			}
			if (vehicleCollectionData != null && vehicleCollectionData.Vehicles != null)
			{
				VehicleData[] vehicles = vehicleCollectionData.Vehicles;
				foreach (VehicleData data in vehicles)
				{
					NetworkSingleton<VehicleManager>.Instance.SpawnAndLoadVehicle(data, string.Empty, playerOwned: true);
				}
			}
		}
		else if (Directory.Exists(mainPath))
		{
			Console.Log("Loading legacy vehicles at: " + mainPath);
			List<DirectoryInfo> directories = GetDirectories(mainPath);
			VehicleLoader loader = new VehicleLoader();
			for (int j = 0; j < directories.Count; j++)
			{
				new LoadRequest(directories[j].FullName, loader);
			}
		}
	}
}
