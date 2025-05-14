using ScheduleOne.ItemFramework;
using ScheduleOne.Management;
using ScheduleOne.ObjectScripts;
using UnityEngine.UI;

namespace ScheduleOne.UI.Management;

public class DryingRackUIElement : WorldspaceUIElement
{
	public Image TargetQualityIcon;

	public DryingRack AssignedRack { get; protected set; }

	public void Initialize(DryingRack rack)
	{
		AssignedRack = rack;
		AssignedRack.Configuration.onChanged.AddListener(RefreshUI);
		RefreshUI();
		base.gameObject.SetActive(value: false);
	}

	protected virtual void RefreshUI()
	{
		DryingRackConfiguration dryingRackConfiguration = AssignedRack.Configuration as DryingRackConfiguration;
		EQuality value = dryingRackConfiguration.TargetQuality.Value;
		TargetQualityIcon.color = ItemQuality.GetColor(value);
		SetAssignedNPC(dryingRackConfiguration.AssignedBotanist.SelectedNPC);
	}
}
