using System.Linq;
using FishNet;
using ScheduleOne.DevUtilities;
using ScheduleOne.EntityFramework;
using ScheduleOne.Growing;
using ScheduleOne.ObjectScripts;
using ScheduleOne.Variables;
using UnityEngine;

namespace ScheduleOne.Property;

public class Bungalow : Property
{
	public Transform ModelContainer;

	private bool NetworkInitialize___EarlyScheduleOne_002EProperty_002EBungalowAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002EProperty_002EBungalowAssembly_002DCSharp_002Edll_Excuted;

	protected override void Start()
	{
		base.Start();
		InvokeRepeating("UpdateVariables", 0f, 0.5f);
	}

	private void UpdateVariables()
	{
		if (!NetworkSingleton<VariableDatabase>.InstanceExists || !InstanceFinder.IsServer)
		{
			return;
		}
		Pot[] array = (from x in BuildableItems
			where x is Pot
			select x as Pot).ToArray();
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = BuildableItems.Where((BuildableItem x) => x is PackagingStation).Count();
		for (int num6 = 0; num6 < array.Length; num6++)
		{
			if (array[num6].IsFilledWithSoil)
			{
				num++;
			}
			if (array[num6].NormalizedWaterLevel > 0.2f)
			{
				num2++;
			}
			if (array[num6].Plant != null)
			{
				num3++;
			}
			if ((bool)array[num6].AppliedAdditives.Find((Additive x) => x.AdditiveName == "Speed Grow"))
			{
				num4++;
			}
		}
		NetworkSingleton<VariableDatabase>.Instance.SetVariableValue("Bungalow_Pots", array.Length.ToString());
		NetworkSingleton<VariableDatabase>.Instance.SetVariableValue("Bungalow_Soil_Pots", num.ToString());
		NetworkSingleton<VariableDatabase>.Instance.SetVariableValue("Bungalow_Watered_Pots", num2.ToString());
		NetworkSingleton<VariableDatabase>.Instance.SetVariableValue("Bungalow_Seed_Pots", num3.ToString());
		NetworkSingleton<VariableDatabase>.Instance.SetVariableValue("Bungalow_PackagingStations", num5.ToString());
	}

	public override void NetworkInitialize___Early()
	{
		if (!NetworkInitialize___EarlyScheduleOne_002EProperty_002EBungalowAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002EProperty_002EBungalowAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize___Early();
		}
	}

	public override void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002EProperty_002EBungalowAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002EProperty_002EBungalowAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize__Late();
		}
	}

	public override void NetworkInitializeIfDisabled()
	{
		NetworkInitialize___Early();
		NetworkInitialize__Late();
	}

	public override void Awake()
	{
		NetworkInitialize___Early();
		base.Awake();
		NetworkInitialize__Late();
	}
}
