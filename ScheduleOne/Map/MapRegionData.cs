using System;
using System.Collections.Generic;
using System.Linq;
using ScheduleOne.DevUtilities;
using ScheduleOne.Economy;
using ScheduleOne.Levelling;
using ScheduleOne.NPCs;
using ScheduleOne.NPCs.Relation;
using UnityEngine;

namespace ScheduleOne.Map;

[Serializable]
public class MapRegionData
{
	public EMapRegion Region;

	public string Name;

	public FullRank RankRequirement;

	public NPC[] StartingNPCs;

	public Sprite RegionSprite;

	public DeliveryLocation[] RegionDeliveryLocations;

	public bool IsUnlocked
	{
		get
		{
			if (NetworkSingleton<LevelManager>.InstanceExists)
			{
				return NetworkSingleton<LevelManager>.Instance.GetFullRank() >= RankRequirement;
			}
			return false;
		}
	}

	public DeliveryLocation GetRandomUnscheduledDeliveryLocation()
	{
		List<DeliveryLocation> list = RegionDeliveryLocations.Where((DeliveryLocation x) => x.ScheduledContracts.Count == 0).ToList();
		if (list.Count == 0)
		{
			Console.LogWarning("No unscheduled delivery locations found for " + Region);
			return null;
		}
		return list[UnityEngine.Random.Range(0, list.Count)];
	}

	public void SetUnlocked()
	{
		NPC[] startingNPCs = StartingNPCs;
		foreach (NPC nPC in startingNPCs)
		{
			if (!nPC.RelationData.Unlocked)
			{
				nPC.RelationData.Unlock(NPCRelationData.EUnlockType.DirectApproach, notify: false);
			}
		}
	}
}
