using System.Collections;
using ScheduleOne.DevUtilities;
using ScheduleOne.NPCs;
using ScheduleOne.PlayerScripts;
using ScheduleOne.VoiceOver;
using UnityEngine;

namespace ScheduleOne.Properties;

[CreateAssetMenu(fileName = "Seizure", menuName = "Properties/Seizure Property")]
public class Seizure : Property
{
	public const float CAMERA_JITTER_INTENSITY = 1f;

	public const float DURATION_NPC = 60f;

	public const float DURATION_PLAYER = 30f;

	public override void ApplyToNPC(NPC npc)
	{
		npc.PlayVO(EVOLineType.Hurt);
		npc.behaviour.RagdollBehaviour.Seizure = true;
		npc.Movement.ActivateRagdoll_Server();
		Singleton<CoroutineService>.Instance.StartCoroutine(Wait());
		IEnumerator Wait()
		{
			yield return new WaitForSeconds(60f);
			npc.behaviour.RagdollBehaviour.Seizure = false;
		}
	}

	public override void ApplyToPlayer(Player player)
	{
		player.Seizure = true;
		Singleton<CoroutineService>.Instance.StartCoroutine(Wait());
		IEnumerator Wait()
		{
			yield return new WaitForSeconds(30f);
			player.Seizure = false;
		}
	}

	public override void ClearFromNPC(NPC npc)
	{
		npc.behaviour.RagdollBehaviour.Seizure = false;
	}

	public override void ClearFromPlayer(Player player)
	{
		player.Seizure = false;
	}
}
