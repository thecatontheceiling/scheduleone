using System;
using System.Collections.Generic;
using System.Linq;
using EasyButtons;
using ScheduleOne.DevUtilities;
using ScheduleOne.GameTime;
using ScheduleOne.ItemFramework;
using ScheduleOne.Levelling;
using ScheduleOne.Product;
using ScheduleOne.Properties;
using UnityEngine;

namespace ScheduleOne.Economy;

[Serializable]
[CreateAssetMenu(fileName = "CustomerData", menuName = "ScriptableObjects/CustomerData", order = 1)]
public class CustomerData : ScriptableObject
{
	public CustomerAffinityData DefaultAffinityData;

	[Header("Preferred Properties - Properties the customer prefers in a product.")]
	public List<ScheduleOne.Properties.Property> PreferredProperties = new List<ScheduleOne.Properties.Property>();

	[Header("Spending Behaviour")]
	public float MinWeeklySpend = 200f;

	public float MaxWeeklySpend = 500f;

	[Range(0f, 7f)]
	public int MinOrdersPerWeek = 1;

	[Range(0f, 7f)]
	public int MaxOrdersPerWeek = 5;

	[Header("Timing Settings")]
	public int OrderTime = 1200;

	public EDay PreferredOrderDay;

	[Header("Standards")]
	public ECustomerStandard Standards = ECustomerStandard.Moderate;

	[Header("Direct approaching")]
	public bool CanBeDirectlyApproached = true;

	public bool GuaranteeFirstSampleSuccess;

	[Tooltip("The average relationship of mutual customers to provide a 50% chance of success")]
	[Range(0f, 5f)]
	public float MinMutualRelationRequirement = 3f;

	[Tooltip("The average relationship of mutual customers to provide a 100% chance of success")]
	[Range(0f, 5f)]
	public float MaxMutualRelationRequirement = 5f;

	[Tooltip("If direct approach fails, whats the chance the police will be called?")]
	[Range(0f, 1f)]
	public float CallPoliceChance = 0.5f;

	[Header("Dependence")]
	[Tooltip("How quickly the customer builds dependence")]
	[Range(0f, 2f)]
	public float DependenceMultiplier = 1f;

	[Tooltip("The customer's starting (and lowest possible) dependence level")]
	[Range(0f, 1f)]
	public float BaseAddiction;

	public Action onChanged;

	public static float GetQualityScalar(EQuality quality)
	{
		return quality switch
		{
			EQuality.Trash => 0f, 
			EQuality.Poor => 0.25f, 
			EQuality.Standard => 0.5f, 
			EQuality.Premium => 0.75f, 
			EQuality.Heavenly => 1f, 
			_ => 0f, 
		};
	}

	public List<EDay> GetOrderDays(float dependence, float normalizedRelationship)
	{
		float t = Mathf.Max(dependence, normalizedRelationship);
		int num = Mathf.RoundToInt(Mathf.Lerp(MinOrdersPerWeek, MaxOrdersPerWeek, t));
		int preferredOrderDay = (int)PreferredOrderDay;
		int a = Mathf.RoundToInt(7f / (float)num);
		a = Mathf.Max(a, 1);
		List<EDay> list = new List<EDay>();
		for (int i = 0; i < 7; i += a)
		{
			list.Add((EDay)((i + preferredOrderDay) % 7));
		}
		return list;
	}

	public float GetAdjustedWeeklySpend(float normalizedRelationship)
	{
		return Mathf.Lerp(MinWeeklySpend, MaxWeeklySpend, normalizedRelationship) * LevelManager.GetOrderLimitMultiplier(NetworkSingleton<LevelManager>.Instance.GetFullRank());
	}

	[Button]
	public void RandomizeAffinities()
	{
		DefaultAffinityData = new CustomerAffinityData();
		List<EDrugType> list = Enum.GetValues(typeof(EDrugType)).Cast<EDrugType>().ToList();
		for (int i = 0; i < list.Count; i++)
		{
			DefaultAffinityData.ProductAffinities.Add(new ProductTypeAffinity
			{
				DrugType = list[i],
				Affinity = 0f
			});
		}
		for (int j = 0; j < DefaultAffinityData.ProductAffinities.Count; j++)
		{
			DefaultAffinityData.ProductAffinities[j].Affinity = UnityEngine.Random.Range(-1f, 1f);
		}
	}

	[Button]
	public void RandomizeProperties()
	{
		string[] obj = new string[5] { "Properties/Tier1", "Properties/Tier2", "Properties/Tier3", "Properties/Tier4", "Properties/Tier5" };
		List<ScheduleOne.Properties.Property> list = new List<ScheduleOne.Properties.Property>();
		string[] array = obj;
		foreach (string path in array)
		{
			list.AddRange(Resources.LoadAll<ScheduleOne.Properties.Property>(path));
		}
		PreferredProperties.Clear();
		for (int j = 0; j < 3; j++)
		{
			int index = UnityEngine.Random.Range(0, list.Count);
			PreferredProperties.Add(list[index]);
			list.RemoveAt(index);
		}
	}

	[Button]
	public void RandomizeTiming()
	{
		PreferredOrderDay = (EDay)UnityEngine.Random.Range(0, 7);
		int num = UnityEngine.Random.Range(420, 1440);
		num = Mathf.RoundToInt((float)num / 15f) * 15;
		OrderTime = TimeManager.Get24HourTimeFromMinSum(num);
	}

	[Button]
	public void ClearInvalid()
	{
		while (DefaultAffinityData.ProductAffinities.Count > 3)
		{
			DefaultAffinityData.ProductAffinities.RemoveAt(DefaultAffinityData.ProductAffinities.Count - 1);
		}
	}
}
