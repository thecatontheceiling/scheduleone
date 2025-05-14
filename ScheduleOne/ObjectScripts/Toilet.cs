using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Serializing;
using FishNet.Transporting;
using ScheduleOne.EntityFramework;
using ScheduleOne.Interaction;
using ScheduleOne.Trash;
using UnityEngine;
using UnityEngine.Events;

namespace ScheduleOne.ObjectScripts;

public class Toilet : GridItem
{
	public float InitialDelay = 0.5f;

	public float FlushTime = 5f;

	public InteractableObject IntObj;

	public LayerMask ItemLayerMask;

	public SphereCollider ItemDetectionCollider;

	public UnityEvent OnFlush;

	private Coroutine _flushCoroutine;

	private bool isFlushing;

	private bool NetworkInitialize___EarlyScheduleOne_002EObjectScripts_002EToiletAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002EObjectScripts_002EToiletAssembly_002DCSharp_002Edll_Excuted;

	public void Hovered()
	{
		if (!isFlushing)
		{
			IntObj.SetInteractableState(InteractableObject.EInteractableState.Default);
			IntObj.SetMessage("Flush");
		}
		else
		{
			IntObj.SetInteractableState(InteractableObject.EInteractableState.Disabled);
		}
	}

	public void Interacted()
	{
		isFlushing = true;
		SendFlush();
	}

	[ServerRpc(RequireOwnership = false)]
	private void SendFlush()
	{
		RpcWriter___Server_SendFlush_2166136261();
	}

	[ObserversRpc]
	private void Flush()
	{
		RpcWriter___Observers_Flush_2166136261();
	}

	public override void NetworkInitialize___Early()
	{
		if (!NetworkInitialize___EarlyScheduleOne_002EObjectScripts_002EToiletAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002EObjectScripts_002EToiletAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize___Early();
			RegisterServerRpc(8u, RpcReader___Server_SendFlush_2166136261);
			RegisterObserversRpc(9u, RpcReader___Observers_Flush_2166136261);
		}
	}

	public override void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002EObjectScripts_002EToiletAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002EObjectScripts_002EToiletAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize__Late();
		}
	}

	public override void NetworkInitializeIfDisabled()
	{
		NetworkInitialize___Early();
		NetworkInitialize__Late();
	}

	private void RpcWriter___Server_SendFlush_2166136261()
	{
		if (!base.IsClientInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because client is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			SendServerRpc(8u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	private void RpcLogic___SendFlush_2166136261()
	{
		Flush();
	}

	private void RpcReader___Server_SendFlush_2166136261(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		if (base.IsServerInitialized)
		{
			RpcLogic___SendFlush_2166136261();
		}
	}

	private void RpcWriter___Observers_Flush_2166136261()
	{
		if (!base.IsServerInitialized)
		{
			NetworkManager networkManager = base.NetworkManager;
			if ((object)networkManager == null)
			{
				networkManager = InstanceFinder.NetworkManager;
			}
			if ((object)networkManager != null)
			{
				networkManager.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
			else
			{
				Debug.LogWarning("Cannot complete action because server is not active. This may also occur if the object is not yet initialized, has deinitialized, or if it does not contain a NetworkObject component.");
			}
		}
		else
		{
			Channel channel = Channel.Reliable;
			PooledWriter writer = WriterPool.GetWriter();
			SendObserversRpc(9u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	private void RpcLogic___Flush_2166136261()
	{
		isFlushing = true;
		_flushCoroutine = StartCoroutine(Routine());
		IEnumerator Routine()
		{
			if (OnFlush != null)
			{
				OnFlush.Invoke();
			}
			yield return new WaitForSeconds(InitialDelay);
			float checkRate = 0.5f;
			int reps = (int)(FlushTime / checkRate);
			for (int i = 0; i < reps; i++)
			{
				if (InstanceFinder.IsServer)
				{
					Collider[] array = Physics.OverlapSphere(ItemDetectionCollider.transform.position, ItemDetectionCollider.radius, ItemLayerMask);
					List<TrashItem> list = new List<TrashItem>();
					Collider[] array2 = array;
					for (int j = 0; j < array2.Length; j++)
					{
						TrashItem componentInParent = array2[j].GetComponentInParent<TrashItem>();
						if (componentInParent != null && !list.Contains(componentInParent))
						{
							list.Add(componentInParent);
						}
					}
					if (list.Count > 0)
					{
						foreach (TrashItem item in list)
						{
							item.DestroyTrash();
						}
					}
				}
				yield return new WaitForSeconds(checkRate);
			}
			_flushCoroutine = null;
			isFlushing = false;
		}
	}

	private void RpcReader___Observers_Flush_2166136261(PooledReader PooledReader0, Channel channel)
	{
		if (base.IsClientInitialized)
		{
			RpcLogic___Flush_2166136261();
		}
	}

	public override void Awake()
	{
		NetworkInitialize___Early();
		base.Awake();
		NetworkInitialize__Late();
	}
}
