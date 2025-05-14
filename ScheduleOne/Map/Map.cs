using System;
using ScheduleOne.DevUtilities;
using ScheduleOne.Levelling;
using ScheduleOne.Persistence;
using ScheduleOne.UI;
using UnityEngine;

namespace ScheduleOne.Map;

public class Map : Singleton<Map>
{
	public MapRegionData[] Regions;

	[Header("References")]
	public PoliceStation PoliceStation;

	public MedicalCentre MedicalCentre;

	public Transform TreeBounds;

	protected override void Awake()
	{
		base.Awake();
		if (!GameManager.IS_TUTORIAL)
		{
			foreach (EMapRegion region in Enum.GetValues(typeof(EMapRegion)))
			{
				if (Regions == null || Array.Find(Regions, (MapRegionData x) => x.Region == region) == null)
				{
					Console.LogError($"No region data found for {region}");
				}
			}
		}
		if (TreeBounds != null)
		{
			TreeBounds.gameObject.SetActive(value: false);
		}
	}

	protected override void Start()
	{
		base.Start();
		LevelManager levelManager = NetworkSingleton<LevelManager>.Instance;
		levelManager.onRankUp = (Action<FullRank, FullRank>)Delegate.Combine(levelManager.onRankUp, new Action<FullRank, FullRank>(OnRankUp));
		Singleton<LoadManager>.Instance.onLoadComplete.AddListener(GameLoaded);
	}

	protected override void OnDestroy()
	{
		if (Singleton<LoadManager>.InstanceExists)
		{
			Singleton<LoadManager>.Instance.onLoadComplete.RemoveListener(GameLoaded);
		}
		base.OnDestroy();
	}

	public MapRegionData GetRegionData(EMapRegion region)
	{
		return Array.Find(Regions, (MapRegionData x) => x.Region == region);
	}

	private void GameLoaded()
	{
		MapRegionData[] regions = Regions;
		foreach (MapRegionData mapRegionData in regions)
		{
			if (mapRegionData.IsUnlocked)
			{
				mapRegionData.SetUnlocked();
			}
		}
	}

	private void OnRankUp(FullRank oldRank, FullRank newRank)
	{
		MapRegionData[] regions = Regions;
		foreach (MapRegionData mapRegionData in regions)
		{
			if (oldRank < mapRegionData.RankRequirement && newRank >= mapRegionData.RankRequirement)
			{
				mapRegionData.SetUnlocked();
				if (!Singleton<LoadManager>.Instance.IsLoading)
				{
					Singleton<RegionUnlockedCanvas>.Instance.QueueUnlocked(mapRegionData.Region);
				}
			}
		}
	}
}
