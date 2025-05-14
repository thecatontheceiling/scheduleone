using EasyButtons;
using ScheduleOne.DevUtilities;
using ScheduleOne.PlayerScripts;
using Steamworks;
using UnityEngine;

namespace ScheduleOne.UI;

public class DemoEndScreen : MonoBehaviour
{
	[Header("References")]
	public Canvas Canvas;

	public RectTransform Container;

	public bool IsOpen { get; private set; }

	public void Awake()
	{
		GameInput.RegisterExitListener(Exit, 4);
		Canvas.enabled = false;
		Container.gameObject.SetActive(value: false);
	}

	private void OnDestroy()
	{
		GameInput.DeregisterExitListener(Exit);
	}

	[Button]
	public void Open()
	{
	}

	private void Update()
	{
		if (IsOpen)
		{
			PlayerSingleton<PlayerCamera>.Instance.SetCanLook(c: false);
		}
	}

	public void Close()
	{
		IsOpen = false;
		Canvas.enabled = false;
		Container.gameObject.SetActive(value: false);
		PlayerSingleton<PlayerCamera>.Instance.SetDoFActive(active: false, 0.25f);
		PlayerSingleton<PlayerCamera>.Instance.RemoveActiveUIElement(base.name);
		PlayerSingleton<PlayerCamera>.Instance.LockMouse();
		Singleton<HUD>.Instance.SetCrosshairVisible(vis: true);
		PlayerSingleton<PlayerCamera>.Instance.SetCanLook(c: true);
		PlayerSingleton<PlayerMovement>.Instance.canMove = true;
		PlayerSingleton<PlayerInventory>.Instance.SetInventoryEnabled(enabled: true);
		Singleton<InputPromptsCanvas>.Instance.UnloadModule();
	}

	private void Exit(ExitAction action)
	{
		if (IsOpen && !action.Used && action.exitType == ExitType.Escape)
		{
			action.Used = true;
			Close();
		}
	}

	public void LinkClicked()
	{
		if (SteamManager.Initialized)
		{
			SteamFriends.ActivateGameOverlayToStore(new AppId_t(3164500u), EOverlayToStoreFlag.k_EOverlayToStoreFlag_None);
		}
	}
}
