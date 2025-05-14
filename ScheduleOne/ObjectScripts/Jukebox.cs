using System;
using System.Collections.Generic;
using System.Linq;
using EasyButtons;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Serializing;
using FishNet.Serializing.Generated;
using FishNet.Transporting;
using GameKit.Utilities;
using ScheduleOne.Audio;
using ScheduleOne.DevUtilities;
using ScheduleOne.EntityFramework;
using ScheduleOne.Persistence.Datas;
using UnityEngine;

namespace ScheduleOne.ObjectScripts;

public class Jukebox : GridItem
{
	[Serializable]
	public class Track
	{
		public string TrackName;

		public AudioClip Clip;

		public string ArtistName = "KAESUL";
	}

	[Serializable]
	public class JukeboxState
	{
		public int CurrentVolume = 4;

		public bool IsPlaying;

		public float CurrentTrackTime;

		public int[] TrackOrder;

		public int CurrentTrackOrderIndex;

		public bool Shuffle;

		public ERepeatMode RepeatMode = ERepeatMode.RepeatQueue;

		public bool Sync;
	}

	public enum ERepeatMode
	{
		None = 0,
		RepeatQueue = 1,
		RepeatTrack = 2
	}

	public const float MUSIC_FADE_MULTIPLIER = 0.4f;

	public const int TRACK_COUNT = 27;

	private JukeboxState _jukeboxState;

	[Header("References")]
	public Track[] TrackList;

	public GameObject[] VolumeIndicatorBars;

	public AudioSourceController AudioSourceController;

	public Action onStateChanged;

	private bool NetworkInitialize___EarlyScheduleOne_002EObjectScripts_002EJukeboxAssembly_002DCSharp_002Edll_Excuted;

	private bool NetworkInitialize__LateScheduleOne_002EObjectScripts_002EJukeboxAssembly_002DCSharp_002Edll_Excuted;

	public int CurrentVolume => _jukeboxState.CurrentVolume;

	public float NormalizedVolume => (float)CurrentVolume / 8f;

	public bool IsPlaying => _jukeboxState.IsPlaying;

	public float CurrentTrackTime => _jukeboxState.CurrentTrackTime;

	private int[] TrackOrder => _jukeboxState.TrackOrder;

	public int CurrentTrackOrderIndex => _jukeboxState.CurrentTrackOrderIndex;

	public bool Shuffle => _jukeboxState.Shuffle;

	public ERepeatMode RepeatMode => _jukeboxState.RepeatMode;

	public bool Sync => _jukeboxState.Sync;

	public Track currentTrack => GetTrack(CurrentTrackOrderIndex);

	private AudioClip currentClip => GetTrack(CurrentTrackOrderIndex).Clip;

	public override void Awake()
	{
		NetworkInitialize___Early();
		Awake_UserLogic_ScheduleOne_002EObjectScripts_002EJukebox_Assembly_002DCSharp_002Edll();
		NetworkInitialize__Late();
	}

	private void FixedUpdate()
	{
		if (IsPlaying)
		{
			ApplyVolume();
			_jukeboxState.CurrentTrackTime += Time.fixedDeltaTime;
		}
		if (IsPlaying && CurrentTrackTime >= currentClip.length)
		{
			if (RepeatMode == ERepeatMode.None && CurrentTrackOrderIndex == 26)
			{
				_jukeboxState.IsPlaying = false;
			}
			else
			{
				Next();
			}
		}
	}

	public override void OnSpawnServer(NetworkConnection connection)
	{
		base.OnSpawnServer(connection);
		if (!connection.IsLocalClient)
		{
			SetJukeboxState(connection, _jukeboxState, setTrackTime: true, setSync: true);
		}
	}

	public void ChangeVolume(int change)
	{
		SetVolume(CurrentVolume + change, replicate: true);
	}

	public void SetVolume(int volume, bool replicate)
	{
		_jukeboxState.CurrentVolume = Mathf.Clamp(volume, 0, 8);
		for (int i = 0; i < VolumeIndicatorBars.Length; i++)
		{
			VolumeIndicatorBars[i].SetActive(i < CurrentVolume);
		}
		ApplyVolume();
		if (replicate)
		{
			ReplicateStateToOtherClients(setTrackTime: false);
		}
	}

	private void ApplyVolume()
	{
		AudioSourceController.VolumeMultiplier = NormalizedVolume * Singleton<AudioManager>.Instance.GetScaledMusicVolumeMultiplier(0.4f);
		AudioSourceController.ApplyVolume();
	}

	[Button]
	public void TogglePlay()
	{
		if (IsPlaying)
		{
			_jukeboxState.IsPlaying = false;
		}
		else
		{
			_jukeboxState.IsPlaying = true;
		}
		ReplicateStateToOtherClients(setTrackTime: true);
		if (Sync)
		{
			ReplicateStateToOtherJukeboxes(setTrackTime: true);
		}
	}

	[Button]
	public void Back()
	{
		if (_jukeboxState.CurrentTrackTime < 1f)
		{
			_jukeboxState.CurrentTrackOrderIndex = GetPreviousTrackOrderIndex();
			_jukeboxState.CurrentTrackTime = 0f;
		}
		else
		{
			_jukeboxState.CurrentTrackTime = 0f;
		}
		ReplicateStateToOtherClients(setTrackTime: true);
		if (Sync)
		{
			ReplicateStateToOtherJukeboxes(setTrackTime: true);
		}
	}

	[Button]
	public void Next()
	{
		_jukeboxState.CurrentTrackTime = 0f;
		_jukeboxState.CurrentTrackOrderIndex = GetNextTrackOrderIndex();
		_jukeboxState.IsPlaying = true;
		ReplicateStateToOtherClients(setTrackTime: true);
		if (Sync)
		{
			ReplicateStateToOtherJukeboxes(setTrackTime: true);
		}
	}

	private int GetPreviousTrackOrderIndex()
	{
		if (RepeatMode == ERepeatMode.RepeatTrack)
		{
			return CurrentTrackOrderIndex;
		}
		if (CurrentTrackOrderIndex == 0)
		{
			return 26;
		}
		return CurrentTrackOrderIndex - 1;
	}

	private int GetNextTrackOrderIndex()
	{
		if (RepeatMode == ERepeatMode.RepeatTrack)
		{
			return CurrentTrackOrderIndex;
		}
		if (CurrentTrackOrderIndex == 26)
		{
			return 0;
		}
		return CurrentTrackOrderIndex + 1;
	}

	[Button]
	public void ToggleShuffle()
	{
		if (Shuffle)
		{
			_jukeboxState.Shuffle = false;
			int currentTrackOrderIndex = TrackOrder[_jukeboxState.CurrentTrackOrderIndex];
			_jukeboxState.TrackOrder = new int[27];
			for (int i = 0; i < TrackOrder.Length; i++)
			{
				TrackOrder[i] = i;
			}
			_jukeboxState.CurrentTrackOrderIndex = currentTrackOrderIndex;
		}
		else
		{
			_jukeboxState.Shuffle = true;
			int item = TrackOrder[_jukeboxState.CurrentTrackOrderIndex];
			_jukeboxState.CurrentTrackOrderIndex = 0;
			List<int> list = new List<int>(TrackOrder);
			list.Remove(item);
			list.Shuffle();
			list.Insert(0, item);
			_jukeboxState.TrackOrder = list.ToArray();
		}
		ReplicateStateToOtherClients(setTrackTime: false);
		if (Sync)
		{
			ReplicateStateToOtherJukeboxes(setTrackTime: false);
		}
	}

	[Button]
	public void ToggleRepeatMode()
	{
		if (RepeatMode == ERepeatMode.RepeatQueue)
		{
			_jukeboxState.RepeatMode = ERepeatMode.RepeatTrack;
		}
		else if (RepeatMode == ERepeatMode.RepeatTrack)
		{
			_jukeboxState.RepeatMode = ERepeatMode.None;
		}
		else
		{
			_jukeboxState.RepeatMode = ERepeatMode.RepeatQueue;
		}
		ReplicateStateToOtherClients(setTrackTime: false);
		if (Sync)
		{
			ReplicateStateToOtherJukeboxes(setTrackTime: false);
		}
	}

	[Button]
	public void ToggleSync()
	{
		if (Sync)
		{
			_jukeboxState.Sync = false;
		}
		else
		{
			_jukeboxState.Sync = true;
			ReplicateStateToOtherJukeboxes(setTrackTime: true);
		}
		ReplicateStateToOtherClients(setTrackTime: false);
	}

	public void PlayTrack(int trackID)
	{
		_jukeboxState.IsPlaying = true;
		_jukeboxState.CurrentTrackTime = 0f;
		if (Shuffle)
		{
			_jukeboxState.CurrentTrackOrderIndex = 0;
			List<int> list = new List<int>(TrackOrder);
			list.Remove(trackID);
			list.Shuffle();
			list.Insert(0, trackID);
			_jukeboxState.TrackOrder = list.ToArray();
		}
		else
		{
			_jukeboxState.CurrentTrackOrderIndex = TrackOrder.ToList().IndexOf(trackID);
		}
		ReplicateStateToOtherClients(setTrackTime: true);
		if (Sync)
		{
			ReplicateStateToOtherJukeboxes(setTrackTime: true);
		}
	}

	[ServerRpc(RequireOwnership = false, RunLocally = true)]
	public void SendJukeboxState(JukeboxState state, bool setTrackTime, bool setSync)
	{
		RpcWriter___Server_SendJukeboxState_1728100027(state, setTrackTime, setSync);
		RpcLogic___SendJukeboxState_1728100027(state, setTrackTime, setSync);
	}

	[ObserversRpc(RunLocally = true)]
	[TargetRpc]
	public void SetJukeboxState(NetworkConnection conn, JukeboxState state, bool setTrackTime, bool setSync)
	{
		if ((object)conn == null)
		{
			RpcWriter___Observers_SetJukeboxState_2499833112(conn, state, setTrackTime, setSync);
			RpcLogic___SetJukeboxState_2499833112(conn, state, setTrackTime, setSync);
		}
		else
		{
			RpcWriter___Target_SetJukeboxState_2499833112(conn, state, setTrackTime, setSync);
		}
	}

	public void SetJukeboxState(JukeboxState state, bool setTrackTime)
	{
		SetVolume(state.CurrentVolume, replicate: false);
		if (ValidateQueue(state.TrackOrder))
		{
			_jukeboxState.TrackOrder = state.TrackOrder;
		}
		else
		{
			Console.LogWarning("Invalid queue data received. Using default queue. Invalid queue: " + string.Join(", ", state.TrackOrder));
		}
		_jukeboxState.CurrentTrackOrderIndex = state.CurrentTrackOrderIndex;
		_jukeboxState.IsPlaying = state.IsPlaying;
		_jukeboxState.CurrentTrackTime = state.CurrentTrackTime;
		_jukeboxState.Shuffle = state.Shuffle;
		_jukeboxState.RepeatMode = state.RepeatMode;
		_jukeboxState.Sync = state.Sync;
		if (setTrackTime)
		{
			AudioSourceController.AudioSource.time = CurrentTrackTime;
		}
		AudioSourceController.AudioSource.clip = GetTrack(CurrentTrackOrderIndex).Clip;
		if (IsPlaying)
		{
			if (!AudioSourceController.isPlaying)
			{
				AudioSourceController.Play();
			}
		}
		else if (AudioSourceController.isPlaying)
		{
			AudioSourceController.Stop();
		}
		if (setTrackTime)
		{
			AudioSourceController.AudioSource.time = CurrentTrackTime;
		}
		if (onStateChanged != null)
		{
			onStateChanged();
		}
	}

	private Track GetTrack(int orderIndex)
	{
		if (orderIndex < 0 || orderIndex >= TrackList.Length)
		{
			Console.LogWarning($"Invalid track index: {orderIndex}. Returning null.");
			return null;
		}
		return TrackList[TrackOrder[orderIndex]];
	}

	private bool ValidateQueue(int[] queue)
	{
		if (queue == null || queue.Length != 27)
		{
			Console.LogWarning("Queue is null or has invalid length.");
			return false;
		}
		if (queue.Distinct().Count() != 27)
		{
			Console.LogWarning("Queue has duplicates.");
			return false;
		}
		foreach (int num in queue)
		{
			if (num < 0 || num >= 27)
			{
				Console.LogWarning($"Queue has invalid value: {num}. Must be between 0 and {26}.");
				return false;
			}
		}
		return true;
	}

	private void ReplicateStateToOtherClients(bool setTrackTime)
	{
		SendJukeboxState(_jukeboxState, setTrackTime, setSync: true);
	}

	private void ReplicateStateToOtherJukeboxes(bool setTrackTime)
	{
		foreach (Jukebox item in base.ParentProperty.GetBuildablesOfType<Jukebox>())
		{
			if (!(item == this))
			{
				item.SendJukeboxState(_jukeboxState, setTrackTime, setSync: false);
			}
		}
	}

	public override string GetSaveString()
	{
		return new JukeboxData(base.GUID, base.ItemInstance, 0, base.OwnerGrid, OriginCoordinate, Rotation, _jukeboxState).GetJson();
	}

	public override void NetworkInitialize___Early()
	{
		if (!NetworkInitialize___EarlyScheduleOne_002EObjectScripts_002EJukeboxAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize___EarlyScheduleOne_002EObjectScripts_002EJukeboxAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize___Early();
			RegisterServerRpc(8u, RpcReader___Server_SendJukeboxState_1728100027);
			RegisterObserversRpc(9u, RpcReader___Observers_SetJukeboxState_2499833112);
			RegisterTargetRpc(10u, RpcReader___Target_SetJukeboxState_2499833112);
		}
	}

	public override void NetworkInitialize__Late()
	{
		if (!NetworkInitialize__LateScheduleOne_002EObjectScripts_002EJukeboxAssembly_002DCSharp_002Edll_Excuted)
		{
			NetworkInitialize__LateScheduleOne_002EObjectScripts_002EJukeboxAssembly_002DCSharp_002Edll_Excuted = true;
			base.NetworkInitialize__Late();
		}
	}

	public override void NetworkInitializeIfDisabled()
	{
		NetworkInitialize___Early();
		NetworkInitialize__Late();
	}

	private void RpcWriter___Server_SendJukeboxState_1728100027(JukeboxState state, bool setTrackTime, bool setSync)
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
			GeneratedWriters___Internal.Write___ScheduleOne_002EObjectScripts_002EJukebox_002FJukeboxStateFishNet_002ESerializing_002EGenerated(writer, state);
			writer.WriteBoolean(setTrackTime);
			writer.WriteBoolean(setSync);
			SendServerRpc(8u, writer, channel, DataOrderType.Default);
			writer.Store();
		}
	}

	public void RpcLogic___SendJukeboxState_1728100027(JukeboxState state, bool setTrackTime, bool setSync)
	{
		SetJukeboxState(null, state, setTrackTime, setSync);
	}

	private void RpcReader___Server_SendJukeboxState_1728100027(PooledReader PooledReader0, Channel channel, NetworkConnection conn)
	{
		JukeboxState state = GeneratedReaders___Internal.Read___ScheduleOne_002EObjectScripts_002EJukebox_002FJukeboxStateFishNet_002ESerializing_002EGenerateds(PooledReader0);
		bool setTrackTime = PooledReader0.ReadBoolean();
		bool setSync = PooledReader0.ReadBoolean();
		if (base.IsServerInitialized && !conn.IsLocalClient)
		{
			RpcLogic___SendJukeboxState_1728100027(state, setTrackTime, setSync);
		}
	}

	private void RpcWriter___Observers_SetJukeboxState_2499833112(NetworkConnection conn, JukeboxState state, bool setTrackTime, bool setSync)
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
			GeneratedWriters___Internal.Write___ScheduleOne_002EObjectScripts_002EJukebox_002FJukeboxStateFishNet_002ESerializing_002EGenerated(writer, state);
			writer.WriteBoolean(setTrackTime);
			writer.WriteBoolean(setSync);
			SendObserversRpc(9u, writer, channel, DataOrderType.Default, bufferLast: false, excludeServer: false, excludeOwner: false);
			writer.Store();
		}
	}

	public void RpcLogic___SetJukeboxState_2499833112(NetworkConnection conn, JukeboxState state, bool setTrackTime, bool setSync)
	{
		SetJukeboxState(state, setTrackTime);
	}

	private void RpcReader___Observers_SetJukeboxState_2499833112(PooledReader PooledReader0, Channel channel)
	{
		JukeboxState state = GeneratedReaders___Internal.Read___ScheduleOne_002EObjectScripts_002EJukebox_002FJukeboxStateFishNet_002ESerializing_002EGenerateds(PooledReader0);
		bool setTrackTime = PooledReader0.ReadBoolean();
		bool setSync = PooledReader0.ReadBoolean();
		if (base.IsClientInitialized && !base.IsHost)
		{
			RpcLogic___SetJukeboxState_2499833112(null, state, setTrackTime, setSync);
		}
	}

	private void RpcWriter___Target_SetJukeboxState_2499833112(NetworkConnection conn, JukeboxState state, bool setTrackTime, bool setSync)
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
			GeneratedWriters___Internal.Write___ScheduleOne_002EObjectScripts_002EJukebox_002FJukeboxStateFishNet_002ESerializing_002EGenerated(writer, state);
			writer.WriteBoolean(setTrackTime);
			writer.WriteBoolean(setSync);
			SendTargetRpc(10u, writer, channel, DataOrderType.Default, conn, excludeServer: false);
			writer.Store();
		}
	}

	private void RpcReader___Target_SetJukeboxState_2499833112(PooledReader PooledReader0, Channel channel)
	{
		JukeboxState state = GeneratedReaders___Internal.Read___ScheduleOne_002EObjectScripts_002EJukebox_002FJukeboxStateFishNet_002ESerializing_002EGenerateds(PooledReader0);
		bool setTrackTime = PooledReader0.ReadBoolean();
		bool setSync = PooledReader0.ReadBoolean();
		if (base.IsClientInitialized)
		{
			RpcLogic___SetJukeboxState_2499833112(base.LocalConnection, state, setTrackTime, setSync);
		}
	}

	protected virtual void Awake_UserLogic_ScheduleOne_002EObjectScripts_002EJukebox_Assembly_002DCSharp_002Edll()
	{
		base.Awake();
		if (isGhost)
		{
			return;
		}
		_jukeboxState = new JukeboxState();
		_jukeboxState.TrackOrder = new int[27];
		for (int i = 0; i < TrackOrder.Length; i++)
		{
			TrackOrder[i] = i;
		}
		for (int j = 0; j < TrackList.Length; j++)
		{
			if (TrackList[j].Clip == null)
			{
				Console.LogError($"Track {j} does not have a clip assigned.");
			}
		}
		SetVolume(CurrentVolume, replicate: true);
	}
}
