using System;
using System.Collections.Generic;
using ScheduleOne.DevUtilities;
using Steamworks;

namespace ScheduleOne;

public class AchievementManager : PersistentSingleton<AchievementManager>
{
	public enum EAchievement
	{
		COMPLETE_PROLOGUE = 0,
		RV_DESTROYED = 1,
		DEALER_RECRUITED = 2,
		MASTER_CHEF = 3,
		BUSINESSMAN = 4,
		BIGWIG = 5,
		MAGNATE = 6,
		UPSTANDING_CITIZEN = 7,
		ROLLING_IN_STYLE = 8,
		LONG_ARM_OF_THE_LAW = 9,
		INDIAN_DEALER = 10
	}

	private EAchievement[] achievements;

	private Dictionary<EAchievement, bool> achievementUnlocked = new Dictionary<EAchievement, bool>();

	protected override void Awake()
	{
		base.Awake();
		if (!(Singleton<AchievementManager>.Instance == null) && !(Singleton<AchievementManager>.Instance != this))
		{
			achievements = (EAchievement[])Enum.GetValues(typeof(EAchievement));
			EAchievement[] array = achievements;
			foreach (EAchievement key in array)
			{
				achievementUnlocked.Add(key, value: false);
			}
		}
	}

	protected override void Start()
	{
		base.Start();
		if (!(Singleton<AchievementManager>.Instance == null) && !(Singleton<AchievementManager>.Instance != this))
		{
			PullAchievements();
		}
	}

	private void PullAchievements()
	{
		if (!SteamManager.Initialized)
		{
			Console.LogWarning("Steamworks not initialized, cannot pull achievement stats");
			return;
		}
		EAchievement[] array = achievements;
		for (int i = 0; i < array.Length; i++)
		{
			EAchievement key = array[i];
			SteamUserStats.GetAchievement(key.ToString(), out var pbAchieved);
			achievementUnlocked[key] = pbAchieved;
		}
	}

	public void UnlockAchievement(EAchievement achievement)
	{
		if (!SteamManager.Initialized)
		{
			Console.LogWarning("Steamworks not initialized, cannot unlock achievement");
		}
		else if (!achievementUnlocked[achievement])
		{
			Console.Log($"Unlocking achievement: {achievement}");
			SteamUserStats.SetAchievement(achievement.ToString());
			SteamUserStats.StoreStats();
			achievementUnlocked[achievement] = true;
		}
	}
}
