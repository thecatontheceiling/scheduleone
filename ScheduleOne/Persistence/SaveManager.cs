using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using FishNet;
using ScheduleOne.DevUtilities;
using ScheduleOne.Persistence.Datas;
using ScheduleOne.UI;
using Steamworks;
using UnityEngine;
using UnityEngine.Events;

namespace ScheduleOne.Persistence;

public class SaveManager : PersistentSingleton<SaveManager>
{
	public const string MAIN_SCENE_NAME = "Main";

	public const string MENU_SCENE_NAME = "Menu";

	public const string TUTORIAL_SCENE_NAME = "Tutorial";

	public const int SAVES_PER_FRAME = 15;

	public const string SAVE_FILE_EXTENSION = ".json";

	public const int SAVE_SLOT_COUNT = 5;

	public const string SAVE_GAME_PREFIX = "SaveGame_";

	public const bool DEBUG = false;

	public const bool PRETTY_PRINT = true;

	public static bool SaveError;

	public List<ISaveable> Saveables = new List<ISaveable>();

	public List<IBaseSaveable> BaseSaveables = new List<IBaseSaveable>();

	[HideInInspector]
	public List<string> ApprovedBaseLevelPaths = new List<string>();

	protected List<ISaveable> CompletedSaveables = new List<ISaveable>();

	protected List<SaveRequest> QueuedSaveRequests = new List<SaveRequest>();

	[Header("References")]
	public RectTransform WriteIssueDisplay;

	[Header("Events")]
	public UnityEvent onSaveStart;

	public UnityEvent onSaveComplete;

	private bool saveFolderInitialized;

	public bool AccessPermissionIssueDetected { get; protected set; }

	public bool IsSaving { get; protected set; }

	public float SecondsSinceLastSave { get; protected set; }

	public string PlayersSavePath { get; protected set; } = string.Empty;

	public string IndividualSavesContainerPath { get; protected set; } = string.Empty;

	public string SaveName { get; protected set; } = "DevSave";

	public static void ReportSaveError()
	{
		SaveError = true;
	}

	protected override void Awake()
	{
		base.Awake();
		if (Singleton<SaveManager>.Instance == null || Singleton<SaveManager>.Instance != this)
		{
			return;
		}
		PlayersSavePath = Path.Combine(Application.persistentDataPath, "Saves");
		if (!Directory.Exists(PlayersSavePath))
		{
			Directory.CreateDirectory(PlayersSavePath);
		}
		if (Directory.GetDirectories(PlayersSavePath).Length == 0)
		{
			string path = Path.Combine(PlayersSavePath, "TempPlayer");
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}
		}
		string[] directories = Directory.GetDirectories(PlayersSavePath);
		if (directories.Length > 1)
		{
			for (int i = 0; i < directories.Length; i++)
			{
				if (!directories[i].Contains("TempPlayer"))
				{
					IndividualSavesContainerPath = directories[i];
					break;
				}
			}
		}
		else
		{
			IndividualSavesContainerPath = directories[0];
		}
	}

	protected override void Start()
	{
		base.Start();
		Singleton<LoadManager>.Instance.onPreSceneChange.AddListener(Clean);
		CheckSaveFolderInitialized();
	}

	public void CheckSaveFolderInitialized()
	{
		if (saveFolderInitialized)
		{
			return;
		}
		saveFolderInitialized = true;
		if (SteamManager.Initialized)
		{
			string path = SteamUser.GetSteamID().ToString();
			string text = Path.Combine(PlayersSavePath, path);
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			IndividualSavesContainerPath = text;
			Console.Log("Initialized individual save folder path: " + IndividualSavesContainerPath);
		}
		else
		{
			Console.LogError("Steamworks not intialized in time for SaveManager! Using save container path: " + IndividualSavesContainerPath);
		}
		if (HasWritePermissionOnDir(IndividualSavesContainerPath))
		{
			AccessPermissionIssueDetected = false;
			Console.Log("Successfully verified write permission on save folder: " + IndividualSavesContainerPath);
			if (WriteIssueDisplay != null)
			{
				WriteIssueDisplay.gameObject.SetActive(value: false);
			}
		}
		else
		{
			AccessPermissionIssueDetected = true;
			Console.LogError("No write permission on save folder: " + IndividualSavesContainerPath);
			if (WriteIssueDisplay != null)
			{
				WriteIssueDisplay.gameObject.SetActive(value: true);
			}
		}
	}

	public static bool HasWritePermissionOnDir(string path)
	{
		bool result = false;
		string path2 = Path.Combine(path, "WriteTest.txt");
		if (Directory.Exists(path))
		{
			try
			{
				File.WriteAllText(path2, "If you're reading this, it means Schedule I can write save files properly - Yay!");
				if (File.Exists(path2))
				{
					result = true;
				}
			}
			catch (Exception)
			{
				result = false;
			}
		}
		return result;
	}

	private void Update()
	{
		if (Singleton<LoadManager>.Instance.IsGameLoaded && Singleton<LoadManager>.Instance.LoadedGameFolderPath != string.Empty && Input.GetKeyDown(KeyCode.F5) && (Application.isEditor || Debug.isDebugBuild))
		{
			Save();
		}
		if (Singleton<LoadManager>.Instance.IsGameLoaded)
		{
			SecondsSinceLastSave += Time.unscaledDeltaTime;
		}
		else
		{
			SecondsSinceLastSave = 0f;
		}
	}

	public void DelayedSave()
	{
		Invoke("Save", 1f);
	}

	public void Save()
	{
		Save(Singleton<LoadManager>.Instance.LoadedGameFolderPath);
	}

	public void Save(string saveFolderPath)
	{
		if (!InstanceFinder.IsServer)
		{
			return;
		}
		if (Singleton<LoadManager>.Instance.LoadedGameFolderPath == string.Empty)
		{
			Console.LogWarning("No game loaded to save");
			return;
		}
		if (IsSaving)
		{
			Console.LogWarning("Save called while saving is already in progress");
			return;
		}
		if (NetworkSingleton<GameManager>.Instance.IsTutorial && !Application.isEditor)
		{
			Console.LogWarning("Can't save during tutorial");
			return;
		}
		if (NetworkSingleton<GameManager>.Instance.IsTutorial)
		{
			saveFolderPath = Path.Combine(Singleton<SaveManager>.Instance.IndividualSavesContainerPath, "DevSave");
		}
		Console.Log("Saving game to " + saveFolderPath);
		IsSaving = true;
		if (onSaveStart != null)
		{
			onSaveStart.Invoke();
		}
		CompletedSaveables.Clear();
		ApprovedBaseLevelPaths.Clear();
		ApprovedBaseLevelPaths.Add("Products");
		SaveError = false;
		StartCoroutine(SaveRoutine());
		IEnumerator SaveRoutine()
		{
			for (int i = 0; i < BaseSaveables.Count; i++)
			{
				string text = string.Empty;
				try
				{
					ApprovedBaseLevelPaths.Add(BaseSaveables[i].GetLocalPath(out var _));
					text = BaseSaveables[i].Save(saveFolderPath);
				}
				catch (Exception ex)
				{
					Console.LogError("Failed to save base saveable: " + BaseSaveables[i]?.ToString() + "\nException: " + ex);
					SaveError = true;
				}
				if (!string.IsNullOrEmpty(text))
				{
					ApprovedBaseLevelPaths.Add(text);
				}
			}
			while (CompletedSaveables.Count < Saveables.Count && QueuedSaveRequests.Count > 0)
			{
				List<SaveRequest> range = QueuedSaveRequests.GetRange(0, Mathf.Min(15, QueuedSaveRequests.Count));
				for (int j = 0; j < range.Count; j++)
				{
					try
					{
						range[j].Complete();
					}
					catch (Exception ex2)
					{
						Console.LogError("Error completing save request for " + range[j].Saveable.SaveFileName + ": " + ex2.Message);
						SaveError = true;
					}
					if (j < range.Count)
					{
						if (QueuedSaveRequests.Contains(range[j]))
						{
							QueuedSaveRequests.Remove(range[j]);
						}
						if (!CompletedSaveables.Contains(range[j].Saveable))
						{
							CompletedSaveables.Add(range[j].Saveable);
						}
					}
				}
				yield return new WaitForEndOfFrame();
			}
			ClearBaseLevelOutdatedSaves(saveFolderPath);
			if (QueuedSaveRequests.Count > 0)
			{
				Console.LogWarning("There are still save requests in the queue after the save cycle has completed.");
				CompletedSaveables.Clear();
			}
			IsSaving = false;
			SecondsSinceLastSave = 0f;
			if (onSaveComplete != null)
			{
				onSaveComplete.Invoke();
			}
			if (!SaveError)
			{
				Console.Log("Save complete!");
			}
			else
			{
				Console.LogError("Save completed with errors! Auto-submitting bug report.");
				if (Singleton<PauseMenu>.InstanceExists)
				{
					Singleton<PauseMenu>.Instance.FeedbackForm.SetFormData("[AUTOREPORT] Error during save");
					Singleton<PauseMenu>.Instance.FeedbackForm.SetCategory("Bugs - Saving/Loading");
					Singleton<PauseMenu>.Instance.FeedbackForm.IncludeScreenshot = false;
					Singleton<PauseMenu>.Instance.FeedbackForm.IncludeSaveFile = false;
					Singleton<PauseMenu>.Instance.FeedbackForm.Submit();
				}
			}
		}
	}

	private void ClearBaseLevelOutdatedSaves(string saveFolderPath)
	{
		string[] array = null;
		string[] array2 = null;
		try
		{
			array = Directory.GetFiles(saveFolderPath);
		}
		catch (Exception ex)
		{
			Console.LogError("Failed to get files in folder: " + saveFolderPath + "\nException: " + ex);
			return;
		}
		try
		{
			array2 = Directory.GetDirectories(saveFolderPath);
		}
		catch (Exception ex2)
		{
			Console.LogError("Failed to get folders in folder: " + saveFolderPath + "\nException: " + ex2);
			return;
		}
		if (array == null || array2 == null)
		{
			Console.LogError("Failed to get files or folders in folder: " + saveFolderPath);
			return;
		}
		string[] array3 = array;
		foreach (string text in array3)
		{
			FileInfo fileInfo = new FileInfo(text);
			if (!ApprovedBaseLevelPaths.Contains(fileInfo.Name))
			{
				try
				{
					Debug.Log("Deleting file: " + text);
					File.Delete(text);
				}
				catch (Exception ex3)
				{
					Console.LogError("Failed to delete file: " + text + "\nException: " + ex3);
				}
			}
		}
		array3 = array2;
		foreach (string text2 in array3)
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(text2);
			if (!ApprovedBaseLevelPaths.Contains(directoryInfo.Name))
			{
				try
				{
					Debug.Log("Deleting folder: " + text2);
					Directory.Delete(text2, recursive: true);
				}
				catch (Exception ex4)
				{
					Console.LogError("Failed to delete folder: " + text2 + "\nException: " + ex4);
				}
			}
		}
	}

	public void CompleteSaveable(ISaveable saveable)
	{
		if (CompletedSaveables.Contains(saveable))
		{
			Console.LogWarning("Saveable already completed");
		}
		else
		{
			CompletedSaveables.Add(saveable);
		}
	}

	public void ClearCompletedSaveable(ISaveable saveable)
	{
		CompletedSaveables.Remove(saveable);
	}

	public void RegisterSaveable(ISaveable saveable)
	{
		if (!Saveables.Contains(saveable))
		{
			Saveables.Add(saveable);
			if (saveable is IBaseSaveable)
			{
				BaseSaveables.Add(saveable as IBaseSaveable);
			}
		}
	}

	public void QueueSaveRequest(SaveRequest request)
	{
		QueuedSaveRequests.Add(request);
	}

	public void DequeueSaveRequest(SaveRequest request)
	{
		QueuedSaveRequests.Remove(request);
	}

	public static string StripExtensions(string filePath)
	{
		return filePath.Replace(".json", string.Empty);
	}

	public static string MakeFileSafe(string fileName)
	{
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		foreach (char oldChar in invalidFileNameChars)
		{
			fileName = fileName.Replace(oldChar, '-');
		}
		return fileName;
	}

	public static float GetVersionNumber(string version)
	{
		version.ToLower().Contains("alternate");
		version = version.Replace(".", string.Empty);
		version = version.Replace("f", ".");
		version = Regex.Replace(version, "[^\\d.]", string.Empty);
		version = version.TrimStart('0');
		if (!float.TryParse(version, out var result))
		{
			Console.LogError("Failed to parse version number: " + version);
			return 0f;
		}
		return result;
	}

	private void Clean()
	{
		Saveables.Clear();
		BaseSaveables.Clear();
	}

	public void DisablePlayTutorial(SaveInfo info)
	{
		string path = Path.Combine(info.SavePath, "Metadata.json");
		MetaData metaData = null;
		if (File.Exists(path))
		{
			string empty = string.Empty;
			try
			{
				empty = File.ReadAllText(path);
			}
			catch (Exception ex)
			{
				Console.LogError("Error reading save metadata: " + ex.Message);
				return;
			}
			metaData = JsonUtility.FromJson<MetaData>(empty);
			metaData.PlayTutorial = false;
			try
			{
				File.WriteAllText(path, metaData.GetJson());
				Console.Log("Successfully disabled tutorial in metadata file");
			}
			catch (Exception ex2)
			{
				Console.LogError("Failed to modify metadata file. Exception: " + ex2);
			}
		}
	}

	public static string SanitizeFileName(string fileName)
	{
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		foreach (char oldChar in invalidFileNameChars)
		{
			fileName = fileName.Replace(oldChar, '_');
		}
		return fileName;
	}
}
