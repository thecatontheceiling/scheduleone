using UnityEngine;
using UnityEngine.Events;

namespace ScheduleOne.Tools;

public class GameVersionEvents : MonoBehaviour
{
	public UnityEvent onFullGame;

	public UnityEvent onDemoGame;

	private void Start()
	{
		if (onFullGame != null)
		{
			onFullGame.Invoke();
		}
	}
}
