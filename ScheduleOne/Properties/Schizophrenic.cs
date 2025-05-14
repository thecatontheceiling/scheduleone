using ScheduleOne.Audio;
using ScheduleOne.DevUtilities;
using ScheduleOne.FX;
using ScheduleOne.NPCs;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI;
using ScheduleOne.VoiceOver;
using UnityEngine;

namespace ScheduleOne.Properties;

[CreateAssetMenu(fileName = "Schizophrenic", menuName = "Properties/Schizophrenic Property")]
public class Schizophrenic : Property
{
	public override void ApplyToNPC(NPC npc)
	{
		npc.Avatar.Eyes.SetPupilDilation(0.1f, writeDefault: false);
		npc.Avatar.EmotionManager.AddEmotionOverride("Scared", "Schizophrenic", 0f, Tier);
		npc.PlayVO(EVOLineType.Concerned);
	}

	public override void ApplyToPlayer(Player player)
	{
		player.Avatar.Eyes.SetPupilDilation(0.1f, writeDefault: false);
		player.Avatar.EmotionManager.AddEmotionOverride("Scared", "Schizophrenic", 0f, Tier);
		player.Schizophrenic = true;
		if (player.IsLocalPlayer)
		{
			Singleton<MusicPlayer>.Instance.SetMusicDistorted(distorted: true);
			Singleton<MusicPlayer>.Instance.SetTrackEnabled("Schizo music", enabled: true);
			Singleton<AudioManager>.Instance.SetDistorted(distorted: true);
			Singleton<PostProcessingManager>.Instance.SaturationController.AddOverride(110f, 7, "Schizophrenic");
			Singleton<EyelidOverlay>.Instance.OpenMultiplier.AddOverride(0.7f, 6, "sedating");
		}
	}

	public override void ClearFromNPC(NPC npc)
	{
		npc.Avatar.Eyes.ResetPupilDilation();
		npc.Avatar.EmotionManager.RemoveEmotionOverride("Schizophrenic");
	}

	public override void ClearFromPlayer(Player player)
	{
		player.Avatar.Eyes.ResetPupilDilation();
		player.Avatar.EmotionManager.RemoveEmotionOverride("Schizophrenic");
		player.Schizophrenic = false;
		if (player.IsLocalPlayer)
		{
			Singleton<MusicPlayer>.Instance.SetMusicDistorted(distorted: false);
			Singleton<MusicPlayer>.Instance.SetTrackEnabled("Schizo music", enabled: false);
			Singleton<AudioManager>.Instance.SetDistorted(distorted: false);
			Singleton<PostProcessingManager>.Instance.SaturationController.RemoveOverride("Schizophrenic");
			Singleton<EyelidOverlay>.Instance.OpenMultiplier.RemoveOverride("sedating");
		}
	}
}
