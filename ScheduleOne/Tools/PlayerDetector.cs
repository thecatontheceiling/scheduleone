using System;
using System.Collections.Generic;
using ScheduleOne.DevUtilities;
using ScheduleOne.GameTime;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Vehicles;
using UnityEngine;
using UnityEngine.Events;

namespace ScheduleOne.Tools;

[RequireComponent(typeof(Rigidbody))]
public class PlayerDetector : MonoBehaviour
{
	public const float ACTIVATION_DISTANCE_SQ = 400f;

	public bool DetectPlayerInVehicle;

	public UnityEvent<Player> onPlayerEnter;

	public UnityEvent<Player> onPlayerExit;

	public UnityEvent onLocalPlayerEnter;

	public UnityEvent onLocalPlayerExit;

	public List<Player> DetectedPlayers = new List<Player>();

	private bool ignoreExit;

	private bool collidersEnabled = true;

	private Collider[] detectionColliders;

	public bool IgnoreNewDetections { get; protected set; }

	private void Awake()
	{
		Rigidbody rigidbody = GetComponent<Rigidbody>();
		if (rigidbody == null)
		{
			rigidbody = base.gameObject.AddComponent<Rigidbody>();
		}
		rigidbody.isKinematic = true;
		detectionColliders = GetComponentsInChildren<Collider>();
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

	private void MinPass()
	{
		bool flag = false;
		for (int i = 0; i < Player.PlayerList.Count; i++)
		{
			if (Vector3.SqrMagnitude(Player.PlayerList[i].Avatar.CenterPoint - base.transform.position) < 400f)
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

	private void OnTriggerEnter(Collider other)
	{
		if (IgnoreNewDetections)
		{
			return;
		}
		Player componentInParent = other.GetComponentInParent<Player>();
		if (componentInParent != null && !DetectedPlayers.Contains(componentInParent) && other == componentInParent.CapCol)
		{
			DetectedPlayers.Add(componentInParent);
			if (onPlayerEnter != null)
			{
				onPlayerEnter.Invoke(componentInParent);
			}
			if (componentInParent.IsOwner && onLocalPlayerEnter != null)
			{
				onLocalPlayerEnter.Invoke();
			}
		}
		if (!DetectPlayerInVehicle)
		{
			return;
		}
		LandVehicle componentInParent2 = other.GetComponentInParent<LandVehicle>();
		if (!(componentInParent2 != null))
		{
			return;
		}
		foreach (Player occupantPlayer in componentInParent2.OccupantPlayers)
		{
			if (occupantPlayer != null && !DetectedPlayers.Contains(occupantPlayer))
			{
				DetectedPlayers.Add(occupantPlayer);
				if (onPlayerEnter != null)
				{
					onPlayerEnter.Invoke(occupantPlayer);
				}
				if (occupantPlayer.IsOwner && onLocalPlayerEnter != null)
				{
					onLocalPlayerEnter.Invoke();
				}
			}
		}
	}

	private void FixedUpdate()
	{
		for (int i = 0; i < DetectedPlayers.Count; i++)
		{
			if (DetectedPlayers[i].CurrentVehicle != null)
			{
				OnTriggerExit(DetectedPlayers[i].CapCol);
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (ignoreExit)
		{
			return;
		}
		Player componentInParent = other.GetComponentInParent<Player>();
		if (componentInParent != null && DetectedPlayers.Contains(componentInParent) && other == componentInParent.CapCol)
		{
			DetectedPlayers.Remove(componentInParent);
			if (onPlayerExit != null)
			{
				onPlayerExit.Invoke(componentInParent);
			}
			if (componentInParent.IsOwner && onLocalPlayerExit != null)
			{
				onLocalPlayerExit.Invoke();
			}
		}
		if (!DetectPlayerInVehicle)
		{
			return;
		}
		LandVehicle componentInParent2 = other.GetComponentInParent<LandVehicle>();
		if (!(componentInParent2 != null))
		{
			return;
		}
		foreach (Player occupantPlayer in componentInParent2.OccupantPlayers)
		{
			if (occupantPlayer != null && DetectedPlayers.Contains(occupantPlayer))
			{
				DetectedPlayers.Remove(occupantPlayer);
				if (onPlayerExit != null)
				{
					onPlayerExit.Invoke(occupantPlayer);
				}
				if (occupantPlayer.IsOwner && onLocalPlayerExit != null)
				{
					onLocalPlayerExit.Invoke();
				}
			}
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
}
