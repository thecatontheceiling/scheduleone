using ScheduleOne.DevUtilities;
using ScheduleOne.NPCs;
using ScheduleOne.PlayerScripts;
using UnityEngine;

namespace ScheduleOne.Properties;

[CreateAssetMenu(fileName = "Lethal", menuName = "Properties/Lethal Property")]
public class Lethal : Property
{
	public const float HEALTH_DRAIN_PLAYER = 15f;

	public const float HEALTH_DRAIN_NPC = 15f;

	public override void ApplyToNPC(NPC npc)
	{
		npc.Avatar.Effects.SetSicklySkinColor();
		npc.Avatar.EmotionManager.AddEmotionOverride("Concerned", "Sickly", 0f, Tier);
		npc.Avatar.Effects.TriggerSick();
		npc.Health.SetAfflictedWithLethalEffect(value: true);
	}

	public override void ApplyToPlayer(Player player)
	{
		player.Avatar.Effects.SetSicklySkinColor();
		player.Avatar.EmotionManager.AddEmotionOverride("Concerned", "Sickly", 0f, Tier);
		player.Avatar.Effects.TriggerSick();
		player.Health.SetAfflictedWithLethalEffect(value: true);
		if (player.IsOwner)
		{
			PlayerSingleton<PlayerCamera>.Instance.HeartbeatSoundController.VolumeController.AddOverride(0.7f, Tier, "sickly");
			PlayerSingleton<PlayerCamera>.Instance.HeartbeatSoundController.PitchController.AddOverride(1f, Tier, "sickly");
		}
	}

	public override void ClearFromNPC(NPC npc)
	{
		npc.Avatar.Effects.SetSicklySkinColor(mirror: false);
		npc.Avatar.EmotionManager.RemoveEmotionOverride("Sickly");
		npc.Avatar.Effects.TriggerSick();
		npc.Health.SetAfflictedWithLethalEffect(value: false);
	}

	public override void ClearFromPlayer(Player player)
	{
		player.Avatar.Effects.SetSicklySkinColor(mirror: false);
		player.Avatar.EmotionManager.RemoveEmotionOverride("Sickly");
		player.Avatar.Effects.TriggerSick();
		player.Health.SetAfflictedWithLethalEffect(value: false);
		if (player.IsOwner)
		{
			PlayerSingleton<PlayerCamera>.Instance.HeartbeatSoundController.VolumeController.RemoveOverride("sickly");
			PlayerSingleton<PlayerCamera>.Instance.HeartbeatSoundController.PitchController.RemoveOverride("sickly");
		}
	}
}
