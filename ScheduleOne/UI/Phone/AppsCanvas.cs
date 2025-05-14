using System;
using System.Collections;
using ScheduleOne.DevUtilities;
using ScheduleOne.UI.Tooltips;
using UnityEngine;

namespace ScheduleOne.UI.Phone;

public class AppsCanvas : PlayerSingleton<AppsCanvas>
{
	[Header("References")]
	public Canvas canvas;

	private Coroutine delayedSetOpenRoutine;

	public bool isOpen { get; private set; }

	protected override void Awake()
	{
		base.Awake();
		SetIsOpen(o: false);
	}

	public override void OnStartClient(bool IsOwner)
	{
		base.OnStartClient(IsOwner);
		if (IsOwner)
		{
			Phone phone = PlayerSingleton<Phone>.Instance;
			phone.onPhoneOpened = (Action)Delegate.Combine(phone.onPhoneOpened, new Action(PhoneOpened));
			Phone phone2 = PlayerSingleton<Phone>.Instance;
			phone2.onPhoneClosed = (Action)Delegate.Combine(phone2.onPhoneClosed, new Action(PhoneClosed));
		}
	}

	protected void PhoneOpened()
	{
		if (isOpen)
		{
			SetCanvasActive(a: true);
		}
	}

	protected void PhoneClosed()
	{
		delayedSetOpenRoutine = StartCoroutine(DelayedSetCanvasActive(active: false, 0.25f));
	}

	private IEnumerator DelayedSetCanvasActive(bool active, float delay)
	{
		yield return new WaitForSeconds(delay);
		delayedSetOpenRoutine = null;
		SetCanvasActive(active);
	}

	public void SetIsOpen(bool o)
	{
		isOpen = o;
		SetCanvasActive(o);
	}

	private void SetCanvasActive(bool a)
	{
		if (delayedSetOpenRoutine != null)
		{
			StopCoroutine(delayedSetOpenRoutine);
		}
		canvas.enabled = a;
	}

	protected override void Start()
	{
		base.Start();
		Singleton<TooltipManager>.Instance.AddCanvas(canvas);
	}
}
