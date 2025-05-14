using System.Collections;
using System.Collections.Generic;
using ScheduleOne.DevUtilities;
using ScheduleOne.Money;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI.Compass;
using ScheduleOne.Vehicles;
using ScheduleOne.Vehicles.Modification;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ScheduleOne.UI;

public class VehicleModMenu : Singleton<VehicleModMenu>
{
	public static float repaintCost = 100f;

	[Header("UI References")]
	[SerializeField]
	protected Canvas canvas;

	[SerializeField]
	protected RectTransform buttonContainer;

	[SerializeField]
	protected RectTransform tempIndicator;

	[SerializeField]
	protected RectTransform permIndicator;

	[SerializeField]
	protected Button confirmButton_Online;

	[SerializeField]
	protected TextMeshProUGUI confirmText_Online;

	[Header("References")]
	public Transform CameraPosition;

	public Transform VehiclePosition;

	[Header("Prefabs")]
	[SerializeField]
	protected GameObject buttonPrefab;

	public UnityEvent onPaintPurchased;

	protected LandVehicle currentVehicle;

	protected List<RectTransform> colorButtons = new List<RectTransform>();

	protected Dictionary<EVehicleColor, RectTransform> colorToButton = new Dictionary<EVehicleColor, RectTransform>();

	protected EVehicleColor selectedColor = EVehicleColor.White;

	private Coroutine openCloseRoutine;

	public bool IsOpen { get; private set; }

	protected override void Awake()
	{
		base.Awake();
		GameInput.RegisterExitListener(Exit, 1);
	}

	protected override void Start()
	{
		base.Start();
		confirmText_Online.text = "Confirm (" + MoneyManager.FormatAmount(repaintCost, showDecimals: false, includeColor: true) + ")";
		for (int i = 0; i < Singleton<VehicleColors>.Instance.colorLibrary.Count; i++)
		{
			RectTransform component = Object.Instantiate(buttonPrefab, buttonContainer).GetComponent<RectTransform>();
			component.anchoredPosition = new Vector2((0.5f + (float)colorButtons.Count) * component.sizeDelta.x, component.anchoredPosition.y);
			component.Find("Image").GetComponent<Image>().color = Singleton<VehicleColors>.Instance.colorLibrary[i].UIColor;
			EVehicleColor c = Singleton<VehicleColors>.Instance.colorLibrary[i].color;
			colorButtons.Add(component);
			colorToButton.Add(c, component);
			component.GetComponent<Button>().onClick.AddListener(delegate
			{
				ColorClicked(c);
			});
		}
	}

	private void Exit(ExitAction action)
	{
		if (!action.Used && IsOpen && openCloseRoutine == null && action.exitType == ExitType.Escape)
		{
			action.Used = true;
			Close();
		}
	}

	protected virtual void Update()
	{
		if (IsOpen)
		{
			UpdateConfirmButton();
		}
	}

	public void Open(LandVehicle vehicle)
	{
		currentVehicle = vehicle;
		selectedColor = vehicle.OwnedColor;
		PlayerSingleton<PlayerCamera>.Instance.AddActiveUIElement(base.name);
		Singleton<CompassManager>.Instance.SetVisible(visible: false);
		openCloseRoutine = StartCoroutine(Close());
		IEnumerator Close()
		{
			Singleton<BlackOverlay>.Instance.Open();
			yield return new WaitForSeconds(0.6f);
			IsOpen = true;
			canvas.enabled = true;
			currentVehicle.AlignTo(VehiclePosition, EParkingAlignment.RearToKerb, network: true);
			RefreshSelectionIndicator();
			UpdateConfirmButton();
			PlayerSingleton<PlayerInventory>.Instance.SetInventoryEnabled(enabled: false);
			PlayerSingleton<PlayerCamera>.Instance.FreeMouse();
			PlayerSingleton<PlayerCamera>.Instance.OverrideTransform(CameraPosition.position, CameraPosition.rotation, 0f);
			PlayerSingleton<PlayerCamera>.Instance.OverrideFOV(60f, 0f);
			Singleton<InputPromptsCanvas>.Instance.LoadModule("exitonly");
			Singleton<BlackOverlay>.Instance.Close();
			openCloseRoutine = null;
		}
	}

	public void Close()
	{
		if (currentVehicle != null)
		{
			currentVehicle.ApplyOwnedColor();
		}
		openCloseRoutine = StartCoroutine(Close());
		IEnumerator Close()
		{
			Singleton<BlackOverlay>.Instance.Open();
			yield return new WaitForSeconds(0.6f);
			PlayerSingleton<PlayerCamera>.Instance.RemoveActiveUIElement(base.name);
			currentVehicle = null;
			IsOpen = false;
			canvas.enabled = false;
			Singleton<CompassManager>.Instance.SetVisible(visible: true);
			PlayerSingleton<PlayerCamera>.Instance.SetCanLook(c: true);
			PlayerSingleton<PlayerCamera>.Instance.LockMouse();
			PlayerSingleton<PlayerCamera>.Instance.StopTransformOverride(0f);
			PlayerSingleton<PlayerCamera>.Instance.StopFOVOverride(0f);
			PlayerSingleton<PlayerMovement>.Instance.canMove = true;
			PlayerSingleton<PlayerInventory>.Instance.SetInventoryEnabled(enabled: true);
			Singleton<InputPromptsCanvas>.Instance.UnloadModule();
			Singleton<BlackOverlay>.Instance.Close();
			openCloseRoutine = null;
		}
	}

	public void ColorClicked(EVehicleColor col)
	{
		selectedColor = col;
		currentVehicle.ApplyColor(col);
		RefreshSelectionIndicator();
		UpdateConfirmButton();
	}

	private void UpdateConfirmButton()
	{
		bool flag = NetworkSingleton<MoneyManager>.Instance.SyncAccessor_onlineBalance >= repaintCost;
		confirmButton_Online.interactable = flag && selectedColor != currentVehicle.OwnedColor;
	}

	private void RefreshSelectionIndicator()
	{
		tempIndicator.position = colorToButton[selectedColor].position;
		permIndicator.position = colorToButton[currentVehicle.OwnedColor].position;
	}

	public void ConfirmButtonClicked()
	{
		NetworkSingleton<MoneyManager>.Instance.CreateOnlineTransaction("Vehicle repaint", 0f - repaintCost, 1f, string.Empty);
		NetworkSingleton<MoneyManager>.Instance.CashSound.Play();
		currentVehicle.SendOwnedColor(selectedColor);
		RefreshSelectionIndicator();
		if (onPaintPurchased != null)
		{
			onPaintPurchased.Invoke();
		}
		Close();
	}
}
