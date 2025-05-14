using ScheduleOne.DevUtilities;
using ScheduleOne.Economy;
using ScheduleOne.ItemFramework;
using ScheduleOne.Map;
using ScheduleOne.NPCs;
using ScheduleOne.NPCs.Relation;
using ScheduleOne.UI.Phone.Map;
using ScheduleOne.Variables;
using UnityEngine;
using UnityEngine.UI;

namespace ScheduleOne.UI.Phone.ContactsApp;

public class ContactsDetailPanel : MonoBehaviour
{
	[Header("Settings")]
	public Color DependenceColor_Min;

	public Color DependenceColor_Max;

	[Header("References")]
	public VerticalLayoutGroup LayoutGroup;

	public Text NameLabel;

	public Text TypeLabel;

	public Text UnlockHintLabel;

	public RectTransform RelationshipContainer;

	public Scrollbar RelationshipScrollbar;

	public Text RelationshipLabel;

	public RectTransform AddictionContainer;

	public Scrollbar AddictionScrollbar;

	public Text AddictionLabel;

	public RectTransform PropertiesContainer;

	public Text PropertiesLabel;

	public Button ShowOnMapButton;

	public RectTransform StandardsContainer;

	public Image StandardsStar;

	public Text StandardsLabel;

	private POI poi;

	public NPC SelectedNPC { get; protected set; }

	public void Open(NPC npc)
	{
		SelectedNPC = npc;
		if (npc == null)
		{
			return;
		}
		bool unlocked = npc.RelationData.Unlocked;
		bool flag = unlocked;
		if (!npc.RelationData.Unlocked && npc.RelationData.IsMutuallyKnown() && NetworkSingleton<VariableDatabase>.InstanceExists)
		{
			NetworkSingleton<VariableDatabase>.Instance.SetVariableValue("SelectedPotentialCustomer", true.ToString(), network: false);
		}
		poi = null;
		UnlockHintLabel.gameObject.SetActive(value: false);
		if (npc is Supplier)
		{
			TypeLabel.text = "Supplier";
			TypeLabel.color = Supplier.SupplierLabelColor;
			if (!unlocked)
			{
				UnlockHintLabel.text = "Unlock this supplier by reaching 'friendly' with one of their connections.";
				UnlockHintLabel.gameObject.SetActive(value: true);
			}
			if (unlocked)
			{
				poi = (npc as Supplier).Stash.StashPoI;
			}
		}
		else if (npc is Dealer)
		{
			TypeLabel.text = "Dealer";
			TypeLabel.color = Dealer.DealerLabelColor;
			Dealer dealer = npc as Dealer;
			if (!(npc as Dealer).HasBeenRecommended)
			{
				UnlockHintLabel.text = "Unlock this dealer by reaching 'friendly' with one of their connections.";
				UnlockHintLabel.gameObject.SetActive(value: true);
			}
			else if (!dealer.IsRecruited)
			{
				UnlockHintLabel.text = "This dealer is ready to be hired. Go to them and pay their signing free to recruit them.";
				UnlockHintLabel.gameObject.SetActive(value: true);
			}
			if (dealer.IsRecruited)
			{
				poi = dealer.dealerPoI;
			}
			else if (dealer.HasBeenRecommended)
			{
				poi = dealer.potentialDealerPoI;
			}
		}
		else
		{
			TypeLabel.text = "Customer";
			TypeLabel.color = Color.white;
			if (npc.RelationData.IsMutuallyKnown())
			{
				flag = true;
				if (!unlocked)
				{
					if (!GameManager.IS_TUTORIAL)
					{
						poi = npc.GetComponent<Customer>().potentialCustomerPoI;
					}
					UnlockHintLabel.text = "Unlock this customer by giving them a free sample. Use your map to see their approximate location.";
					UnlockHintLabel.gameObject.SetActive(value: true);
				}
			}
		}
		if (flag)
		{
			NameLabel.text = npc.fullName;
		}
		else
		{
			NameLabel.text = "???";
		}
		ShowOnMapButton.gameObject.SetActive(poi != null);
		if (npc.RelationData.Unlocked)
		{
			RelationshipScrollbar.value = npc.RelationData.RelationDelta / 5f;
			RelationshipLabel.text = "<color=#" + ColorUtility.ToHtmlStringRGB(RelationshipCategory.GetColor(RelationshipCategory.GetCategory(npc.RelationData.RelationDelta))) + ">" + RelationshipCategory.GetCategory(npc.RelationData.RelationDelta).ToString() + "</color>";
			RelationshipLabel.enabled = true;
			RelationshipContainer.gameObject.SetActive(value: true);
		}
		else
		{
			RelationshipContainer.gameObject.SetActive(value: false);
		}
		Customer component = npc.GetComponent<Customer>();
		StandardsContainer.gameObject.SetActive(value: false);
		if (component != null)
		{
			AddictionContainer.gameObject.SetActive(npc.RelationData.Unlocked);
			AddictionScrollbar.value = component.CurrentAddiction;
			AddictionLabel.text = Mathf.FloorToInt(component.CurrentAddiction * 100f) + "%";
			AddictionLabel.color = Color.Lerp(DependenceColor_Min, DependenceColor_Max, component.CurrentAddiction);
			EQuality correspondingQuality = component.CustomerData.Standards.GetCorrespondingQuality();
			StandardsStar.color = ItemQuality.GetColor(correspondingQuality);
			StandardsLabel.color = StandardsStar.color;
			StandardsLabel.text = component.CustomerData.Standards.GetName();
			StandardsContainer.gameObject.SetActive(value: true);
			PropertiesContainer.gameObject.SetActive(value: true);
			PropertiesLabel.text = string.Empty;
			for (int i = 0; i < component.CustomerData.PreferredProperties.Count; i++)
			{
				if (i > 0)
				{
					PropertiesLabel.text += "\n";
				}
				string text = "<color=#" + ColorUtility.ToHtmlStringRGBA(component.CustomerData.PreferredProperties[i].LabelColor) + ">•  " + component.CustomerData.PreferredProperties[i].Name + "</color>";
				PropertiesLabel.text += text;
			}
		}
		else
		{
			AddictionContainer.gameObject.SetActive(value: false);
			PropertiesContainer.gameObject.SetActive(value: false);
		}
		LayoutGroup.CalculateLayoutInputHorizontal();
		LayoutGroup.CalculateLayoutInputVertical();
	}

	public void ShowOnMap()
	{
		if (!(poi == null) && !(poi.UI == null))
		{
			if (NetworkSingleton<VariableDatabase>.InstanceExists)
			{
				NetworkSingleton<VariableDatabase>.Instance.SetVariableValue("PotentialCustomerShownOnMap", true.ToString(), network: false);
			}
			PlayerSingleton<ContactsApp>.Instance.SetOpen(open: false);
			PlayerSingleton<MapApp>.Instance.FocusPosition(poi.UI.anchoredPosition);
			PlayerSingleton<MapApp>.Instance.SkipFocusPlayer = true;
			PlayerSingleton<MapApp>.Instance.SetOpen(open: true);
		}
	}
}
