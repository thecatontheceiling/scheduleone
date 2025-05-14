using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ScheduleOne.Tools;

public class InputFieldAttachment : MonoBehaviour
{
	private void Awake()
	{
		InputField inputField = GetComponent<InputField>();
		if (inputField != null)
		{
			EventTrigger eventTrigger = inputField.gameObject.AddComponent<EventTrigger>();
			EventTrigger.Entry entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.Select;
			entry.callback.AddListener(delegate
			{
				EditStart(inputField.text);
			});
			eventTrigger.triggers.Add(entry);
			inputField.onEndEdit.AddListener(EndEdit);
		}
		TMP_InputField component = GetComponent<TMP_InputField>();
		if (component != null)
		{
			component.onSelect.AddListener(EditStart);
			component.onEndEdit.AddListener(EndEdit);
		}
	}

	private void EditStart(string newVal)
	{
		GameInput.IsTyping = true;
	}

	private void EndEdit(string newVal)
	{
		GameInput.IsTyping = false;
	}
}
