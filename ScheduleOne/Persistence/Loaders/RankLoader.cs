using System;
using ScheduleOne.DevUtilities;
using ScheduleOne.Levelling;
using UnityEngine;

namespace ScheduleOne.Persistence.Loaders;

public class RankLoader : Loader
{
	public override void Load(string mainPath)
	{
		if (TryLoadFile(mainPath, out var contents))
		{
			RankData rankData = null;
			try
			{
				rankData = JsonUtility.FromJson<RankData>(contents);
			}
			catch (Exception ex)
			{
				Debug.LogError("Failed to load rank data: " + ex.Message);
			}
			if (rankData != null)
			{
				NetworkSingleton<LevelManager>.Instance.SetData(null, (ERank)rankData.Rank, rankData.Tier, rankData.XP, rankData.TotalXP);
			}
		}
	}
}
