using ScheduleOne.NPCs;
using ScheduleOne.PlayerScripts;
using ScheduleOne.VoiceOver;
using UnityEngine;

namespace ScheduleOne.Properties;

[CreateAssetMenu(fileName = "Zombifying", menuName = "Properties/Zombifying Property")]
public class Zombifying : Property
{
	public VODatabase zombieVODatabase;

	public override void ApplyToNPC(NPC npc)
	{
		npc.Avatar.Effects.SetZombified(zombified: true);
		npc.VoiceOverEmitter.SetRuntimePitchMultiplier(0.5f);
		npc.VoiceOverEmitter.SetDatabase(zombieVODatabase, writeDefault: false);
		npc.PlayVO(EVOLineType.Grunt);
		npc.Movement.SpeedController.SpeedMultiplier = 0.4f;
	}

	public override void ApplyToPlayer(Player player)
	{
		player.Avatar.Effects.SetZombified(zombified: true);
	}

	public override void ClearFromNPC(NPC npc)
	{
		npc.Avatar.Effects.SetZombified(zombified: false);
		npc.VoiceOverEmitter.SetRuntimePitchMultiplier(1f);
		npc.VoiceOverEmitter.ResetDatabase();
		npc.Movement.SpeedController.SpeedMultiplier = 1f;
	}

	public override void ClearFromPlayer(Player player)
	{
		player.Avatar.Effects.SetZombified(zombified: false);
	}
}
