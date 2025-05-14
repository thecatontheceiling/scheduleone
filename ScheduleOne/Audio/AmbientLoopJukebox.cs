using System;
using System.Collections.Generic;
using GameKit.Utilities;
using ScheduleOne.DevUtilities;
using ScheduleOne.GameTime;
using UnityEngine;

namespace ScheduleOne.Audio;

[RequireComponent(typeof(AudioSourceController))]
public class AmbientLoopJukebox : MonoBehaviour
{
	public AnimationCurve VolumeCurve;

	public List<AudioClip> Clips = new List<AudioClip>();

	private AudioSourceController audioSourceController;

	private int currentClipIndex;

	private float musicScale = 1f;

	private void Start()
	{
		audioSourceController = GetComponent<AudioSourceController>();
		audioSourceController.Play();
		Clips.Shuffle();
		TimeManager instance = NetworkSingleton<TimeManager>.Instance;
		instance.onMinutePass = (Action)Delegate.Combine(instance.onMinutePass, new Action(MinPass));
	}

	private void Update()
	{
		musicScale = Singleton<AudioManager>.Instance.GetScaledMusicVolumeMultiplier(0.3f);
	}

	private void MinPass()
	{
		float num = VolumeCurve.Evaluate((float)NetworkSingleton<TimeManager>.Instance.DailyMinTotal / 1440f);
		audioSourceController.VolumeMultiplier = num * musicScale;
		if (!audioSourceController.isPlaying)
		{
			currentClipIndex = (currentClipIndex + 1) % Clips.Count;
			audioSourceController.AudioSource.clip = Clips[currentClipIndex];
			audioSourceController.Play();
		}
	}
}
