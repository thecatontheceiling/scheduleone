using ScheduleOne.Audio;
using UnityEngine;

namespace ScheduleOne.Vehicles.Sound;

public class VehicleSound : MonoBehaviour
{
	public const float COLLISION_SOUND_COOLDOWN = 0.5f;

	public float VolumeMultiplier = 1f;

	[Header("References")]
	public AudioSourceController EngineStartSource;

	public AudioSourceController EngineIdleSource;

	public AudioSourceController EngineLoopSource;

	public AudioSourceController HandbrakeSource;

	public AudioSourceController HonkSource;

	public AudioSourceController ImpactSound;

	[Header("Impact Sounds")]
	public float MinCollisionMomentum = 3000f;

	public float MaxCollisionMomentum = 20000f;

	public float MinCollisionVolume = 0.2f;

	public float MaxCollisionVolume = 1f;

	public float MinCollisionPitch = 0.6f;

	public float MaxCollisionPitch = 1.1f;

	[Header("Engine Loop Settings")]
	public AnimationCurve EngineLoopPitchCurve;

	public float EngineLoopPitchMultiplier = 1f;

	public AnimationCurve EngineLoopVolumeCurve;

	private float currentIdleVolume;

	private float lastCollisionTime;

	private float lastCollisionMomentum;

	public LandVehicle Vehicle { get; private set; }

	protected virtual void Awake()
	{
		Vehicle = GetComponentInParent<LandVehicle>();
		if (!(Vehicle == null))
		{
			Vehicle.onHandbrakeApplied.AddListener(HandbrakeApplied);
			Vehicle.onVehicleStart.AddListener(EngineStart);
			EngineIdleSource.VolumeMultiplier = 0f;
			EngineLoopSource.VolumeMultiplier = 0f;
			Vehicle.onCollision.AddListener(OnCollision);
		}
	}

	protected virtual void FixedUpdate()
	{
		UpdateIdle();
	}

	private void UpdateIdle()
	{
		if (Vehicle.isOccupied)
		{
			currentIdleVolume = Mathf.MoveTowards(currentIdleVolume, 1f, Time.fixedDeltaTime * 2f);
			float time = Mathf.Abs(Vehicle.VelocityCalculator.Velocity.magnitude * 3.6f / Vehicle.TopSpeed);
			EngineLoopSource.AudioSource.pitch = EngineLoopPitchCurve.Evaluate(time) * EngineLoopPitchMultiplier;
			EngineLoopSource.VolumeMultiplier = EngineLoopVolumeCurve.Evaluate(time) * VolumeMultiplier;
			if (!EngineLoopSource.AudioSource.isPlaying)
			{
				EngineLoopSource.Play();
			}
		}
		else
		{
			currentIdleVolume = Mathf.MoveTowards(currentIdleVolume, 0f, Time.fixedDeltaTime * 2f);
			if (EngineLoopSource.AudioSource.isPlaying)
			{
				EngineLoopSource.Stop();
			}
		}
		EngineIdleSource.VolumeMultiplier = currentIdleVolume * VolumeMultiplier;
		if (currentIdleVolume > 0f)
		{
			if (!EngineIdleSource.AudioSource.isPlaying)
			{
				EngineIdleSource.Play();
			}
		}
		else
		{
			EngineIdleSource.Stop();
		}
	}

	protected void HandbrakeApplied()
	{
		HandbrakeSource.VolumeMultiplier = VolumeMultiplier;
		HandbrakeSource.Play();
	}

	protected void EngineStart()
	{
		EngineStartSource.VolumeMultiplier = VolumeMultiplier;
		EngineStartSource.Play();
	}

	public void Honk()
	{
		HonkSource.Play();
	}

	private void OnCollision(Collision collision)
	{
		float num = collision.relativeVelocity.magnitude * Vehicle.Rb.mass;
		if (collision.gameObject.layer == LayerMask.NameToLayer("NPC"))
		{
			num *= 0.2f;
		}
		if (!(num < MinCollisionMomentum) && (!(Time.time - lastCollisionTime < 0.5f) || !(num < lastCollisionMomentum)))
		{
			float t = Mathf.InverseLerp(MinCollisionMomentum, MaxCollisionMomentum, num);
			ImpactSound.VolumeMultiplier = Mathf.Lerp(MinCollisionVolume, MaxCollisionVolume, t);
			ImpactSound.PitchMultiplier = Mathf.Lerp(MaxCollisionPitch, MinCollisionPitch, t);
			ImpactSound.transform.position = collision.contacts[0].point;
			ImpactSound.Play();
			lastCollisionTime = Time.time;
			lastCollisionMomentum = num;
		}
	}
}
