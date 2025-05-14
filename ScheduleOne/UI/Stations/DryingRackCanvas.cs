using System;
using System.Collections.Generic;
using System.Linq;
using ScheduleOne.DevUtilities;
using ScheduleOne.GameTime;
using ScheduleOne.ItemFramework;
using ScheduleOne.ObjectScripts;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI.Items;
using ScheduleOne.UI.Stations.Drying_rack;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScheduleOne.UI.Stations;

public class DryingRackCanvas : Singleton<DryingRackCanvas>
{
	[Header("References")]
	public Canvas Canvas;

	public RectTransform Container;

	public ItemSlotUI InputSlotUI;

	public ItemSlotUI OutputSlotUI;

	public TextMeshProUGUI InstructionLabel;

	public TextMeshProUGUI CapacityLabel;

	public Button InsertButton;

	public RectTransform IndicatorContainer;

	public RectTransform[] IndicatorAlignments;

	[Header("Prefabs")]
	public DryingOperationUI IndicatorPrefab;

	private List<DryingOperationUI> operationUIs = new List<DryingOperationUI>();

	public bool isOpen { get; protected set; }

	public DryingRack Rack { get; protected set; }

	protected override void Awake()
	{
		base.Awake();
	}

	protected override void Start()
	{
		base.Start();
		SetIsOpen(null, open: false);
		TimeManager timeManager = NetworkSingleton<TimeManager>.Instance;
		timeManager.onMinutePass = (Action)Delegate.Combine(timeManager.onMinutePass, new Action(MinPass));
	}

	private void MinPass()
	{
		if (isOpen)
		{
			UpdateDryingOperations();
		}
	}

	protected virtual void Update()
	{
		if (isOpen)
		{
			UpdateUI();
		}
	}

	private void UpdateUI()
	{
		InsertButton.interactable = Rack.CanStartOperation();
		CapacityLabel.text = Rack.GetTotalDryingItems() + " / " + Rack.ItemCapacity;
		CapacityLabel.color = ((Rack.GetTotalDryingItems() >= Rack.ItemCapacity) ? ((Color)new Color32(byte.MaxValue, 50, 50, byte.MaxValue)) : Color.white);
	}

	private void UpdateDryingOperations()
	{
		foreach (DryingOperationUI operationUI in operationUIs)
		{
			RectTransform alignment = null;
			DryingOperation assignedOperation = operationUI.AssignedOperation;
			if (assignedOperation.StartQuality == EQuality.Trash)
			{
				alignment = IndicatorAlignments[0];
			}
			else if (assignedOperation.StartQuality == EQuality.Poor)
			{
				alignment = IndicatorAlignments[1];
			}
			else if (assignedOperation.StartQuality == EQuality.Standard)
			{
				alignment = IndicatorAlignments[2];
			}
			else if (assignedOperation.StartQuality == EQuality.Premium)
			{
				alignment = IndicatorAlignments[3];
			}
			else
			{
				Console.LogWarning("Alignment not found for quality: " + assignedOperation.StartQuality);
			}
			operationUI.SetAlignment(alignment);
		}
	}

	private void UpdateQuantities()
	{
		foreach (DryingOperationUI operationUI in operationUIs)
		{
			operationUI.RefreshQuantity();
		}
	}

	public void SetIsOpen(DryingRack rack, bool open)
	{
		isOpen = open;
		Canvas.enabled = open;
		Container.gameObject.SetActive(open);
		if (PlayerSingleton<PlayerCamera>.InstanceExists)
		{
			PlayerSingleton<PlayerCamera>.Instance.RemoveActiveUIElement(base.name);
		}
		if (open)
		{
			PlayerSingleton<PlayerCamera>.Instance.AddActiveUIElement(base.name);
			InputSlotUI.AssignSlot(rack.InputSlot);
			OutputSlotUI.AssignSlot(rack.OutputSlot);
			Singleton<InputPromptsCanvas>.Instance.LoadModule("exitonly");
			PlayerSingleton<PlayerInventory>.Instance.SetInventoryEnabled(enabled: true);
			PlayerSingleton<PlayerInventory>.Instance.SetEquippingEnabled(enabled: false);
			for (int i = 0; i < rack.DryingOperations.Count; i++)
			{
				CreateOperationUI(rack.DryingOperations[i]);
			}
			rack.onOperationStart = (Action<DryingOperation>)Delegate.Combine(rack.onOperationStart, new Action<DryingOperation>(CreateOperationUI));
			rack.onOperationComplete = (Action<DryingOperation>)Delegate.Combine(rack.onOperationComplete, new Action<DryingOperation>(DestroyOperationUI));
			rack.onOperationsChanged = (Action)Delegate.Combine(rack.onOperationsChanged, new Action(UpdateQuantities));
		}
		else
		{
			InputSlotUI.ClearSlot();
			OutputSlotUI.ClearSlot();
			Singleton<InputPromptsCanvas>.Instance.UnloadModule();
			if (Rack != null)
			{
				DryingRack rack2 = Rack;
				rack2.onOperationStart = (Action<DryingOperation>)Delegate.Remove(rack2.onOperationStart, new Action<DryingOperation>(CreateOperationUI));
				DryingRack rack3 = Rack;
				rack3.onOperationComplete = (Action<DryingOperation>)Delegate.Remove(rack3.onOperationComplete, new Action<DryingOperation>(DestroyOperationUI));
				DryingRack rack4 = Rack;
				rack4.onOperationsChanged = (Action)Delegate.Remove(rack4.onOperationsChanged, new Action(UpdateQuantities));
			}
			foreach (DryingOperationUI operationUI in operationUIs)
			{
				UnityEngine.Object.Destroy(operationUI.gameObject);
			}
			operationUIs.Clear();
		}
		Singleton<ItemUIManager>.Instance.SetDraggingEnabled(open);
		if (open)
		{
			List<ItemSlot> list = new List<ItemSlot>();
			list.AddRange(rack.InputSlots);
			list.Add(rack.OutputSlot);
			Singleton<ItemUIManager>.Instance.EnableQuickMove(PlayerSingleton<PlayerInventory>.Instance.GetAllInventorySlots(), list);
		}
		Rack = rack;
		if (open)
		{
			UpdateUI();
			MinPass();
		}
	}

	private void CreateOperationUI(DryingOperation operation)
	{
		DryingOperationUI dryingOperationUI = UnityEngine.Object.Instantiate(IndicatorPrefab, IndicatorContainer);
		dryingOperationUI.SetOperation(operation);
		operationUIs.Add(dryingOperationUI);
		UpdateDryingOperations();
	}

	private void DestroyOperationUI(DryingOperation operation)
	{
		DryingOperationUI dryingOperationUI = operationUIs.FirstOrDefault((DryingOperationUI x) => x.AssignedOperation == operation);
		if (dryingOperationUI != null)
		{
			operationUIs.Remove(dryingOperationUI);
			UnityEngine.Object.Destroy(dryingOperationUI.gameObject);
		}
	}

	public void Insert()
	{
		Rack.StartOperation();
	}
}
