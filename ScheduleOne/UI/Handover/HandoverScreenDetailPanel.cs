using ScheduleOne.Economy;
using ScheduleOne.ItemFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScheduleOne.UI.Handover;

public class HandoverScreenDetailPanel : MonoBehaviour
{
	public LayoutGroup LayoutGroup;

	public RectTransform Container;

	public TextMeshProUGUI NameLabel;

	public RectTransform RelationshipContainer;

	public Scrollbar RelationshipScrollbar;

	public RectTransform AddictionContainer;

	public Scrollbar AdditionScrollbar;

	public Image StandardsStar;

	public TextMeshProUGUI StandardsLabel;

	public TextMeshProUGUI EffectsLabel;

	public void Open(Customer customer)
	{
		NameLabel.text = customer.NPC.fullName;
		if (customer.NPC.RelationData.Unlocked)
		{
			RelationshipContainer.gameObject.SetActive(value: true);
			RelationshipScrollbar.value = customer.NPC.RelationData.NormalizedRelationDelta;
			AddictionContainer.gameObject.SetActive(value: true);
			AdditionScrollbar.value = customer.CurrentAddiction;
		}
		else
		{
			RelationshipContainer.gameObject.SetActive(value: false);
			AddictionContainer.gameObject.SetActive(value: false);
		}
		StandardsStar.color = ItemQuality.GetColor(customer.CustomerData.Standards.GetCorrespondingQuality());
		StandardsLabel.text = customer.CustomerData.Standards.GetName();
		StandardsLabel.color = StandardsStar.color;
		EffectsLabel.text = string.Empty;
		for (int i = 0; i < customer.CustomerData.PreferredProperties.Count; i++)
		{
			if (i > 0)
			{
				EffectsLabel.text += "\n";
			}
			string text = "<color=#" + ColorUtility.ToHtmlStringRGBA(customer.CustomerData.PreferredProperties[i].LabelColor) + ">•  " + customer.CustomerData.PreferredProperties[i].Name + "</color>";
			EffectsLabel.text += text;
		}
		base.gameObject.SetActive(value: true);
		LayoutGroup.CalculateLayoutInputHorizontal();
		LayoutGroup.CalculateLayoutInputVertical();
		LayoutRebuilder.ForceRebuildLayoutImmediate(LayoutGroup.GetComponent<RectTransform>());
		LayoutGroup.GetComponent<ContentSizeFitter>().SetLayoutVertical();
		Container.anchoredPosition = new Vector2(0f, (0f - Container.sizeDelta.y) / 2f);
	}

	public void Close()
	{
		base.gameObject.SetActive(value: false);
	}
}
