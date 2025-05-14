using System;
using ScheduleOne.Map;
using ScheduleOne.Storage;
using ScheduleOne.Vehicles;
using UnityEngine;

namespace ScheduleOne.Delivery;

[RequireComponent(typeof(LandVehicle))]
public class DeliveryVehicle : MonoBehaviour
{
	public string GUID = string.Empty;

	public LandVehicle Vehicle { get; private set; }

	public DeliveryInstance ActiveDelivery { get; private set; }

	private void Awake()
	{
		Vehicle = GetComponent<LandVehicle>();
		Vehicle.SetGUID(new Guid(GUID));
	}

	public void Activate(DeliveryInstance instance)
	{
		Console.Log("Activating delivery vehicle for delivery instance " + instance.DeliveryID);
		ActiveDelivery = instance;
		ParkingLot parking = instance.LoadingDock.Parking;
		instance.LoadingDock.SetStaticOccupant(Vehicle);
		Vehicle.Park(null, new ParkData
		{
			lotGUID = parking.GUID,
			spotIndex = 0,
			alignment = parking.ParkingSpots[0].Alignment
		}, network: false);
		Vehicle.SetVisible(vis: true);
		Vehicle.Storage.AccessSettings = StorageEntity.EAccessSettings.Full;
		Vehicle.GetComponentInChildren<StorageDoorAnimation>().OverrideState(open: true);
	}

	public void Deactivate()
	{
		if (Vehicle != null)
		{
			Vehicle.ExitPark(moveToExitPoint: false);
			Vehicle.SetIsStatic(stat: true);
			Vehicle.SetVisible(vis: false);
			Vehicle.SetTransform(new Vector3(0f, -100f, 0f), Quaternion.identity);
		}
		if (ActiveDelivery != null)
		{
			ActiveDelivery.LoadingDock.SetStaticOccupant(null);
			ActiveDelivery.LoadingDock.VehicleDetector.Clear();
		}
	}
}
