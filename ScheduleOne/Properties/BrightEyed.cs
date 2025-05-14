using ScheduleOne.NPCs;
using ScheduleOne.PlayerScripts;
using UnityEngine;

namespace ScheduleOne.Properties;

[CreateAssetMenu(fileName = "BrightEyed", menuName = "Properties/BrightEyed Property")]
public class BrightEyed : Property
{
	public Color EyeColor;

	public float Emission = 0.5f;

	public float LightIntensity = 1f;

	public override void ApplyToNPC(NPC npc)
	{
		npc.Avatar.Effects.OverrideEyeColor(EyeColor, Emission);
		npc.Avatar.Effects.SetEyeLightEmission(LightIntensity, EyeColor);
	}

	public override void ApplyToPlayer(Player player)
	{
		player.Avatar.Effects.OverrideEyeColor(EyeColor, Emission);
		player.Avatar.Effects.SetEyeLightEmission(LightIntensity, EyeColor);
	}

	public override void ClearFromNPC(NPC npc)
	{
		npc.Avatar.Effects.ResetEyeColor();
		npc.Avatar.Effects.SetEyeLightEmission(0f, EyeColor);
	}

	public override void ClearFromPlayer(Player player)
	{
		player.Avatar.Effects.ResetEyeColor();
		player.Avatar.Effects.SetEyeLightEmission(0f, EyeColor);
	}
}
