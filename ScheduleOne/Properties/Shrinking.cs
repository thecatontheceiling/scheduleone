using ScheduleOne.NPCs;
using ScheduleOne.PlayerScripts;
using UnityEngine;

namespace ScheduleOne.Properties;

[CreateAssetMenu(fileName = "Shrinking", menuName = "Properties/Shrinking Property")]
public class Shrinking : Property
{
	public const float Scale = 0.8f;

	public const float LerpTime = 1f;

	public override void ApplyToNPC(NPC npc)
	{
		npc.SetScale(0.8f, 1f);
		npc.VoiceOverEmitter.SetRuntimePitchMultiplier(1.5f);
	}

	public override void ApplyToPlayer(Player player)
	{
		player.SetScale(0.8f, 1f);
	}

	public override void ClearFromNPC(NPC npc)
	{
		npc.SetScale(1f, 1f);
		npc.VoiceOverEmitter.SetRuntimePitchMultiplier(1f);
	}

	public override void ClearFromPlayer(Player player)
	{
		player.SetScale(1f, 1f);
	}
}
