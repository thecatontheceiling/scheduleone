using ScheduleOne.EntityFramework;
using ScheduleOne.ItemFramework;
using ScheduleOne.Property;
using ScheduleOne.Tiles;
using ScheduleOne.UI;
using UnityEngine;

namespace ScheduleOne.ObjectScripts;

public class LaunderingStation : GridItem
{
	[Header("References")]
	public LaunderingInterface Interface;

	[SerializeField]
	protected CashCounter CashCounter;

	private bool NetworkInitialize___EarlyScheduleOne_002EObjectScripts_002ELaunderingStationAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002EObjectScripts_002ELaunderingStationAssembly_002DCSharp_002Edll_Excuted;

	public override void InitializeGridItem(ItemInstance instance, Grid grid, Vector2 originCoordinate, int rotation, string GUID)
	{
		bool initialized = base.Initialized;
		base.InitializeGridItem(instance, grid, originCoordinate, rotation, GUID);
		if (!initialized)
		{
			Interface.Initialize(base.ParentProperty as Business);
		}
	}

	private void Update()
	{
		if (Interface != null && Interface.business != null)
		{
			CashCounter.IsOn = Interface.business.currentLaunderTotal > 0f;
		}
	}

	public override bool CanBeDestroyed(out string reason)
	{
		reason = string.Empty;
		return false;
	}

	public override void NetworkInitialize___Early()
	{
		if (!NetworkInitialize___EarlyScheduleOne_002EObjectScripts_002ELaunderingStationAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002EObjectScripts_002ELaunderingStationAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize___Early();
		}
	}

	public override void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002EObjectScripts_002ELaunderingStationAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002EObjectScripts_002ELaunderingStationAssembly_002DCSharp_002Edll_Excuted = true;
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
