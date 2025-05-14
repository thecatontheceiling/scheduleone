using System.Collections.Generic;
using ScheduleOne.Map;
using ScheduleOne.Storage;
using UnityEngine;

namespace ScheduleOne.Economy;

public class SupplierLocation : MonoBehaviour
{
	public static List<SupplierLocation> AllLocations = new List<SupplierLocation>();

	[Header("Settings")]
	public string LocationName;

	public string LocationDescription;

	[Header("References")]
	public Transform GenericContainer;

	public Transform SupplierStandPoint;

	public WorldStorageEntity[] DeliveryBays;

	public POI PoI;

	private SupplierLocationConfiguration[] configs;

	public bool IsOccupied => ActiveSupplier != null;

	public Supplier ActiveSupplier { get; private set; }

	public void Awake()
	{
		AllLocations.Add(this);
		GenericContainer.gameObject.SetActive(value: false);
		WorldStorageEntity[] deliveryBays = DeliveryBays;
		for (int i = 0; i < deliveryBays.Length; i++)
		{
			deliveryBays[i].transform.Find("Container").gameObject.SetActive(value: false);
		}
		configs = GetComponentsInChildren<SupplierLocationConfiguration>();
		SupplierLocationConfiguration[] array = configs;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Deactivate();
		}
	}

	private void OnDestroy()
	{
		AllLocations.Remove(this);
	}

	public void SetActiveSupplier(Supplier supplier)
	{
		ActiveSupplier = supplier;
		GenericContainer.gameObject.SetActive(ActiveSupplier != null);
		WorldStorageEntity[] deliveryBays = DeliveryBays;
		for (int i = 0; i < deliveryBays.Length; i++)
		{
			deliveryBays[i].transform.Find("Container").gameObject.SetActive(ActiveSupplier != null);
		}
		if (supplier != null)
		{
			PoI.SetMainText("Supplier Meeting\n(" + supplier.fullName + ")");
		}
		SupplierLocationConfiguration[] array = configs;
		foreach (SupplierLocationConfiguration supplierLocationConfiguration in array)
		{
			if (ActiveSupplier != null && supplierLocationConfiguration.SupplierID == ActiveSupplier.ID)
			{
				supplierLocationConfiguration.Activate();
			}
			else
			{
				supplierLocationConfiguration.Deactivate();
			}
		}
	}
}
