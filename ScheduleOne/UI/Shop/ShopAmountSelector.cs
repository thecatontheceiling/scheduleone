using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace ScheduleOne.UI.Shop;

public class ShopAmountSelector : MonoBehaviour
{
	[Header("References")]
	public RectTransform Container;

	public TMP_InputField InputField;

	public UnityEvent<int> onSubmitted;

	public bool IsOpen { get; private set; }

	public int SelectedAmount { get; private set; } = 1;

	private void Awake()
	{
		Container.gameObject.SetActive(value: false);
		InputField.onSubmit.AddListener(OnSubmitted);
		InputField.onValueChanged.AddListener(OnValueChanged);
	}

	public void Open()
	{
		Container.gameObject.SetActive(value: true);
		Container.SetAsLastSibling();
		InputField.text = string.Empty;
		InputField.Select();
		IsOpen = true;
	}

	public void Close()
	{
		Container.gameObject.SetActive(value: false);
		IsOpen = false;
	}

	private void OnSubmitted(string value)
	{
		if (IsOpen)
		{
			OnValueChanged(value);
			if (onSubmitted != null)
			{
				onSubmitted.Invoke(SelectedAmount);
			}
			Close();
		}
	}

	private void OnValueChanged(string value)
	{
		if (int.TryParse(value, out var result))
		{
			SelectedAmount = Mathf.Clamp(result, 1, 999);
			InputField.SetTextWithoutNotify(SelectedAmount.ToString());
		}
		else
		{
			SelectedAmount = 1;
			InputField.SetTextWithoutNotify(string.Empty);
		}
	}
}
