using System.Collections;
using FishNet;
using ScheduleOne.Audio;
using ScheduleOne.Combat;
using ScheduleOne.DevUtilities;
using UnityEngine;

namespace ScheduleOne.FX;

public class CountdownExplosion : MonoBehaviour
{
	public const float COUNTDOWN = 30f;

	public const float TICK_SPACING_MAX = 1f;

	public const float TICK_SPACING_MIN = 0.1f;

	public AudioSourceController TickSound;

	private Coroutine countdownRoutine;

	public void Trigger()
	{
		countdownRoutine = Singleton<CoroutineService>.Instance.StartCoroutine(Routine());
		IEnumerator Routine()
		{
			float timeUntilNextTick = 1f;
			for (float i = 0f; i < 30f; i += Time.deltaTime)
			{
				timeUntilNextTick -= Time.deltaTime;
				if (timeUntilNextTick <= 0f)
				{
					timeUntilNextTick = Mathf.Lerp(1f, 0.1f, i / 30f);
					TickSound.PitchMultiplier = Mathf.Lerp(1f, 1.1f, i / 30f);
					TickSound.VolumeMultiplier = Mathf.Lerp(0.6f, 1f, i / 30f);
					TickSound.Play();
				}
				yield return new WaitForEndOfFrame();
			}
			if (InstanceFinder.IsServer)
			{
				NetworkSingleton<CombatManager>.Instance.CreateExplosion(base.transform.position, ExplosionData.DefaultSmall);
			}
			countdownRoutine = null;
		}
	}

	public void StopCountdown()
	{
		if (countdownRoutine != null)
		{
			StopCoroutine(countdownRoutine);
		}
	}
}
