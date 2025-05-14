using System;
using System.Runtime.CompilerServices;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Serializing;
using FishNet.Transporting;
using ScheduleOne.GameTime;
using ScheduleOne.Persistence.Datas;
using UnityEngine;
using UnityEngine.Events;

namespace ScheduleOne.NPCs;

[RequireComponent(typeof(NPCHealth))]
[DisallowMultipleComponent]
public class NPCHealth : NetworkBehaviour
{
	public const int REVIVE_DAYS = 3;

	[CompilerGenerated]
	[SyncVar(WritePermissions = WritePermission.ClientUnsynchronized)]
	public float _003CHealth_003Ek__BackingField;

	[Header("Settings")]
	public bool Invincible;

	public float MaxHealth = 100f;

	private NPC npc;

	public UnityEvent onDie;

	public UnityEvent onKnockedOut;

	private bool AfflictedWithLethalEffect;

	public SyncVar<float> syncVar____003CHealth_003Ek__BackingField;

	private bool NetworkInitialize___EarlyScheduleOne_002ENPCs_002ENPCHealthAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002ENPCs_002ENPCHealthAssembly_002DCSharp_002Edll_Excuted;

	public float Health
	{
		[CompilerGenerated]
		get
		{
			return SyncAccessor__003CHealth_003Ek__BackingField;
		}
		[CompilerGenerated]
		private set
		{
			this.sync___set_value__003CHealth_003Ek__BackingField(value, asServer: true);
		}
	}

	public bool IsDead { get; private set; }

	public bool IsKnockedOut { get; private set; }

	public int DaysPassedSinceDeath { get; private set; }

	public float SyncAccessor__003CHealth_003Ek__BackingField
	{
		get
		{
			return Health;
		}
		set
		{
			if (value || !base.IsServerInitialized)
			{
				Health = value;
			}
			if (Application.isPlaying)
			{
				syncVar____003CHealth_003Ek__BackingField.SetValue(value, value);
			}
		}
	}

	public virtual void Awake()
	{
		NetworkInitialize___Early();
		Awake_UserLogic_ScheduleOne_002ENPCs_002ENPCHealth_Assembly_002DCSharp_002Edll();
		NetworkInitialize__Late();
	}

	private void OnDestroy()
	{
		ScheduleOne.GameTime.TimeManager.onSleepStart = (Action)Delegate.Remove(ScheduleOne.GameTime.TimeManager.onSleepStart, new Action(SleepStart));
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		Health = MaxHealth;
	}

	public void Load(NPCHealthData healthData)
	{
		Health = healthData.Health;
		DaysPassedSinceDeath = healthData.DaysPassedSinceDeath;
		if (IsDead)
		{
			Die();
		}
		else if (SyncAccessor__003CHealth_003Ek__BackingField == 0f)
		{
			KnockOut();
		}
	}

	private void Update()
	{
		if (!IsDead && AfflictedWithLethalEffect)
		{
			TakeDamage(15f * Time.deltaTime);
		}
	}

	public void SetAfflictedWithLethalEffect(bool value)
	{
		AfflictedWithLethalEffect = value;
	}

	public void SleepStart()
	{
		if (!InstanceFinder.IsServer)
		{
			return;
		}
		if (!npc.IsConscious)
		{
			Console.Log(npc.fullName + " Dead: " + IsDead);
			if (IsDead)
			{
				DaysPassedSinceDeath++;
				if (DaysPassedSinceDeath >= 3 || npc.IsImportant)
				{
					Revive();
				}
			}
			else
			{
				Revive();
			}
		}
		if (npc.IsConscious)
		{
			Health = MaxHealth;
		}
	}

	public void TakeDamage(float damage, bool isLethal = true)
	{
		if (IsDead)
		{
			return;
		}
		Console.Log(npc.fullName + " has taken " + damage + " damage.");
		SyncAccessor__003CHealth_003Ek__BackingField -= damage;
		if (!(SyncAccessor__003CHealth_003Ek__BackingField <= 0f))
		{
			return;
		}
		Health = 0f;
		if (Invincible)
		{
			return;
		}
		if (isLethal)
		{
			if (!IsDead)
			{
				Die();
			}
		}
		else if (!IsKnockedOut)
		{
			KnockOut();
		}
	}

	public virtual void Die()
	{
		if (!Invincible)
		{
			Console.Log(npc.fullName + " has died.");
			IsDead = true;
			npc.behaviour.DeadBehaviour.Enable_Networked(null);
			if (onDie != null)
			{
				onDie.Invoke();
			}
		}
	}

	public virtual void KnockOut()
	{
		if (!Invincible)
		{
			Console.Log(npc.fullName + " has been knocked out.");
			IsKnockedOut = true;
			npc.behaviour.UnconsciousBehaviour.Enable_Networked(null);
			if (onKnockedOut != null)
			{
				onKnockedOut.Invoke();
			}
		}
	}

	public virtual void Revive()
	{
		Console.Log(npc.fullName + " has been revived.");
		IsDead = false;
		IsKnockedOut = false;
		Health = MaxHealth;
		npc.behaviour.DeadBehaviour.SendDisable();
		npc.behaviour.UnconsciousBehaviour.SendDisable();
	}

	public virtual void NetworkInitialize___Early()
	{
		if (!NetworkInitialize___EarlyScheduleOne_002ENPCs_002ENPCHealthAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002ENPCs_002ENPCHealthAssembly_002DCSharp_002Edll_Excuted = true;
			syncVar____003CHealth_003Ek__BackingField = new SyncVar<float>(this, 0u, WritePermission.ClientUnsynchronized, ReadPermission.Observers, -1f, Channel.Reliable, Health);
			RegisterSyncVarRead(ReadSyncVar___ScheduleOne_002ENPCs_002ENPCHealth);
		}
	}

	public virtual void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002ENPCs_002ENPCHealthAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002ENPCs_002ENPCHealthAssembly_002DCSharp_002Edll_Excuted = true;
			syncVar____003CHealth_003Ek__BackingField.SetRegistered();
		}
	}

	public override void NetworkInitializeIfDisabled()
	{
		NetworkInitialize___Early();
		NetworkInitialize__Late();
	}

	public virtual bool ReadSyncVar___ScheduleOne_002ENPCs_002ENPCHealth(PooledReader PooledReader0, uint UInt321, bool Boolean2)
	{
		if (UInt321 == 0)
		{
			if (PooledReader0 == null)
			{
				this.sync___set_value__003CHealth_003Ek__BackingField(syncVar____003CHealth_003Ek__BackingField.GetValue(calledByUser: true), asServer: true);
				return true;
			}
			float value = PooledReader0.ReadSingle();
			this.sync___set_value__003CHealth_003Ek__BackingField(value, Boolean2);
			return true;
		}
		return false;
	}

	protected virtual void Awake_UserLogic_ScheduleOne_002ENPCs_002ENPCHealth_Assembly_002DCSharp_002Edll()
	{
		npc = GetComponent<NPC>();
		ScheduleOne.GameTime.TimeManager.onSleepStart = (Action)Delegate.Remove(ScheduleOne.GameTime.TimeManager.onSleepStart, new Action(SleepStart));
		ScheduleOne.GameTime.TimeManager.onSleepStart = (Action)Delegate.Combine(ScheduleOne.GameTime.TimeManager.onSleepStart, new Action(SleepStart));
	}
}
