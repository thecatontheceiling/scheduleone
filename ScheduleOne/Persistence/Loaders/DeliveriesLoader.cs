using System;
using System.Collections.Generic;
using System.IO;
using ScheduleOne.Delivery;
using ScheduleOne.DevUtilities;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.Vehicles;
using UnityEngine;

namespace ScheduleOne.Persistence.Loaders;

public class DeliveriesLoader : Loader
{
	public override void Load(string mainPath)
	{
		bool flag = false;
		if (TryLoadFile(Path.Combine(mainPath, "Deliveries"), out var contents) || TryLoadFile(mainPath, out contents))
		{
			DeliveriesData deliveriesData = null;
			try
			{
				deliveriesData = JsonUtility.FromJson<DeliveriesData>(contents);
			}
			catch (Exception ex)
			{
				Debug.LogError("Error loading data: " + ex.Message);
			}
			if (deliveriesData != null && deliveriesData.ActiveDeliveries != null)
			{
				DeliveryInstance[] activeDeliveries = deliveriesData.ActiveDeliveries;
				foreach (DeliveryInstance delivery in activeDeliveries)
				{
					NetworkSingleton<DeliveryManager>.Instance.SendDelivery(delivery);
				}
				if (deliveriesData.DeliveryVehicles != null)
				{
					flag = true;
					VehicleData[] deliveryVehicles = deliveriesData.DeliveryVehicles;
					foreach (VehicleData data in deliveryVehicles)
					{
						NetworkSingleton<VehicleManager>.Instance.LoadVehicle(data, mainPath);
					}
				}
			}
		}
		if (!flag && Directory.Exists(mainPath))
		{
			Console.Log("Loading legacy delivery vehicles at: " + mainPath);
			string parentPath = Path.Combine(mainPath, "DeliveryVehicles");
			List<DirectoryInfo> directories = GetDirectories(parentPath);
			for (int j = 0; j < directories.Count; j++)
			{
				LoadVehicle(directories[j].FullName);
			}
		}
	}

	public void LoadVehicle(string vehiclePath)
	{
		Console.Log("Loading delivery vehicle: " + vehiclePath);
		if (TryLoadFile(vehiclePath, "Vehicle", out var contents))
		{
			VehicleData vehicleData = null;
			try
			{
				vehicleData = JsonUtility.FromJson<VehicleData>(contents);
			}
			catch (Exception ex)
			{
				Console.LogError(GetType()?.ToString() + " error reading data: " + ex);
			}
			Console.Log("Data: " + vehicleData);
			if (vehicleData != null)
			{
				NetworkSingleton<VehicleManager>.Instance.LoadVehicle(vehicleData, vehiclePath);
			}
		}
	}
}
