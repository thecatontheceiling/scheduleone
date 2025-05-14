using FishNet;
using ScheduleOne.DevUtilities;
using UnityEngine;
using UnityEngine.UI;

namespace ScheduleOne.UI.Settings;

public class GameSettingsWindow : MonoBehaviour
{
	public Toggle ConsoleToggle;

	public GameObject Blocker;

	private void Awake()
	{
		ConsoleToggle.onValueChanged.AddListener(ConsoleToggled);
	}

	public void Start()
	{
		ApplySettings(NetworkSingleton<GameManager>.Instance.Settings);
		Blocker.SetActive(!InstanceFinder.IsServer);
	}

	public void ApplySettings(GameSettings settings)
	{
		ConsoleToggle.SetIsOnWithoutNotify(settings.ConsoleEnabled);
	}

	private void ConsoleToggled(bool value)
	{
		NetworkSingleton<GameManager>.Instance.Settings.ConsoleEnabled = value;
	}
}
