using System.Collections.Generic;
using ScheduleOne.ItemFramework;
using ScheduleOne.Management;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScheduleOne.UI.Management;

public class QualityFieldUI : MonoBehaviour
{
	[Header("References")]
	public TextMeshProUGUI FieldLabel;

	public Button[] QualityButtons;

	public List<QualityField> Fields { get; protected set; } = new List<QualityField>();

	public void Bind(List<QualityField> field)
	{
		Fields = new List<QualityField>();
		Fields.AddRange(field);
		Fields[Fields.Count - 1].onValueChanged.AddListener(Refresh);
		for (int i = 0; i < QualityButtons.Length; i++)
		{
			EQuality quality = (EQuality)i;
			QualityButtons[i].onClick.AddListener(delegate
			{
				ValueChanged(quality);
			});
		}
		Refresh(Fields[0].Value);
	}

	private void Refresh(EQuality value)
	{
		if (AreFieldsUniform())
		{
			EQuality value2 = Fields[0].Value;
			for (int i = 0; i < QualityButtons.Length; i++)
			{
				EQuality eQuality = (EQuality)i;
				QualityButtons[i].interactable = eQuality != value2;
			}
		}
		else
		{
			Button[] qualityButtons = QualityButtons;
			for (int j = 0; j < qualityButtons.Length; j++)
			{
				qualityButtons[j].interactable = true;
			}
		}
	}

	private bool AreFieldsUniform()
	{
		for (int i = 0; i < Fields.Count - 1; i++)
		{
			if (Fields[i].Value != Fields[i + 1].Value)
			{
				return false;
			}
		}
		return true;
	}

	public void ValueChanged(EQuality value)
	{
		for (int i = 0; i < Fields.Count; i++)
		{
			Fields[i].SetValue(value, network: true);
		}
	}
}
