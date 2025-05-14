using System;
using System.Collections.Generic;
using EPOOutline;
using EasyButtons;
using FluffyUnderware.DevTools.Extensions;
using ScheduleOne.DevUtilities;
using ScheduleOne.ItemFramework;
using ScheduleOne.Management;
using ScheduleOne.Map;
using ScheduleOne.Property;
using ScheduleOne.Vehicles;
using UnityEngine;

namespace ScheduleOne.Delivery;

public class LoadingDock : MonoBehaviour, IGUIDRegisterable, ITransitEntity
{
	[SerializeField]
	protected string BakedGUID = string.Empty;

	public ScheduleOne.Property.Property ParentProperty;

	public VehicleDetector VehicleDetector;

	public ParkingLot Parking;

	public Transform uiPoint;

	public Transform[] accessPoints;

	public GameObject[] OutlineRenderers;

	private Outlinable OutlineEffect;

	public LandVehicle DynamicOccupant { get; private set; }

	public LandVehicle StaticOccupant { get; private set; }

	public bool IsInUse
	{
		get
		{
			if (!(DynamicOccupant != null))
			{
				return StaticOccupant != null;
			}
			return true;
		}
	}

	public Guid GUID { get; protected set; }

	public string Name => "Loading Dock " + (ParentProperty.LoadingDocks.IndexOf(this) + 1);

	public List<ItemSlot> InputSlots { get; set; } = new List<ItemSlot>();

	public List<ItemSlot> OutputSlots { get; set; } = new List<ItemSlot>();

	public Transform LinkOrigin => uiPoint;

	public Transform[] AccessPoints => accessPoints;

	public bool Selectable { get; } = true;

	public bool IsAcceptingItems { get; set; }

	public bool IsDestroyed { get; set; }

	[Button]
	public void RegenerateGUID()
	{
		BakedGUID = Guid.NewGuid().ToString();
	}

	private void Awake()
	{
		GUID = new Guid(BakedGUID);
		GUIDManager.RegisterObject(this);
	}

	private void Start()
	{
		InvokeRepeating("RefreshOccupant", UnityEngine.Random.Range(0f, 1f), 1f);
	}

	public void SetGUID(Guid guid)
	{
		GUID = guid;
		GUIDManager.RegisterObject(this);
	}

	private void RefreshOccupant()
	{
		LandVehicle closestVehicle = VehicleDetector.closestVehicle;
		if (closestVehicle != null && closestVehicle.speed_Kmh < 2f)
		{
			SetOccupant(VehicleDetector.closestVehicle);
		}
		else
		{
			SetOccupant(null);
		}
		if (StaticOccupant != null && !StaticOccupant.IsVisible)
		{
			SetStaticOccupant(null);
		}
		if (DynamicOccupant != null)
		{
			Vector3 position = DynamicOccupant.transform.position - DynamicOccupant.transform.forward * (DynamicOccupant.boundingBoxDimensions.z / 2f + 0.6f);
			accessPoints[0].transform.position = position;
			accessPoints[0].transform.rotation = Quaternion.LookRotation(DynamicOccupant.transform.forward, Vector3.up);
			accessPoints[0].transform.localPosition = new Vector3(accessPoints[0].transform.localPosition.x, 0f, accessPoints[0].transform.localPosition.z);
		}
	}

	private void SetOccupant(LandVehicle occupant)
	{
		if (!(occupant == DynamicOccupant))
		{
			Console.Log("Loading dock " + base.name + " is " + ((occupant == null) ? "empty" : "occupied") + ".");
			DynamicOccupant = occupant;
			InputSlots.Clear();
			OutputSlots.Clear();
			if (DynamicOccupant != null)
			{
				OutputSlots.AddRange(DynamicOccupant.Storage.ItemSlots);
			}
		}
	}

	public void SetStaticOccupant(LandVehicle vehicle)
	{
		StaticOccupant = vehicle;
	}

	public virtual void ShowOutline(Color color)
	{
		if (OutlineEffect == null)
		{
			OutlineEffect = base.gameObject.AddComponent<Outlinable>();
			OutlineEffect.OutlineParameters.BlurShift = 0f;
			OutlineEffect.OutlineParameters.DilateShift = 0.5f;
			OutlineEffect.OutlineParameters.FillPass.Shader = Resources.Load<Shader>("Easy performant outline/Shaders/Fills/ColorFill");
			GameObject[] outlineRenderers = OutlineRenderers;
			foreach (GameObject gameObject in outlineRenderers)
			{
				MeshRenderer[] array = new MeshRenderer[0];
				array = new MeshRenderer[1] { gameObject.GetComponent<MeshRenderer>() };
				for (int j = 0; j < array.Length; j++)
				{
					OutlineTarget target = new OutlineTarget(array[j]);
					OutlineEffect.TryAddTarget(target);
				}
			}
		}
		OutlineEffect.OutlineParameters.Color = color;
		Color32 color2 = color;
		color2.a = 9;
		OutlineEffect.OutlineParameters.FillPass.SetColor("_PublicColor", color2);
		OutlineEffect.enabled = true;
	}

	public virtual void HideOutline()
	{
		if (OutlineEffect != null)
		{
			OutlineEffect.enabled = false;
		}
	}
}
