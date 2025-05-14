using FishNet;
using ScheduleOne.DevUtilities;
using ScheduleOne.GameTime;
using ScheduleOne.Law;
using ScheduleOne.Police;
using UnityEngine;

namespace ScheduleOne.NPCs.Behaviour;

public class SentryBehaviour : Behaviour
{
	public const float BODY_SEARCH_CHANCE = 0.75f;

	public const int FLASHLIGHT_MIN_TIME = 1930;

	public int FLASHLIGHT_MAX_TIME = 500;

	public const string FLASHLIGHT_ASSET_PATH = "Tools/Flashlight/Flashlight_AvatarEquippable";

	public bool UseFlashlight = true;

	private bool flashlightEquipped;

	private PoliceOfficer officer;

	private bool NetworkInitialize___EarlyScheduleOne_002ENPCs_002EBehaviour_002ESentryBehaviourAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002ENPCs_002EBehaviour_002ESentryBehaviourAssembly_002DCSharp_002Edll_Excuted;

	public SentryLocation AssignedLocation { get; private set; }

	private Transform standPoint => AssignedLocation.StandPoints[AssignedLocation.AssignedOfficers.IndexOf(officer)];

	public override void Awake()
	{
		NetworkInitialize___Early();
		Awake_UserLogic_ScheduleOne_002ENPCs_002EBehaviour_002ESentryBehaviour_Assembly_002DCSharp_002Edll();
		NetworkInitialize__Late();
	}

	protected override void Begin()
	{
		base.Begin();
	}

	protected override void Resume()
	{
		base.Resume();
	}

	protected override void End()
	{
		base.End();
		if (flashlightEquipped)
		{
			SetFlashlightEquipped(equipped: false);
		}
	}

	protected override void Pause()
	{
		base.Pause();
		if (flashlightEquipped)
		{
			SetFlashlightEquipped(equipped: false);
		}
	}

	public override void Disable()
	{
		base.Disable();
		End();
	}

	public void AssignLocation(SentryLocation loc)
	{
		if (AssignedLocation != null)
		{
			UnassignLocation();
		}
		AssignedLocation = loc;
		AssignedLocation.AssignedOfficers.Add(officer);
	}

	public void UnassignLocation()
	{
		if (AssignedLocation != null)
		{
			AssignedLocation.AssignedOfficers.Remove(officer);
			AssignedLocation = null;
		}
	}

	public override void ActiveMinPass()
	{
		base.ActiveMinPass();
		if (!InstanceFinder.IsServer)
		{
			return;
		}
		if (NetworkSingleton<ScheduleOne.GameTime.TimeManager>.Instance.IsCurrentTimeWithinRange(1930, FLASHLIGHT_MAX_TIME))
		{
			if (UseFlashlight && !flashlightEquipped)
			{
				SetFlashlightEquipped(equipped: true);
			}
		}
		else if (flashlightEquipped)
		{
			SetFlashlightEquipped(equipped: false);
		}
		officer.BodySearchChance = 0.1f;
		if (base.Npc.Movement.IsMoving)
		{
			return;
		}
		if (Vector3.Distance(base.Npc.transform.position, standPoint.position) < 2f)
		{
			officer.BodySearchChance = 0.75f;
			if (!base.Npc.Movement.FaceDirectionInProgress)
			{
				base.Npc.Movement.FaceDirection(standPoint.forward);
			}
		}
		else if (base.Npc.Movement.CanMove())
		{
			base.Npc.Movement.SetDestination(standPoint.position);
		}
	}

	private void SetFlashlightEquipped(bool equipped)
	{
		flashlightEquipped = equipped;
		if (equipped)
		{
			base.Npc.SetEquippable_Networked(null, "Tools/Flashlight/Flashlight_AvatarEquippable");
		}
		else
		{
			base.Npc.SetEquippable_Networked(null, string.Empty);
		}
	}

	public override void NetworkInitialize___Early()
	{
		if (!NetworkInitialize___EarlyScheduleOne_002ENPCs_002EBehaviour_002ESentryBehaviourAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002ENPCs_002EBehaviour_002ESentryBehaviourAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize___Early();
		}
	}

	public override void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002ENPCs_002EBehaviour_002ESentryBehaviourAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002ENPCs_002EBehaviour_002ESentryBehaviourAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize__Late();
		}
	}

	public override void NetworkInitializeIfDisabled()
	{
		NetworkInitialize___Early();
		NetworkInitialize__Late();
	}

	protected virtual void Awake_UserLogic_ScheduleOne_002ENPCs_002EBehaviour_002ESentryBehaviour_Assembly_002DCSharp_002Edll()
	{
		base.Awake();
		officer = base.Npc as PoliceOfficer;
	}
}
