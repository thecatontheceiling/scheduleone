using ScheduleOne.AvatarFramework;
using ScheduleOne.PlayerScripts;
using UnityEngine.Events;

namespace ScheduleOne.Cutscenes;

public class EndCutscene : Cutscene
{
	public UnityEvent onStandUp;

	public UnityEvent onRunStart;

	public UnityEvent onEngineStart;

	public Avatar Avatar;

	public override void Play()
	{
		base.Play();
		Avatar.LoadAvatarSettings(Player.Local.Avatar.CurrentSettings);
	}

	public void StandUp()
	{
		if (onStandUp != null)
		{
			Console.Log("StandUp");
			onStandUp.Invoke();
		}
	}

	public void RunStart()
	{
		if (onRunStart != null)
		{
			Console.Log("RunStart");
			onRunStart.Invoke();
		}
	}

	public void EngineStart()
	{
		if (onEngineStart != null)
		{
			Console.Log("EngineStart");
			onEngineStart.Invoke();
		}
	}

	public void On3rdPerson()
	{
		Avatar.gameObject.SetActive(value: true);
		Avatar.Anim.SetBool("Sitting", value: true);
	}
}
