using System.Collections;
using EasyButtons;
using UnityEngine;
using UnityEngine.Events;

namespace ScheduleOne.Tools;

public class DelayedUnityEvent : MonoBehaviour
{
	public float Delay = 1f;

	public UnityEvent onDelayStart;

	public UnityEvent onDelayedExecute;

	[Button]
	public void Execute()
	{
		StartCoroutine(Wait());
		IEnumerator Wait()
		{
			if (onDelayStart != null)
			{
				onDelayStart.Invoke();
			}
			yield return new WaitForSeconds(Delay);
			if (onDelayedExecute != null)
			{
				onDelayedExecute.Invoke();
			}
		}
	}
}
