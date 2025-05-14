using ScheduleOne.NPCs;
using ScheduleOne.PlayerScripts;
using UnityEngine;

namespace ScheduleOne.Properties;

[CreateAssetMenu(fileName = "Slippery", menuName = "Properties/Slippery Property")]
public class Slippery : Property
{
	public override void ApplyToNPC(NPC npc)
	{
		npc.Movement.SlipperyMode = true;
	}

	public override void ApplyToPlayer(Player player)
	{
		player.Slippery = true;
	}

	public override void ClearFromNPC(NPC npc)
	{
		npc.Movement.SlipperyMode = false;
	}

	public override void ClearFromPlayer(Player player)
	{
		player.Slippery = false;
	}
}
