using System;
using ScheduleOne.DevUtilities;
using UnityEngine;
using UnityEngine.Events;

namespace ScheduleOne.GameTime;

public class AnalogueClock : MonoBehaviour
{
	public Transform MinHand;

	public Transform HourHand;

	public Vector3 RotationAxis = Vector3.forward;

	public UnityEvent onNoon;

	public UnityEvent onMidnight;

	public void Start()
	{
		TimeManager instance = NetworkSingleton<TimeManager>.Instance;
		instance.onMinutePass = (Action)Delegate.Remove(instance.onMinutePass, new Action(MinPass));
		TimeManager instance2 = NetworkSingleton<TimeManager>.Instance;
		instance2.onMinutePass = (Action)Delegate.Combine(instance2.onMinutePass, new Action(MinPass));
		MinPass();
	}

	private void OnDestroy()
	{
		if (NetworkSingleton<TimeManager>.InstanceExists)
		{
			TimeManager instance = NetworkSingleton<TimeManager>.Instance;
			instance.onMinutePass = (Action)Delegate.Remove(instance.onMinutePass, new Action(MinPass));
		}
	}

	public void MinPass()
	{
		int currentTime = NetworkSingleton<TimeManager>.Instance.CurrentTime;
		int minSumFrom24HourTime = TimeManager.GetMinSumFrom24HourTime(currentTime);
		float num = minSumFrom24HourTime % 60;
		float num2 = minSumFrom24HourTime / 60;
		float num3 = num / 60f * 360f;
		float num4 = num2 / 12f * 360f + num3 / 12f;
		if (currentTime == 1200 && onNoon != null)
		{
			onNoon.Invoke();
		}
		if (currentTime == 0 && onMidnight != null)
		{
			onMidnight.Invoke();
		}
		MinHand.localEulerAngles = RotationAxis * num3;
		HourHand.localEulerAngles = RotationAxis * num4;
	}
}
