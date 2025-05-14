using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ScheduleOne.Audio;
using ScheduleOne.Delivery;
using ScheduleOne.DevUtilities;
using ScheduleOne.GameTime;
using ScheduleOne.UI.Shop;
using UnityEngine;
using UnityEngine.UI;

namespace ScheduleOne.UI.Phone.Delivery;

public class DeliveryApp : App<DeliveryApp>
{
	private List<DeliveryShop> deliveryShops = new List<DeliveryShop>();

	public DeliveryStatusDisplay StatusDisplayPrefab;

	[Header("References")]
	public Animation OrderSubmittedAnim;

	public AudioSourceController OrderSubmittedSound;

	public RectTransform StatusDisplayContainer;

	public RectTransform NoDeliveriesIndicator;

	public ScrollRect MainScrollRect;

	public LayoutGroup MainLayoutGroup;

	private List<DeliveryStatusDisplay> statusDisplays = new List<DeliveryStatusDisplay>();

	private bool started;

	protected override void Awake()
	{
		base.Awake();
		deliveryShops = GetComponentsInChildren<DeliveryShop>(includeInactive: true).ToList();
	}

	protected override void Start()
	{
		base.Start();
		if (!started)
		{
			started = true;
			NetworkSingleton<DeliveryManager>.Instance.onDeliveryCreated.AddListener(CreateDeliveryStatusDisplay);
			NetworkSingleton<DeliveryManager>.Instance.onDeliveryCompleted.AddListener(DeliveryCompleted);
			TimeManager timeManager = NetworkSingleton<TimeManager>.Instance;
			timeManager.onMinutePass = (Action)Delegate.Combine(timeManager.onMinutePass, new Action(OnMinPass));
			for (int i = 0; i < NetworkSingleton<DeliveryManager>.Instance.Deliveries.Count; i++)
			{
				CreateDeliveryStatusDisplay(NetworkSingleton<DeliveryManager>.Instance.Deliveries[i]);
			}
		}
	}

	protected override void Update()
	{
		base.Update();
	}

	public override void SetOpen(bool open)
	{
		base.SetOpen(open);
		if (!open)
		{
			return;
		}
		foreach (DeliveryShop deliveryShop in deliveryShops)
		{
			deliveryShop.RefreshShop();
		}
		foreach (DeliveryStatusDisplay statusDisplay in statusDisplays)
		{
			statusDisplay.RefreshStatus();
		}
		if (MainScrollRect.verticalNormalizedPosition > 1f)
		{
			MainScrollRect.verticalNormalizedPosition = 1f;
		}
		OrderSubmittedAnim.GetComponent<CanvasGroup>().alpha = 0f;
	}

	private void OnMinPass()
	{
		if (!base.isOpen)
		{
			return;
		}
		foreach (DeliveryStatusDisplay statusDisplay in statusDisplays)
		{
			statusDisplay.RefreshStatus();
		}
	}

	public void RefreshContent(bool keepScrollPosition = true)
	{
		float scrollPos = MainScrollRect.verticalNormalizedPosition;
		StartCoroutine(Delay());
		IEnumerator Delay()
		{
			RefreshLayoutGroupsImmediateAndRecursive(MainLayoutGroup.gameObject);
			if (keepScrollPosition)
			{
				MainScrollRect.verticalNormalizedPosition = scrollPos;
			}
			yield return new WaitForEndOfFrame();
			RefreshLayoutGroupsImmediateAndRecursive(MainLayoutGroup.gameObject);
			if (keepScrollPosition)
			{
				MainScrollRect.verticalNormalizedPosition = scrollPos;
			}
			yield return new WaitForEndOfFrame();
		}
	}

	public void PlayOrderSubmittedAnim()
	{
		OrderSubmittedAnim.Play();
		OrderSubmittedSound.Play();
	}

	private void CreateDeliveryStatusDisplay(DeliveryInstance instance)
	{
		DeliveryStatusDisplay deliveryStatusDisplay = UnityEngine.Object.Instantiate(StatusDisplayPrefab, StatusDisplayContainer);
		deliveryStatusDisplay.AssignDelivery(instance);
		statusDisplays.Add(deliveryStatusDisplay);
		SortStatusDisplays();
		RefreshContent();
		RefreshNoDeliveriesIndicator();
	}

	private void DeliveryCompleted(DeliveryInstance instance)
	{
		DeliveryStatusDisplay deliveryStatusDisplay = statusDisplays.FirstOrDefault((DeliveryStatusDisplay d) => d.DeliveryInstance.DeliveryID == instance.DeliveryID);
		if (deliveryStatusDisplay != null)
		{
			statusDisplays.Remove(deliveryStatusDisplay);
			UnityEngine.Object.Destroy(deliveryStatusDisplay.gameObject);
		}
		RefreshNoDeliveriesIndicator();
	}

	private void SortStatusDisplays()
	{
		statusDisplays = statusDisplays.OrderBy((DeliveryStatusDisplay d) => d.DeliveryInstance.GetTimeStatus()).ToList();
		for (int num = 0; num < statusDisplays.Count; num++)
		{
			statusDisplays[num].transform.SetSiblingIndex(num);
		}
	}

	private void RefreshNoDeliveriesIndicator()
	{
		NoDeliveriesIndicator.gameObject.SetActive(statusDisplays.Count == 0);
	}

	public static void RefreshLayoutGroupsImmediateAndRecursive(GameObject root)
	{
		LayoutGroup[] componentsInChildren = root.GetComponentsInChildren<LayoutGroup>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(componentsInChildren[i].GetComponent<RectTransform>());
		}
		LayoutRebuilder.ForceRebuildLayoutImmediate(root.GetComponent<LayoutGroup>().GetComponent<RectTransform>());
	}

	public DeliveryShop GetShop(ShopInterface matchingShop)
	{
		return deliveryShops.Find((DeliveryShop x) => x.MatchingShop == matchingShop);
	}

	public DeliveryShop GetShop(string shopName)
	{
		return deliveryShops.Find((DeliveryShop x) => x.MatchingShop.ShopName == shopName);
	}
}
