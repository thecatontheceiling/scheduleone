using System;
using ScheduleOne.DevUtilities;
using ScheduleOne.ScriptableObjects;
using ScheduleOne.UI.Phone;
using UnityEngine;

namespace ScheduleOne.Calling;

public class CallManager : Singleton<CallManager>
{
	public PhoneCallData QueuedCallData { get; private set; }

	protected override void Start()
	{
		base.Start();
		if (Singleton<CallInterface>.Instance == null)
		{
			Debug.LogError("CallInterface instance is null. CallManager cannot function without it.");
			return;
		}
		CallInterface callInterface = Singleton<CallInterface>.Instance;
		callInterface.CallCompleted = (Action<PhoneCallData>)Delegate.Combine(callInterface.CallCompleted, new Action<PhoneCallData>(CallCompleted));
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		if (Singleton<CallInterface>.Instance != null)
		{
			CallInterface callInterface = Singleton<CallInterface>.Instance;
			callInterface.CallCompleted = (Action<PhoneCallData>)Delegate.Remove(callInterface.CallCompleted, new Action<PhoneCallData>(CallCompleted));
		}
	}

	public void QueueCall(PhoneCallData data)
	{
		QueuedCallData = data;
	}

	public void ClearQueuedCall()
	{
		QueuedCallData = null;
	}

	private void CallCompleted(PhoneCallData call)
	{
		if (call == QueuedCallData)
		{
			ClearQueuedCall();
		}
	}
}
