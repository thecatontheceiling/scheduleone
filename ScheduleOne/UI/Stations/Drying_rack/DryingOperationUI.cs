using ScheduleOne.DevUtilities;
using ScheduleOne.ObjectScripts;
using ScheduleOne.UI.Tooltips;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScheduleOne.UI.Stations.Drying_rack;

public class DryingOperationUI : MonoBehaviour
{
	[Header("References")]
	public RectTransform Rect;

	public Image Icon;

	public TextMeshProUGUI QuantityLabel;

	public Button Button;

	public Tooltip Tooltip;

	public DryingOperation AssignedOperation { get; protected set; }

	public RectTransform Alignment { get; private set; }

	public void SetOperation(DryingOperation operation)
	{
		AssignedOperation = operation;
		Icon.sprite = Registry.GetItem(operation.ItemID).Icon;
		RefreshQuantity();
		UpdatePosition();
	}

	public void SetAlignment(RectTransform alignment)
	{
		Alignment = alignment;
		base.transform.SetParent(alignment);
		UpdatePosition();
	}

	public void RefreshQuantity()
	{
		QuantityLabel.text = AssignedOperation.Quantity + "x";
	}

	public void Start()
	{
		Button.onClick.AddListener(delegate
		{
			Clicked();
		});
	}

	public void UpdatePosition()
	{
		float t = Mathf.Clamp01((float)AssignedOperation.Time / 720f);
		int num = Mathf.Clamp(720 - AssignedOperation.Time, 0, 720);
		int num2 = num / 60;
		int num3 = num % 60;
		Tooltip.text = num2 + "h " + num3 + "m until next tier";
		float num4 = -62.5f;
		float b = 0f - num4;
		Rect.anchoredPosition = new Vector2(Mathf.Lerp(num4, b, t), 0f);
	}

	private void Clicked()
	{
		Singleton<DryingRackCanvas>.Instance.Rack.TryEndOperation(Singleton<DryingRackCanvas>.Instance.Rack.DryingOperations.IndexOf(AssignedOperation), allowSplitting: true, AssignedOperation.GetQuality(), Random.Range(int.MinValue, int.MaxValue));
	}
}
