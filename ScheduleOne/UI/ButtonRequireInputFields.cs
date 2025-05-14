using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScheduleOne.UI;

public class ButtonRequireInputFields : MonoBehaviour
{
	[Serializable]
	public class Input
	{
		public TMP_InputField InputField;

		public RectTransform ErrorMessage;
	}

	public List<Input> Inputs;

	public TMP_Dropdown Dropdown;

	public Button Button;

	public void Update()
	{
		Button.interactable = true;
		if (Dropdown != null && Dropdown.value == 0)
		{
			Button.interactable = false;
		}
		foreach (Input input in Inputs)
		{
			if (string.IsNullOrEmpty(input.InputField.text))
			{
				input.ErrorMessage.gameObject.SetActive(value: true);
				Button.interactable = false;
			}
			else
			{
				input.ErrorMessage.gameObject.SetActive(value: false);
			}
		}
	}
}
