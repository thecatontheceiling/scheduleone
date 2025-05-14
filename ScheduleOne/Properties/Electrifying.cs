using ScheduleOne.NPCs;
using ScheduleOne.PlayerScripts;
using UnityEngine;

namespace ScheduleOne.Properties;

[CreateAssetMenu(fileName = "Electrifying", menuName = "Properties/Electrifying Property")]
public class Electrifying : Property
{
	public Color EyeColor;

	public override void ApplyToNPC(NPC npc)
	{
		npc.Avatar.Effects.SetZapped(zapped: true);
		npc.Avatar.Effects.OverrideEyeColor(EyeColor, 0.5f);
	}

	public override void ApplyToPlayer(Player player)
	{
		player.Avatar.Effects.SetZapped(zapped: true);
		player.Avatar.Effects.OverrideEyeColor(EyeColor, 0.5f);
	}

	public override void ClearFromNPC(NPC npc)
	{
		npc.Avatar.Effects.SetZapped(zapped: false);
		npc.Avatar.Effects.ResetEyeColor();
	}

	public override void ClearFromPlayer(Player player)
	{
		player.Avatar.Effects.SetZapped(zapped: false);
		player.Avatar.Effects.ResetEyeColor();
	}
}
