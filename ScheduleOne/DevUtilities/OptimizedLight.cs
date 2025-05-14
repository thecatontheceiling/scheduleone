using System;
using ScheduleOne.PlayerScripts;
using UnityEngine;

namespace ScheduleOne.DevUtilities;

[RequireComponent(typeof(Light))]
[ExecuteInEditMode]
public class OptimizedLight : MonoBehaviour
{
	public bool Enabled = true;

	[HideInInspector]
	public bool DisabledForOptimization;

	[Range(10f, 500f)]
	public float MaxDistance = 100f;

	public Light _Light;

	private bool culled;

	private float maxDistanceSquared;

	public virtual void Awake()
	{
		_Light = GetComponent<Light>();
		maxDistanceSquared = MaxDistance * MaxDistance;
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
			PlayerSingleton<PlayerCamera>.Instance.RegisterMovementEvent(Mathf.RoundToInt(Mathf.Clamp(MaxDistance / 10f, 0.5f, 20f)), UpdateCull);
		}
	}

	private void OnDestroy()
	{
		if (PlayerSingleton<PlayerCamera>.InstanceExists)
		{
			PlayerSingleton<PlayerCamera>.Instance.DeregisterMovementEvent(UpdateCull);
		}
	}

	public virtual void FixedUpdate()
	{
		if (_Light != null)
		{
			_Light.enabled = Enabled && !DisabledForOptimization && !culled;
		}
	}

	private void UpdateCull()
	{
		if (!(this == null) && !(base.gameObject == null))
		{
			culled = Vector3.SqrMagnitude(PlayerSingleton<PlayerCamera>.Instance.transform.position - base.transform.position) > maxDistanceSquared * QualitySettings.lodBias;
		}
	}

	public void SetEnabled(bool enabled)
	{
		Enabled = enabled;
	}
}
