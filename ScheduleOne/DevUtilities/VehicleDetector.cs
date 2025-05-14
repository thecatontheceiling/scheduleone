using System;
using System.Collections.Generic;
using System.Linq;
using ScheduleOne.GameTime;
using ScheduleOne.Vehicles;
using UnityEngine;

namespace ScheduleOne.DevUtilities;

[RequireComponent(typeof(Rigidbody))]
public class VehicleDetector : MonoBehaviour
{
	public const float ACTIVATION_DISTANCE_SQ = 400f;

	public List<LandVehicle> vehicles = new List<LandVehicle>();

	public LandVehicle closestVehicle;

	private bool ignoreExit;

	private Collider[] detectionColliders;

	private bool collidersEnabled = true;

	public bool IgnoreNewDetections { get; protected set; }

	private void Awake()
	{
		Rigidbody rigidbody = GetComponent<Rigidbody>();
		if (rigidbody == null)
		{
			rigidbody = base.gameObject.AddComponent<Rigidbody>();
		}
		detectionColliders = GetComponentsInChildren<Collider>();
		rigidbody.isKinematic = true;
	}

	private void Start()
	{
		TimeManager instance = NetworkSingleton<TimeManager>.Instance;
		instance.onTick = (Action)Delegate.Combine(instance.onTick, new Action(MinPass));
	}

	private void OnDestroy()
	{
		if (NetworkSingleton<TimeManager>.InstanceExists)
		{
			TimeManager instance = NetworkSingleton<TimeManager>.Instance;
			instance.onTick = (Action)Delegate.Remove(instance.onTick, new Action(MinPass));
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!IgnoreNewDetections)
		{
			LandVehicle componentInParent = other.GetComponentInParent<LandVehicle>();
			if (componentInParent != null && other == componentInParent.boundingBox && !vehicles.Contains(componentInParent))
			{
				vehicles.Add(componentInParent);
				SortVehicles();
			}
		}
	}

	private void MinPass()
	{
		bool flag = false;
		for (int i = 0; i < NetworkSingleton<VehicleManager>.Instance.AllVehicles.Count; i++)
		{
			if (Vector3.SqrMagnitude(NetworkSingleton<VehicleManager>.Instance.AllVehicles[i].transform.position - base.transform.position) < 400f)
			{
				flag = true;
				break;
			}
		}
		if (flag != collidersEnabled)
		{
			collidersEnabled = flag;
			for (int j = 0; j < detectionColliders.Length; j++)
			{
				detectionColliders[j].enabled = collidersEnabled;
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (!ignoreExit)
		{
			LandVehicle componentInParent = other.GetComponentInParent<LandVehicle>();
			if (componentInParent != null && other == componentInParent.boundingBox && vehicles.Contains(componentInParent))
			{
				vehicles.Remove(componentInParent);
				SortVehicles();
			}
		}
	}

	private void SortVehicles()
	{
		if (vehicles.Count > 1)
		{
			vehicles.OrderBy((LandVehicle x) => Vector3.Distance(base.transform.position, x.transform.position));
		}
		if (vehicles.Count > 0)
		{
			closestVehicle = vehicles[0];
		}
		else
		{
			closestVehicle = null;
		}
	}

	public void SetIgnoreNewCollisions(bool ignore)
	{
		IgnoreNewDetections = ignore;
		if (ignore)
		{
			return;
		}
		ignoreExit = true;
		Collider[] componentsInChildren = GetComponentsInChildren<Collider>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (componentsInChildren[i].isTrigger)
			{
				componentsInChildren[i].enabled = false;
				componentsInChildren[i].enabled = true;
			}
		}
		ignoreExit = false;
	}

	public bool AreAnyVehiclesOccupied()
	{
		for (int i = 0; i < vehicles.Count; i++)
		{
			if (vehicles[i].isOccupied)
			{
				return true;
			}
		}
		return false;
	}

	public void Clear()
	{
		vehicles.Clear();
		SortVehicles();
	}
}
