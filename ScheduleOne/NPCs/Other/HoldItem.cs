using ScheduleOne.AvatarFramework.Equipping;
using UnityEngine;

namespace ScheduleOne.NPCs.Other;

public class HoldItem : MonoBehaviour
{
	public NPC Npc;

	public AvatarEquippable Equippable;

	public bool active { get; protected set; }

	public void Begin()
	{
		active = true;
		Npc.SetEquippable_Return(Equippable.AssetPath);
	}

	private void Update()
	{
		_ = active;
	}

	public void End()
	{
		active = false;
		Npc.SetEquippable_Return(string.Empty);
	}
}
