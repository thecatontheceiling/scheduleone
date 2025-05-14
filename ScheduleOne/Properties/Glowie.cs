using ScheduleOne.NPCs;
using ScheduleOne.PlayerScripts;
using UnityEngine;

namespace ScheduleOne.Properties;

[CreateAssetMenu(fileName = "Glowie", menuName = "Properties/Glowie Property")]
public class Glowie : Property
{
	[ColorUsage(true, true)]
	[SerializeField]
	public Color GlowColor = Color.white;

	public override void ApplyToNPC(NPC npc)
	{
		npc.Avatar.Effects.SetGlowingOn(GlowColor);
	}

	public override void ApplyToPlayer(Player player)
	{
		player.Avatar.Effects.SetGlowingOn(GlowColor);
	}

	public override void ClearFromNPC(NPC npc)
	{
		npc.Avatar.Effects.SetGlowingOff();
	}

	public override void ClearFromPlayer(Player player)
	{
		player.Avatar.Effects.SetGlowingOff();
	}
}
