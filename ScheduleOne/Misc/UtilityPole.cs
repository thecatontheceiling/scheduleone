using System;
using System.Collections.Generic;
using EasyButtons;
using ScheduleOne.DevUtilities;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Property.Utilities.Power;
using UnityEngine;

namespace ScheduleOne.Misc;

public class UtilityPole : MonoBehaviour
{
	public const float CABLE_CULL_DISTANCE = 100f;

	public const float CABLE_CULL_DISTANCE_SQR = 10000f;

	public UtilityPole previousPole;

	public UtilityPole nextPole;

	public bool Connection1Enabled = true;

	public bool Connection2Enabled = true;

	public float LengthFactor = 1.002f;

	[Header("References")]
	public Transform cable1Connection;

	public Transform cable2Connection;

	public List<Transform> cable1Segments = new List<Transform>();

	public List<Transform> cable2Segments = new List<Transform>();

	public Transform Cable1Container;

	public Transform Cable2Container;

	private Vector3 cableStart = Vector3.zero;

	private Vector3 cableEnd = Vector3.zero;

	private Vector3 cableMid = Vector3.zero;

	private void Awake()
	{
		if (Cable1Container.gameObject.activeSelf)
		{
			cableStart = cable1Connection.position;
			cableEnd = cable1Segments[cable1Segments.Count - 1].position;
			cableMid = (cableStart + cableEnd) / 2f;
		}
		else
		{
			cableStart = cable2Connection.position;
			cableEnd = cable2Segments[cable2Segments.Count - 1].position;
			cableMid = (cableStart + cableEnd) / 2f;
		}
	}

	private void Start()
	{
		if (PlayerSingleton<PlayerMovement>.InstanceExists)
		{
			Register();
		}
		else
		{
			Player.onLocalPlayerSpawned = (Action)Delegate.Combine(Player.onLocalPlayerSpawned, new Action(Register));
		}
		void Register()
		{
			Player.onLocalPlayerSpawned = (Action)Delegate.Remove(Player.onLocalPlayerSpawned, new Action(Register));
			PlayerSingleton<PlayerCamera>.Instance.RegisterMovementEvent(2, UpdateCulling);
		}
	}

	private void UpdateCulling()
	{
		if (PlayerSingleton<PlayerCamera>.InstanceExists)
		{
			float sqrMagnitude = (cableStart - PlayerSingleton<PlayerCamera>.Instance.transform.position).sqrMagnitude;
			float sqrMagnitude2 = (cableEnd - PlayerSingleton<PlayerCamera>.Instance.transform.position).sqrMagnitude;
			float sqrMagnitude3 = (cableMid - PlayerSingleton<PlayerCamera>.Instance.transform.position).sqrMagnitude;
			float num = Mathf.Min(sqrMagnitude, sqrMagnitude2, sqrMagnitude3) * QualitySettings.lodBias;
			Cable1Container.gameObject.SetActive(num < 10000f && Connection1Enabled);
			Cable2Container.gameObject.SetActive(num < 10000f && Connection2Enabled);
		}
	}

	[Button]
	public void Orient()
	{
		if (previousPole == null && nextPole == null)
		{
			Console.LogWarning("No neighbour poles!");
		}
		else if (nextPole != null && previousPole != null)
		{
			Vector3 normalized = (base.transform.position - previousPole.transform.position).normalized;
			Vector3 normalized2 = (nextPole.transform.position - base.transform.position).normalized;
			Vector3 normalized3 = (normalized + normalized2).normalized;
			base.transform.rotation = Quaternion.LookRotation(normalized3, Vector3.up);
		}
		else if (previousPole != null)
		{
			Vector3 normalized4 = (base.transform.position - previousPole.transform.position).normalized;
			base.transform.rotation = Quaternion.LookRotation(normalized4, Vector3.up);
		}
		else if (nextPole != null)
		{
			Vector3 normalized5 = (nextPole.transform.position - base.transform.position).normalized;
			base.transform.rotation = Quaternion.LookRotation(normalized5, Vector3.up);
		}
	}

	[Button]
	public void DrawLines()
	{
		if (previousPole == null)
		{
			if (Connection1Enabled)
			{
				foreach (Transform cable1Segment in cable1Segments)
				{
					cable1Segment.gameObject.SetActive(value: false);
				}
			}
			if (!Connection2Enabled)
			{
				return;
			}
			{
				foreach (Transform cable2Segment in cable2Segments)
				{
					cable2Segment.gameObject.SetActive(value: false);
				}
				return;
			}
		}
		if (Connection1Enabled)
		{
			PowerLine.DrawPowerLine(cable1Connection.position, previousPole.cable1Connection.position, cable1Segments, LengthFactor);
			foreach (Transform cable1Segment2 in cable1Segments)
			{
				cable1Segment2.gameObject.SetActive(value: true);
			}
		}
		if (!Connection2Enabled)
		{
			return;
		}
		PowerLine.DrawPowerLine(cable2Connection.position, previousPole.cable2Connection.position, cable2Segments, LengthFactor);
		foreach (Transform cable2Segment2 in cable2Segments)
		{
			cable2Segment2.gameObject.SetActive(value: true);
		}
	}
}
