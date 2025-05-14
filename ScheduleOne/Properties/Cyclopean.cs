using ScheduleOne.NPCs;
using ScheduleOne.PlayerScripts;
using UnityEngine;

namespace ScheduleOne.Properties;

[CreateAssetMenu(fileName = "Cyclopean", menuName = "Properties/Cyclopean Property")]
public class Cyclopean : Property
{
	public override void ApplyToNPC(NPC npc)
	{
		npc.Avatar.Effects.SetCyclopean(enabled: true);
	}

	public override void ApplyToPlayer(Player player)
	{
		player.Avatar.Effects.SetCyclopean(enabled: true);
	}

	public override void ClearFromNPC(NPC npc)
	{
		npc.Avatar.Effects.SetCyclopean(enabled: false);
	}

	public override void ClearFromPlayer(Player player)
	{
		player.Avatar.Effects.SetCyclopean(enabled: false);
	}
}
