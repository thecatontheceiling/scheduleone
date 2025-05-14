using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FishNet;
using FishNet.Component.Scenes;
using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Transporting.Multipass;
using FishNet.Transporting.Yak;
using FishySteamworks;
using Pathfinding;
using ScheduleOne.Audio;
using ScheduleOne.DevUtilities;
using ScheduleOne.Economy;
using ScheduleOne.Money;
using ScheduleOne.Networking;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.Persistence.ItemLoaders;
using ScheduleOne.Persistence.Loaders;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Property;
using ScheduleOne.Quests;
using ScheduleOne.UI;
using ScheduleOne.UI.MainMenu;
using ScheduleOne.UI.Phone;
using Steamworks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace ScheduleOne.Persistence;

public class LoadManager : PersistentSingleton<LoadManager>
{
	public enum ELoadStatus
	{
		None = 0,
		LoadingScene = 1,
		Initializing = 2,
		LoadingData = 3,
		SpawningPlayer = 4,
		WaitingForHost = 5
	}

	public const int LOADS_PER_FRAME = 50;

	public const bool DEBUG = false;

	public const float LOAD_ERROR_TIMEOUT = 90f;

	public const float NETWORK_TIMEOUT = 30f;

	public static List<string> LoadHistory = new List<string>();

	public static SaveInfo[] SaveGames = new SaveInfo[5];

	public static SaveInfo LastPlayedGame = null;

	private List<LoadRequest> loadRequests = new List<LoadRequest>();

	public List<ItemLoader> ItemLoaders = new List<ItemLoader>();

	public List<BuildableItemLoader> ObjectLoaders = new List<BuildableItemLoader>();

	public List<NPCLoader> NPCLoaders = new List<NPCLoader>();

	public UnityEvent onPreSceneChange;

	public UnityEvent onPreLoad;

	public UnityEvent onLoadComplete;

	public UnityEvent onSaveInfoLoaded;

	public string DefaultTutorialSaveFolder => System.IO.Path.Combine(Application.streamingAssetsPath, "DefaultTutorialSave");

	public bool IsGameLoaded { get; protected set; }

	public bool IsLoading { get; protected set; }

	public float TimeSinceGameLoaded { get; protected set; }

	public bool DebugMode { get; protected set; }

	public ELoadStatus LoadStatus { get; protected set; }

	public string LoadedGameFolderPath { get; protected set; } = string.Empty;

	public SaveInfo ActiveSaveInfo { get; private set; }

	public SaveInfo StoredSaveInfo { get; private set; }

	protected override void Awake()
	{
		base.Awake();
	}

	protected override void Start()
	{
		base.Start();
		if (Singleton<LoadManager>.Instance == null || Singleton<LoadManager>.Instance != this)
		{
			return;
		}
		Bananas();
		InitializeItemLoaders();
		InitializeObjectLoaders();
		InitializeNPCLoaders();
		Singleton<SaveManager>.Instance.CheckSaveFolderInitialized();
		RefreshSaveInfo();
		if (SceneManager.GetActiveScene().name == "Main" || SceneManager.GetActiveScene().name == "Tutorial")
		{
			DebugMode = true;
			IsGameLoaded = true;
			LoadedGameFolderPath = System.IO.Path.Combine(Singleton<SaveManager>.Instance.IndividualSavesContainerPath, "DevSave");
			if (!Directory.Exists(LoadedGameFolderPath))
			{
				Directory.CreateDirectory(LoadedGameFolderPath);
			}
		}
	}

	private void Bananas()
	{
		string fullName = new DirectoryInfo(Application.dataPath).Parent.FullName;
		Console.Log("Game folder path: " + fullName);
		string path = System.IO.Path.Combine(fullName, "OnlineFix.ini");
		if (!File.Exists(path))
		{
			return;
		}
		string[] array;
		try
		{
			array = File.ReadAllLines(path);
		}
		catch (Exception ex)
		{
			Console.LogWarning("Error reading INI file: " + ex.Message);
			return;
		}
		int num = -1;
		int num2 = -1;
		string text = null;
		string text2 = null;
		for (int i = 0; i < array.Length; i++)
		{
			string text3 = array[i].Trim();
			if (text3.StartsWith("RealAppId="))
			{
				num = i;
				text = text3.Substring("RealAppId=".Length);
			}
			else if (text3.StartsWith("FakeAppId="))
			{
				num2 = i;
				text2 = text3.Substring("FakeAppId=".Length);
			}
		}
		if (num == -1 || num2 == -1)
		{
			return;
		}
		array[num] = "RealAppId=" + text2;
		array[num2] = "FakeAppId=" + text;
		try
		{
			File.WriteAllLines(path, array);
		}
		catch (Exception ex2)
		{
			Console.LogError("Error writing INI file: " + ex2.Message);
		}
	}

	private void InitializeItemLoaders()
	{
		new ItemLoader();
		new WateringCanLoader();
		new CashLoader();
		new QualityItemLoader();
		new ProductItemLoader();
		new WeedLoader();
		new MethLoader();
		new CocaineLoader();
		new IntegerItemLoader();
		new TrashGrabberLoader();
		new ClothingLoader();
	}

	private void InitializeObjectLoaders()
	{
		new BuildableItemLoader();
		new GridItemLoader();
		new ProceduralGridItemLoader();
		new SurfaceItemLoader();
		new ToggleableItemLoader();
		new PotLoader();
		new PackagingStationLoader();
		new StorageRackLoader();
		new ChemistryStationLoader();
		new LabOvenLoader();
		new BrickPressLoader();
		new MixingStationLoader();
		new CauldronLoader();
		new TrashContainerLoader();
		new SoilPourerLoader();
		new DryingRackLoader();
		new JukeboxLoader();
		new ToggleableSurfaceItemLoader();
		new StorageSurfaceItemLoader();
		new LabelledSurfaceItemLoader();
	}

	private void InitializeNPCLoaders()
	{
		new NPCLoader();
		new EmployeeLoader();
		new PackagerLoader();
		new BotanistLoader();
		new ChemistLoader();
		new CleanerLoader();
	}

	public void Update()
	{
		if (IsGameLoaded && LoadedGameFolderPath != string.Empty && Input.GetKeyDown(KeyCode.F6) && (Application.isEditor || Debug.isDebugBuild))
		{
			NetworkManager networkManager = UnityEngine.Object.FindObjectOfType<NetworkManager>();
			networkManager.ClientManager.StopConnection();
			networkManager.ServerManager.StopConnection(sendDisconnectMessage: false);
			StartGame(new SaveInfo(LoadedGameFolderPath, -1, "Test Org", DateTime.Now, DateTime.Now, 0f, Application.version, new MetaData(null, null, string.Empty, string.Empty, playTutorial: false)), allowLoadStacking: true);
		}
		if (IsGameLoaded && LoadStatus == ELoadStatus.None)
		{
			TimeSinceGameLoaded += Time.deltaTime;
		}
	}

	public void QueueLoadRequest(LoadRequest request)
	{
		loadRequests.Add(request);
	}

	public void DequeueLoadRequest(LoadRequest request)
	{
		loadRequests.Remove(request);
	}

	public ItemLoader GetItemLoader(string itemType)
	{
		ItemLoader itemLoader = ItemLoaders.Find((ItemLoader loader) => loader.ItemType == itemType);
		if (itemLoader == null)
		{
			Console.LogError("No item loader found for data type: " + itemType);
			return null;
		}
		return itemLoader;
	}

	public BuildableItemLoader GetObjectLoader(string objectType)
	{
		BuildableItemLoader buildableItemLoader = ObjectLoaders.Find((BuildableItemLoader loader) => loader.ItemType == objectType);
		if (buildableItemLoader == null)
		{
			Console.LogError("No object loader found for data type: " + objectType);
			return null;
		}
		return buildableItemLoader;
	}

	public NPCLoader GetNPCLoader(string npcType)
	{
		NPCLoader nPCLoader = NPCLoaders.Find((NPCLoader loader) => loader.NPCType == npcType);
		if (nPCLoader == null)
		{
			Console.LogError("No NPC loader found for NPC type: " + npcType);
			return null;
		}
		return nPCLoader;
	}

	public string GetLoadStatusText()
	{
		return LoadStatus switch
		{
			ELoadStatus.LoadingScene => "Loading world...", 
			ELoadStatus.Initializing => "Initializing...", 
			ELoadStatus.SpawningPlayer => "Spawning player...", 
			ELoadStatus.LoadingData => "Loading data...", 
			ELoadStatus.WaitingForHost => "Waiting for host to finish loading...", 
			_ => string.Empty, 
		};
	}

	public void StartGame(SaveInfo info, bool allowLoadStacking = false)
	{
		if (IsGameLoaded && !allowLoadStacking)
		{
			Console.LogWarning("Game already loaded, cannot start another");
			return;
		}
		if (info == null)
		{
			Console.LogWarning("Save info is null, cannot start game");
			return;
		}
		string savePath = info.SavePath;
		if (!Directory.Exists(savePath))
		{
			Console.LogWarning("Save game does not exist at " + savePath);
			return;
		}
		Singleton<MusicPlayer>.Instance.StopAndDisableTracks();
		Console.Log("Starting game!");
		ActiveSaveInfo = info;
		IsLoading = true;
		TimeSinceGameLoaded = 0f;
		LoadedGameFolderPath = info.SavePath;
		LoadHistory.Add("Loading game: " + ActiveSaveInfo.OrganisationName);
		StartCoroutine(LoadRoutine());
		IEnumerator Load()
		{
			Console.Log("Load start!");
			foreach (IBaseSaveable baseSaveable in Singleton<SaveManager>.Instance.BaseSaveables)
			{
				new LoadRequest(System.IO.Path.Combine(LoadedGameFolderPath, baseSaveable.SaveFolderName), baseSaveable.Loader);
			}
			while (loadRequests.Count > 0)
			{
				for (int i = 0; i < 50; i++)
				{
					if (loadRequests.Count <= 0)
					{
						break;
					}
					LoadRequest loadRequest = loadRequests[0];
					try
					{
						loadRequest.Complete();
					}
					catch (Exception ex)
					{
						Console.LogError("LOAD ERROR for load request: " + loadRequest.Path + " : " + ex.Message + "\nSite: " + ex.TargetSite);
						if (loadRequests.FirstOrDefault() == loadRequest)
						{
							loadRequests.RemoveAt(0);
						}
					}
				}
				yield return new WaitForEndOfFrame();
			}
			yield return new WaitForEndOfFrame();
			yield return new WaitForEndOfFrame();
			if (onLoadComplete != null)
			{
				onLoadComplete.Invoke();
			}
		}
		IEnumerator LoadRoutine()
		{
			bool playingTutorial = info.MetaData.PlayTutorial;
			if (Singleton<Lobby>.Instance.IsInLobby && Singleton<Lobby>.Instance.IsHost)
			{
				Console.Log("Sending host loading message to lobby");
				if (playingTutorial)
				{
					Singleton<Lobby>.Instance.SetLobbyData("load_tutorial", "true");
					Singleton<Lobby>.Instance.SendLobbyMessage("load_tutorial");
				}
				Singleton<Lobby>.Instance.SetLobbyData("host_loading", "true");
				Singleton<Lobby>.Instance.SendLobbyMessage("host_loading");
			}
			LoadStatus = ELoadStatus.LoadingScene;
			Singleton<LoadingScreen>.Instance.Open(playingTutorial);
			yield return new WaitForSecondsRealtime(1.25f);
			if (InstanceFinder.IsServer)
			{
				InstanceFinder.NetworkManager.ServerManager.StopConnection(sendDisconnectMessage: false);
			}
			if (InstanceFinder.IsClient)
			{
				InstanceFinder.NetworkManager.ClientManager.StopConnection();
			}
			if (onPreSceneChange != null)
			{
				onPreSceneChange.Invoke();
			}
			CleanUp();
			string sceneName = "Main";
			if (playingTutorial)
			{
				StoredSaveInfo = info;
				sceneName = "Tutorial";
				LoadedGameFolderPath = DefaultTutorialSaveFolder;
				InstanceFinder.NetworkManager.gameObject.GetComponent<DefaultScene>().SetOnlineScene("Tutorial");
			}
			else
			{
				StoredSaveInfo = null;
				if (InstanceFinder.NetworkManager != null)
				{
					InstanceFinder.NetworkManager.gameObject.GetComponent<DefaultScene>().SetOnlineScene("Main");
				}
			}
			AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
			while (!asyncLoad.isDone)
			{
				yield return new WaitForEndOfFrame();
			}
			Console.Log("Scene loaded: " + SceneManager.GetActiveScene().name);
			LoadStatus = ELoadStatus.Initializing;
			if (onPreLoad != null)
			{
				onPreLoad.Invoke();
			}
			Console.Log("Starting server");
			global::FishySteamworks.FishySteamworks fishy;
			ushort port;
			if (Singleton<Lobby>.Instance.IsInLobby && Singleton<Lobby>.Instance.IsHost)
			{
				fishy = InstanceFinder.TransportManager.GetTransport<Multipass>().GetTransport<global::FishySteamworks.FishySteamworks>();
				fishy.SetClientAddress(Singleton<Lobby>.Instance.LocalPlayerID.ToString());
				port = fishy.GetPort();
				fishy.OnServerConnectionState += Done;
				fishy.StartConnection(server: true);
			}
			else
			{
				Yak yak = InstanceFinder.TransportManager.GetTransport<Multipass>().GetTransport<Yak>();
				yak.SetPort(38465);
				yak.StartConnection(server: true);
				yield return new WaitUntil(() => InstanceFinder.IsServer);
				Console.Log("Server initialized");
				InstanceFinder.TransportManager.GetTransport<Multipass>().SetClientTransport(yak);
				yak.SetClientAddress("localhost");
				yak.StartConnection(server: false);
			}
			yield return new WaitUntil(() => InstanceFinder.NetworkManager.IsClient);
			Console.Log("Network initialized");
			LoadStatus = ELoadStatus.SpawningPlayer;
			yield return new WaitUntil(() => Player.Local != null);
			Console.Log("Local player spawned");
			LoadStatus = ELoadStatus.LoadingData;
			StartCoroutine(Load());
			yield return new WaitForSeconds(2f);
			LoadStatus = ELoadStatus.None;
			Console.Log("Game loaded");
			Singleton<LoadingScreen>.Instance.Close();
			IsLoading = false;
			IsGameLoaded = true;
			if (Singleton<Lobby>.Instance.IsInLobby && Singleton<Lobby>.Instance.IsHost)
			{
				Singleton<Lobby>.Instance.SetLobbyData("host_loading", "false");
				if (!playingTutorial)
				{
					Console.Log("Sending join ready message to lobby");
					Singleton<Lobby>.Instance.SetLobbyData("ready", "true");
					Singleton<Lobby>.Instance.SendLobbyMessage("ready");
				}
			}
			void Done(ServerConnectionStateArgs args)
			{
				Console.Log("Server connection state: " + args.ConnectionState.ToString() + " and transport index: " + args.TransportIndex);
				if (args.ConnectionState == LocalConnectionState.Started)
				{
					Console.Log("Server intialized");
					fishy.OnServerConnectionState -= Done;
					Console.Log("Starting FishySteamworks client connection: " + fishy.LocalUserSteamID);
					InstanceFinder.TransportManager.GetTransport<Multipass>().SetClientTransport<global::FishySteamworks.FishySteamworks>();
					InstanceFinder.NetworkManager.ClientManager.StartConnection(fishy.LocalUserSteamID.ToString(), port);
					InstanceFinder.TransportManager.Transport.SetTimeout(30f, asServer: true);
				}
			}
		}
	}

	public void LoadTutorialAsClient()
	{
		bool waitForExit = false;
		if (IsGameLoaded)
		{
			Console.LogWarning("Game already loaded, exiting");
			waitForExit = true;
			ExitToMenu();
		}
		StartCoroutine(LoadRoutine());
		IEnumerator Load()
		{
			Console.Log("Load start!");
			foreach (IBaseSaveable baseSaveable in Singleton<SaveManager>.Instance.BaseSaveables)
			{
				new LoadRequest(System.IO.Path.Combine(LoadedGameFolderPath, baseSaveable.SaveFolderName), baseSaveable.Loader);
			}
			while (loadRequests.Count > 0)
			{
				for (int i = 0; i < 50; i++)
				{
					if (loadRequests.Count <= 0)
					{
						break;
					}
					LoadRequest loadRequest = loadRequests[0];
					try
					{
						loadRequest.Complete();
					}
					catch (Exception ex)
					{
						Console.LogError("LOAD ERROR for load request: " + loadRequest.Path + " : " + ex.Message + "\nSite: " + ex.TargetSite);
						if (loadRequests.FirstOrDefault() == loadRequest)
						{
							loadRequests.RemoveAt(0);
						}
					}
				}
				yield return new WaitForEndOfFrame();
			}
			yield return new WaitForEndOfFrame();
			yield return new WaitForEndOfFrame();
			if (onLoadComplete != null)
			{
				onLoadComplete.Invoke();
			}
		}
		IEnumerator LoadRoutine()
		{
			if (waitForExit)
			{
				yield return new WaitUntil(() => !IsLoading && SceneManager.GetActiveScene().name == "Menu");
			}
			LoadHistory.Add("Loading as client to tutorial");
			ActiveSaveInfo = null;
			IsLoading = true;
			TimeSinceGameLoaded = 0f;
			LoadedGameFolderPath = string.Empty;
			LoadStatus = ELoadStatus.LoadingScene;
			Singleton<LoadingScreen>.Instance.Open(loadingTutorial: true);
			yield return new WaitForSecondsRealtime(1.25f);
			if (InstanceFinder.IsServer)
			{
				InstanceFinder.NetworkManager.ServerManager.StopConnection(sendDisconnectMessage: false);
			}
			if (InstanceFinder.IsClient)
			{
				InstanceFinder.NetworkManager.ClientManager.StopConnection();
			}
			if (onPreSceneChange != null)
			{
				onPreSceneChange.Invoke();
			}
			CleanUp();
			string sceneName = "Tutorial";
			LoadedGameFolderPath = DefaultTutorialSaveFolder;
			InstanceFinder.NetworkManager.gameObject.GetComponent<DefaultScene>().SetOnlineScene("Tutorial");
			AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
			while (!asyncLoad.isDone)
			{
				yield return new WaitForEndOfFrame();
			}
			Console.Log("Scene loaded: " + SceneManager.GetActiveScene().name);
			LoadStatus = ELoadStatus.Initializing;
			if (onPreLoad != null)
			{
				onPreLoad.Invoke();
			}
			Console.Log("Starting server");
			Yak yak = InstanceFinder.TransportManager.GetTransport<Multipass>().GetTransport<Yak>();
			yak.SetPort(38465);
			yak.StartConnection(server: true);
			yield return new WaitUntil(() => InstanceFinder.IsServer);
			Console.Log("Server initialized");
			InstanceFinder.TransportManager.GetTransport<Multipass>().SetClientTransport(yak);
			yak.SetClientAddress("localhost");
			yak.StartConnection(server: false);
			yield return new WaitUntil(() => InstanceFinder.NetworkManager.IsClient);
			Console.Log("Network initialized");
			LoadStatus = ELoadStatus.SpawningPlayer;
			yield return new WaitUntil(() => Player.Local != null);
			Console.Log("Local player spawned");
			LoadStatus = ELoadStatus.LoadingData;
			StartCoroutine(Load());
			yield return new WaitForSeconds(1f);
			LoadStatus = ELoadStatus.None;
			Console.Log("Game loaded");
			Singleton<LoadingScreen>.Instance.Close();
			IsLoading = false;
			IsGameLoaded = true;
		}
	}

	public void LoadAsClient(string steamId64)
	{
		bool waitForExit = false;
		if (IsGameLoaded)
		{
			Console.LogWarning("Game already loaded, exiting");
			waitForExit = true;
			ExitToMenu();
		}
		StartCoroutine(LoadRoutine());
		IEnumerator LoadRoutine()
		{
			if (waitForExit)
			{
				yield return new WaitUntil(() => !IsLoading && SceneManager.GetActiveScene().name == "Menu");
			}
			Console.Log("Joining as client to: " + steamId64);
			LoadHistory.Add("Loading as client to: " + steamId64);
			ActiveSaveInfo = null;
			IsLoading = true;
			TimeSinceGameLoaded = 0f;
			LoadedGameFolderPath = string.Empty;
			LoadStatus = ELoadStatus.LoadingScene;
			Singleton<LoadingScreen>.Instance.Open();
			StartLoadErrorAutosubmit();
			if (onPreSceneChange != null)
			{
				onPreSceneChange.Invoke();
			}
			CleanUp();
			InstanceFinder.TransportManager.GetTransport<Multipass>().SetClientTransport<global::FishySteamworks.FishySteamworks>();
			InstanceFinder.TransportManager.Transport.SetTimeout(30f, asServer: false);
			InstanceFinder.ClientManager.StartConnection(steamId64);
			Player.onLocalPlayerSpawned = (Action)Delegate.Combine(Player.onLocalPlayerSpawned, new Action(PlayerSpawned));
			yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "Main");
			Console.Log("Scene loaded: " + SceneManager.GetActiveScene().name);
			if (onPreLoad != null)
			{
				onPreLoad.Invoke();
			}
			LoadStatus = ELoadStatus.SpawningPlayer;
			yield return new WaitUntil(() => Player.Local != null);
			Console.Log("Local player spawned");
			LoadStatus = ELoadStatus.LoadingData;
			yield return new WaitUntil(() => Player.Local.playerDataRetrieveReturned);
			Console.Log("Player data retrieved");
			LoadStatus = ELoadStatus.Initializing;
			yield return new WaitForSeconds(2f);
			if (onLoadComplete != null)
			{
				onLoadComplete.Invoke();
			}
			LoadStatus = ELoadStatus.None;
			Console.Log("Game loaded as client");
			Singleton<LoadingScreen>.Instance.Close();
			IsLoading = false;
			IsGameLoaded = true;
		}
		static void PlayerSpawned()
		{
			Player.onLocalPlayerSpawned = (Action)Delegate.Remove(Player.onLocalPlayerSpawned, new Action(PlayerSpawned));
			Console.Log("Local player spawned");
		}
	}

	private void StartLoadErrorAutosubmit()
	{
		StartCoroutine(Wait());
		IEnumerator Wait()
		{
			for (float t = 0f; t < 90f; t += Time.deltaTime)
			{
				if (LoadStatus == ELoadStatus.None)
				{
					yield break;
				}
				yield return new WaitForEndOfFrame();
			}
			if (Singleton<PauseMenu>.InstanceExists)
			{
				Console.LogError("Load error timeout reached, submitting error report");
				Singleton<PauseMenu>.Instance.FeedbackForm.SetFormData("[AUTOREPORT] Load as client error");
				Singleton<PauseMenu>.Instance.FeedbackForm.SetCategory("Bugs - Multiplayer");
				Singleton<PauseMenu>.Instance.FeedbackForm.IncludeScreenshot = false;
				Singleton<PauseMenu>.Instance.FeedbackForm.IncludeSaveFile = false;
				Singleton<PauseMenu>.Instance.FeedbackForm.Submit();
			}
		}
	}

	public void SetWaitingForHostLoad()
	{
		IsLoading = true;
		LoadStatus = ELoadStatus.WaitingForHost;
	}

	public void LoadLastSave()
	{
		if (ActiveSaveInfo == null)
		{
			Console.LogWarning("No active save info, cannot load last save");
		}
		else
		{
			StartGame(ActiveSaveInfo, allowLoadStacking: true);
		}
	}

	private void CleanUp()
	{
		GUIDManager.Clear();
		Quest.Quests.Clear();
		Quest.ActiveQuests.Clear();
		NodeLink.validNodeLinks.Clear();
		Player.onLocalPlayerSpawned = null;
		Player.PlayerList.Clear();
		SupplierLocation.AllLocations.Clear();
		Phone.ActiveApp = null;
		ATM.WeeklyDepositSum = 0f;
		NavMeshUtility.ClearCache();
		Business.OwnedBusinesses.Clear();
		Business.UnownedBusinesses.Clear();
		ScheduleOne.Property.Property.OwnedProperties.Clear();
		ScheduleOne.Property.Property.UnownedProperties.Clear();
		PlayerMovement.StaticMoveSpeedMultiplier = 1f;
		Business.onOperationFinished = null;
		Business.onOperationStarted = null;
		ScheduleOne.Property.Property.onPropertyAcquired = null;
	}

	public void ExitToMenu(SaveInfo autoLoadSave = null, MainMenuPopup.Data mainMenuPopup = null, bool preventLeaveLobby = false)
	{
		if (!IsGameLoaded)
		{
			Console.LogWarning("Game not loaded, cannot exit to menu");
			return;
		}
		Console.Log("Exiting to menu");
		LoadHistory.Add("Exiting to menu");
		if (Player.Local != null && InstanceFinder.IsServer)
		{
			Player.Local.HostExitedGame();
		}
		if (Singleton<Lobby>.InstanceExists && Singleton<Lobby>.Instance.IsInLobby && !preventLeaveLobby)
		{
			Singleton<Lobby>.Instance.LeaveLobby();
		}
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
		IsGameLoaded = false;
		ActiveSaveInfo = null;
		IsLoading = true;
		Time.timeScale = 1f;
		Singleton<MusicPlayer>.Instance.StopAndDisableTracks();
		StartCoroutine(Load());
		IEnumerator Load()
		{
			Singleton<LoadingScreen>.Instance.Open();
			if (!InstanceFinder.IsServer)
			{
				Console.Log("Requesting server to save player data");
				Player.Local.RequestSavePlayer();
				float maxWait = 3f;
				float timeOnWaitStart = Time.realtimeSinceStartup;
				yield return new WaitUntil(() => Player.Local.playerSaveRequestReturned || Time.realtimeSinceStartup - timeOnWaitStart > maxWait);
				Console.Log("Player data saved");
			}
			yield return new WaitForSecondsRealtime(1.25f);
			try
			{
				if (onPreSceneChange != null)
				{
					onPreSceneChange.Invoke();
				}
			}
			catch (Exception ex)
			{
				Console.LogError("Error invoking pre scene change event: " + ex.Message);
			}
			Console.Log("Pre scene change event invoked");
			InstanceFinder.NetworkManager.ServerManager.StopConnection(sendDisconnectMessage: true);
			InstanceFinder.NetworkManager.ClientManager.StopConnection();
			Console.Log("Connection stopped");
			AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Menu");
			while (!asyncLoad.isDone)
			{
				yield return new WaitForEndOfFrame();
			}
			Console.Log("Menu scene loaded");
			bool flag = Singleton<Lobby>.Instance.IsInLobby && !Singleton<Lobby>.Instance.IsHost;
			if (autoLoadSave != null || flag)
			{
				if (Singleton<Lobby>.Instance.IsInLobby)
				{
					if (Singleton<Lobby>.Instance.IsHost)
					{
						IsLoading = false;
						StartGame(autoLoadSave);
						Console.Log("Disabling load_tutorial flag");
						Singleton<Lobby>.Instance.SetLobbyData("load_tutorial", "false");
					}
					else if (SteamMatchmaking.GetLobbyData(Singleton<Lobby>.Instance.LobbySteamID, "ready") == "true")
					{
						LoadAsClient(SteamMatchmaking.GetLobbyOwner(Singleton<Lobby>.Instance.LobbySteamID).m_SteamID.ToString());
					}
					else
					{
						SetWaitingForHostLoad();
					}
				}
				else
				{
					IsLoading = false;
					StartGame(autoLoadSave);
				}
			}
			else
			{
				RefreshSaveInfo();
				yield return new WaitForSeconds(0.5f);
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
				if (mainMenuPopup != null && Singleton<MainMenuPopup>.InstanceExists)
				{
					Singleton<MainMenuPopup>.Instance.Open(mainMenuPopup);
				}
				Singleton<LoadingScreen>.Instance.Close();
				IsLoading = false;
			}
		}
	}

	public static bool TryLoadSaveInfo(string saveFolderPath, int saveSlotIndex, out SaveInfo saveInfo, bool requireGameFile = false)
	{
		saveInfo = null;
		if (Directory.Exists(saveFolderPath))
		{
			string path = System.IO.Path.Combine(saveFolderPath, "Metadata.json");
			MetaData metaData = null;
			if (File.Exists(path))
			{
				string text = string.Empty;
				try
				{
					text = File.ReadAllText(path);
				}
				catch (Exception ex)
				{
					Console.LogError("Error reading save metadata: " + ex.Message);
				}
				if (!string.IsNullOrEmpty(text))
				{
					try
					{
						metaData = JsonUtility.FromJson<MetaData>(text);
					}
					catch (Exception ex2)
					{
						metaData = null;
						Console.LogError("Error parsing save metadata: " + ex2.Message);
					}
				}
				else
				{
					Console.LogWarning("Metadata is empty");
				}
			}
			string path2 = System.IO.Path.Combine(saveFolderPath, "Game.json");
			GameData gameData = null;
			if (File.Exists(path2))
			{
				string text2 = string.Empty;
				try
				{
					text2 = File.ReadAllText(path2);
				}
				catch (Exception ex3)
				{
					Console.LogError("Error reading save game data: " + ex3.Message);
				}
				if (!string.IsNullOrEmpty(text2))
				{
					try
					{
						gameData = JsonUtility.FromJson<GameData>(text2);
					}
					catch (Exception ex4)
					{
						gameData = null;
						Console.LogError("Error parsing save game data: " + ex4.Message);
					}
				}
				else
				{
					Console.LogWarning("Game data is empty");
				}
			}
			float networth = 0f;
			string path3 = System.IO.Path.Combine(saveFolderPath, "Money.json");
			MoneyData moneyData = null;
			if (File.Exists(path3))
			{
				string text3 = string.Empty;
				try
				{
					text3 = File.ReadAllText(path3);
				}
				catch (Exception ex5)
				{
					Console.LogError("Error reading save money data: " + ex5.Message);
				}
				if (!string.IsNullOrEmpty(text3))
				{
					try
					{
						moneyData = JsonUtility.FromJson<MoneyData>(text3);
					}
					catch (Exception ex6)
					{
						moneyData = null;
						Console.LogError("Error parsing save money data: " + ex6.Message);
					}
				}
				else
				{
					Console.LogWarning("Money data is empty");
				}
				if (moneyData != null)
				{
					networth = moneyData.Networth;
				}
			}
			if (metaData == null)
			{
				Console.LogWarning("Failed to load metadata. Setting default");
				metaData = new MetaData(new DateTimeData(DateTime.Now), new DateTimeData(DateTime.Now), Application.version, Application.version, playTutorial: false);
				try
				{
					File.WriteAllText(path, metaData.GetJson());
				}
				catch (Exception)
				{
				}
			}
			if (gameData == null)
			{
				if (requireGameFile)
				{
					return false;
				}
				Console.LogWarning("Failed to load game data. Setting default");
				gameData = new GameData("Unknown", UnityEngine.Random.Range(0, int.MaxValue), new GameSettings());
				try
				{
					File.WriteAllText(path2, gameData.GetJson());
				}
				catch (Exception)
				{
				}
			}
			saveInfo = new SaveInfo(saveFolderPath, saveSlotIndex, gameData.OrganisationName, metaData.CreationDate.GetDateTime(), metaData.LastPlayedDate.GetDateTime(), networth, metaData.LastSaveVersion, metaData);
			return true;
		}
		return false;
	}

	public void RefreshSaveInfo()
	{
		for (int i = 0; i < 5; i++)
		{
			SaveGames[i] = null;
			if (TryLoadSaveInfo(System.IO.Path.Combine(Singleton<SaveManager>.Instance.IndividualSavesContainerPath, "SaveGame_" + (i + 1)), i + 1, out var saveInfo))
			{
				SaveGames[i] = saveInfo;
			}
			else
			{
				SaveGames[i] = null;
			}
		}
		LastPlayedGame = null;
		for (int j = 0; j < SaveGames.Length; j++)
		{
			if (SaveGames[j] != null && (LastPlayedGame == null || SaveGames[j].DateLastPlayed > LastPlayedGame.DateLastPlayed))
			{
				LastPlayedGame = SaveGames[j];
			}
		}
		if (onSaveInfoLoaded != null)
		{
			onSaveInfoLoaded.Invoke();
		}
	}
}
