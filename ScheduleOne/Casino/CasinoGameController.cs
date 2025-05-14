using System;
using FishNet.Object;
using ScheduleOne.DevUtilities;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI.Compass;
using UnityEngine;

namespace ScheduleOne.Casino;

public class CasinoGameController : NetworkBehaviour
{
	public const float FOV = 65f;

	public const float CAMERA_LERP_TIME = 0.2f;

	[Header("References")]
	public CasinoGamePlayers Players;

	public CasinoGameInteraction Interaction;

	public Transform[] DefaultCameraTransforms;

	protected Transform localDefaultCameraTransform;

	private bool NetworkInitialize___EarlyScheduleOne_002ECasino_002ECasinoGameControllerAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002ECasino_002ECasinoGameControllerAssembly_002DCSharp_002Edll_Excuted;

	public bool IsOpen { get; private set; }

	public CasinoGamePlayerData LocalPlayerData => Players.GetPlayerData();

	public virtual void Awake()
	{
		NetworkInitialize___Early();
		Awake_UserLogic_ScheduleOne_002ECasino_002ECasinoGameController_Assembly_002DCSharp_002Edll();
		NetworkInitialize__Late();
	}

	protected virtual void OnLocalPlayerRequestJoin(Player player)
	{
		Open();
	}

	protected virtual void Exit(ExitAction action)
	{
		if (!action.Used && IsOpen && action.exitType == ExitType.Escape)
		{
			Close();
			action.Used = true;
		}
	}

	protected virtual void Update()
	{
	}

	protected virtual void FixedUpdate()
	{
	}

	protected virtual void Open()
	{
		IsOpen = true;
		Players.AddPlayer(Player.Local);
		PlayerSingleton<PlayerCamera>.Instance.SetCanLook(c: false);
		PlayerSingleton<PlayerCamera>.Instance.AddActiveUIElement(base.name);
		localDefaultCameraTransform = DefaultCameraTransforms[Players.GetPlayerIndex(Player.Local)];
		PlayerSingleton<PlayerCamera>.Instance.OverrideTransform(localDefaultCameraTransform.position, localDefaultCameraTransform.rotation, 0.2f);
		PlayerSingleton<PlayerCamera>.Instance.OverrideFOV(65f, 0.2f);
		PlayerSingleton<PlayerCamera>.Instance.FreeMouse();
		PlayerSingleton<PlayerMovement>.Instance.canMove = false;
		PlayerSingleton<PlayerInventory>.Instance.SetEquippingEnabled(enabled: false);
		Singleton<CompassManager>.Instance.SetVisible(visible: false);
	}

	protected virtual void Close()
	{
		IsOpen = false;
		Players.RemovePlayer(Player.Local);
		PlayerSingleton<PlayerCamera>.Instance.RemoveActiveUIElement(base.name);
		PlayerSingleton<PlayerCamera>.Instance.StopTransformOverride(0.2f);
		PlayerSingleton<PlayerCamera>.Instance.StopFOVOverride(0.2f);
		PlayerSingleton<PlayerCamera>.Instance.LockMouse();
		PlayerSingleton<PlayerMovement>.Instance.canMove = true;
		PlayerSingleton<PlayerInventory>.Instance.SetEquippingEnabled(enabled: true);
		Singleton<CompassManager>.Instance.SetVisible(visible: true);
	}

	public virtual void NetworkInitialize___Early()
	{
		if (!NetworkInitialize___EarlyScheduleOne_002ECasino_002ECasinoGameControllerAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002ECasino_002ECasinoGameControllerAssembly_002DCSharp_002Edll_Excuted = true;
		}
	}

	public virtual void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002ECasino_002ECasinoGameControllerAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002ECasino_002ECasinoGameControllerAssembly_002DCSharp_002Edll_Excuted = true;
		}
	}

	public override void NetworkInitializeIfDisabled()
	{
		NetworkInitialize___Early();
		NetworkInitialize__Late();
	}

	protected virtual void Awake_UserLogic_ScheduleOne_002ECasino_002ECasinoGameController_Assembly_002DCSharp_002Edll()
	{
		CasinoGameInteraction interaction = Interaction;
		interaction.onLocalPlayerRequestJoin = (Action<Player>)Delegate.Combine(interaction.onLocalPlayerRequestJoin, new Action<Player>(OnLocalPlayerRequestJoin));
		GameInput.RegisterExitListener(Exit);
	}
}
