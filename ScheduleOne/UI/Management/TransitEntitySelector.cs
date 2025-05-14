using System;
using System.Collections.Generic;
using ScheduleOne.DevUtilities;
using ScheduleOne.Management;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Tools;
using UnityEngine;

namespace ScheduleOne.UI.Management;

public class TransitEntitySelector : MonoBehaviour
{
	public delegate bool ObjectFilter(ITransitEntity obj, out string reason);

	public const float SELECTION_RANGE = 5f;

	[Header("Settings")]
	public LayerMask DetectionMask;

	public Color HoverOutlineColor;

	public Color SelectOutlineColor;

	private int maxSelectedObjects;

	private List<ITransitEntity> selectedObjects = new List<ITransitEntity>();

	private List<Type> typeRequirements = new List<Type>();

	private ObjectFilter objectFilter;

	private Action<List<ITransitEntity>> callback;

	private ITransitEntity hoveredObj;

	private ITransitEntity highlightedObj;

	private string selectionTitle = "";

	private bool changesMade;

	private List<Transform> transitSources = new List<Transform>();

	private List<TransitLineVisuals> transitLines = new List<TransitLineVisuals>();

	private bool selectDestination = true;

	public bool IsOpen { get; protected set; }

	private void Start()
	{
		GameInput.RegisterExitListener(Exit, 12);
		Singleton<ManagementClipboard>.Instance.onClipboardUnequipped.AddListener(ClipboardClosed);
	}

	public virtual void Open(string _selectionTitle, string instruction, int _maxSelectedObjects, List<ITransitEntity> _selectedObjects, List<Type> _typeRequirements, ObjectFilter _objectFilter, Action<List<ITransitEntity>> _callback, List<Transform> transitLineSources = null, bool selectingDestination = true)
	{
		IsOpen = true;
		changesMade = false;
		selectDestination = selectingDestination;
		selectionTitle = _selectionTitle;
		if (instruction != string.Empty)
		{
			Singleton<HUD>.Instance.ShowTopScreenText(instruction);
		}
		maxSelectedObjects = _maxSelectedObjects;
		selectedObjects = new List<ITransitEntity>();
		selectedObjects.AddRange(_selectedObjects);
		for (int i = 0; i < selectedObjects.Count; i++)
		{
			SetSelectionOutline(selectedObjects[i], on: true);
		}
		objectFilter = _objectFilter;
		typeRequirements = _typeRequirements;
		callback = _callback;
		UpdateInstructions();
		Singleton<ManagementInterface>.Instance.EquippedClipboard.OverrideClipboardText(selectionTitle);
		Singleton<ManagementClipboard>.Instance.Close(preserveState: true);
		if (maxSelectedObjects == 1)
		{
			Singleton<InputPromptsCanvas>.Instance.LoadModule("objectselector");
		}
		else
		{
			Singleton<InputPromptsCanvas>.Instance.LoadModule("objectselector_multi");
		}
		if (transitLineSources != null)
		{
			transitSources.Clear();
			transitSources.AddRange(transitLineSources);
			for (int j = 0; j < transitSources.Count; j++)
			{
				TransitLineVisuals item = UnityEngine.Object.Instantiate(Singleton<ManagementWorldspaceCanvas>.Instance.TransitRouteVisualsPrefab, NetworkSingleton<GameManager>.Instance.Temp);
				transitLines.Add(item);
			}
			UpdateTransitLines();
		}
	}

	private void UpdateTransitLines()
	{
		float num = 1.5f;
		Vector3 vector = PlayerSingleton<PlayerCamera>.Instance.transform.position + PlayerSingleton<PlayerCamera>.Instance.transform.forward * num;
		if (PlayerSingleton<PlayerCamera>.Instance.LookRaycast(num, out var hit, DetectionMask, includeTriggers: false))
		{
			vector = hit.point;
		}
		for (int i = 0; i < transitSources.Count; i++)
		{
			if (selectDestination)
			{
				transitLines[i].SetSourcePosition(transitSources[i].position);
				transitLines[i].SetDestinationPosition(vector);
			}
			else
			{
				transitLines[i].SetSourcePosition(vector);
				transitLines[i].SetDestinationPosition(transitSources[i].position);
			}
		}
	}

	public virtual void Close(bool returnToClipboard, bool pushChanges)
	{
		IsOpen = false;
		if (Singleton<InputPromptsCanvas>.Instance.currentModuleLabel == "npcselector" || Singleton<InputPromptsCanvas>.Instance.currentModuleLabel == "objectselector_multi")
		{
			Singleton<InputPromptsCanvas>.Instance.UnloadModule();
		}
		for (int i = 0; i < selectedObjects.Count; i++)
		{
			SetSelectionOutline(selectedObjects[i], on: false);
		}
		Singleton<HUD>.Instance.HideTopScreenText();
		if (returnToClipboard)
		{
			Singleton<ManagementInterface>.Instance.EquippedClipboard.EndOverride();
			Singleton<ManagementClipboard>.Instance.Open(Singleton<ManagementInterface>.Instance.Configurables, Singleton<ManagementInterface>.Instance.EquippedClipboard);
		}
		for (int j = 0; j < transitLines.Count; j++)
		{
			UnityEngine.Object.Destroy(transitLines[j].gameObject);
		}
		transitLines.Clear();
		transitSources.Clear();
		objectFilter = null;
		if (pushChanges)
		{
			callback(selectedObjects);
		}
	}

	private void Update()
	{
		if (!IsOpen)
		{
			return;
		}
		hoveredObj = GetHoveredObject();
		string reason = string.Empty;
		if (hoveredObj != null && IsObjectTypeValid(hoveredObj, out reason))
		{
			if (hoveredObj != highlightedObj && !selectedObjects.Contains(hoveredObj))
			{
				if (highlightedObj != null)
				{
					if (selectedObjects.Contains(highlightedObj))
					{
						highlightedObj.ShowOutline(SelectOutlineColor);
					}
					else
					{
						highlightedObj.HideOutline();
					}
					highlightedObj = null;
				}
				highlightedObj = hoveredObj;
				hoveredObj.ShowOutline(HoverOutlineColor);
			}
		}
		else
		{
			Singleton<HUD>.Instance.CrosshairText.Show(reason, new Color32(byte.MaxValue, 125, 125, byte.MaxValue));
			if (highlightedObj != null)
			{
				if (selectedObjects.Contains(highlightedObj))
				{
					highlightedObj.ShowOutline(SelectOutlineColor);
				}
				else
				{
					highlightedObj.HideOutline();
				}
				highlightedObj = null;
			}
		}
		UpdateInstructions();
		if (GameInput.GetButtonDown(GameInput.ButtonCode.PrimaryClick) && hoveredObj != null && IsObjectTypeValid(hoveredObj, out reason))
		{
			ObjectClicked(hoveredObj);
		}
		if (GameInput.GetButtonDown(GameInput.ButtonCode.Submit) && maxSelectedObjects > 1)
		{
			Close(returnToClipboard: true, pushChanges: true);
		}
	}

	private void LateUpdate()
	{
		if (IsOpen)
		{
			UpdateTransitLines();
		}
	}

	private void UpdateInstructions()
	{
		string text = selectionTitle;
		if (maxSelectedObjects > 1)
		{
			text = text + " (" + selectedObjects.Count + "/" + maxSelectedObjects + ")";
		}
		Singleton<HUD>.Instance.ShowTopScreenText(text);
	}

	private ITransitEntity GetHoveredObject()
	{
		if (PlayerSingleton<PlayerCamera>.Instance.LookRaycast(5f, out var hit, DetectionMask, includeTriggers: false, 0.1f))
		{
			return hit.collider.GetComponentInParent<ITransitEntity>();
		}
		return null;
	}

	public bool IsObjectTypeValid(ITransitEntity obj, out string reason)
	{
		reason = string.Empty;
		if (typeRequirements.Count > 0 && !typeRequirements.Contains(obj.GetType()))
		{
			bool flag = false;
			for (int i = 0; i < typeRequirements.Count; i++)
			{
				if (obj.GetType().IsAssignableFrom(typeRequirements[i]))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				reason = "Does not match type requirement";
				return false;
			}
		}
		if (objectFilter != null && !objectFilter(obj, out var reason2))
		{
			reason = reason2;
			return false;
		}
		return true;
	}

	public void ObjectClicked(ITransitEntity obj)
	{
		if (!IsObjectTypeValid(obj, out var _))
		{
			return;
		}
		changesMade = true;
		if (!selectedObjects.Contains(obj))
		{
			if (selectedObjects.Count < maxSelectedObjects)
			{
				selectedObjects.Add(obj);
				SetSelectionOutline(obj, on: true);
			}
		}
		else if (maxSelectedObjects > 1)
		{
			selectedObjects.Remove(obj);
			SetSelectionOutline(obj, on: false);
		}
		if (maxSelectedObjects == 1 || !GameInput.GetButton(GameInput.ButtonCode.Sprint))
		{
			Close(returnToClipboard: true, pushChanges: true);
		}
	}

	private void SetSelectionOutline(ITransitEntity obj, bool on)
	{
		if (obj != null)
		{
			if (on)
			{
				obj.ShowOutline(SelectOutlineColor);
			}
			else
			{
				obj.HideOutline();
			}
		}
	}

	private void ClipboardClosed()
	{
		Close(returnToClipboard: false, pushChanges: false);
	}

	private void Exit(ExitAction exitAction)
	{
		if (IsOpen && !exitAction.Used && exitAction.exitType == ExitType.Escape)
		{
			exitAction.Used = true;
			Close(returnToClipboard: true, changesMade);
		}
	}
}
