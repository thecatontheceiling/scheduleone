using System.Collections.Generic;
using ScheduleOne.DevUtilities;
using ScheduleOne.UI.MainMenu;
using UnityEngine;

namespace ScheduleOne.UI.Settings;

public class MonitorDropdown : SettingsDropdown
{
	protected override void Awake()
	{
		base.Awake();
		Display[] displays = Display.displays;
		for (int i = 0; i < displays.Length; i++)
		{
			AddOption("Monitor " + (i + 1));
		}
	}

	protected virtual void OnEnable()
	{
		Display[] displays = Display.displays;
		for (int i = 0; i < displays.Length; i++)
		{
			_ = displays[i].active;
		}
		dropdown.SetValueWithoutNotify(Mathf.Clamp(GetCurrentDisplayNumber(), 0, dropdown.options.Count - 1));
	}

	protected override void OnValueChanged(int value)
	{
		base.OnValueChanged(value);
		Singleton<ScheduleOne.DevUtilities.Settings>.Instance.UnappliedDisplaySettings.ActiveDisplayIndex = value;
		GetComponentInParent<SettingsScreen>().DisplayChanged();
	}

	public static int GetCurrentDisplayNumber()
	{
		List<DisplayInfo> list = new List<DisplayInfo>();
		Screen.GetDisplayLayout(list);
		return list.IndexOf(Screen.mainWindowDisplayInfo);
	}
}
