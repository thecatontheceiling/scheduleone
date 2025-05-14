using System.Collections.Generic;
using ScheduleOne.UI.MainMenu;
using TMPro;
using UnityEngine;

namespace ScheduleOne;

public class CommandListScreen : MainMenuScreen
{
	public RectTransform CommandEntryContainer;

	public RectTransform CommandEntryPrefab;

	private List<RectTransform> commandEntries = new List<RectTransform>();

	private void Start()
	{
		if (commandEntries.Count == 0)
		{
			foreach (Console.ConsoleCommand command in Console.Commands)
			{
				RectTransform rectTransform = Object.Instantiate(CommandEntryPrefab, CommandEntryContainer);
				rectTransform.Find("Command").GetComponent<TextMeshProUGUI>().text = command.CommandWord;
				rectTransform.Find("Description").GetComponent<TextMeshProUGUI>().text = command.CommandDescription;
				rectTransform.Find("Example").GetComponent<TextMeshProUGUI>().text = command.ExampleUsage;
				commandEntries.Add(rectTransform);
			}
		}
		CommandEntryContainer.offsetMin = new Vector2(CommandEntryContainer.offsetMin.x, 0f);
		CommandEntryContainer.offsetMax = new Vector2(CommandEntryContainer.offsetMax.x, 0f);
	}
}
