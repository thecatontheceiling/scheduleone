using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using AeLa.EasyFeedback;
using AeLa.EasyFeedback.FormElements;
using AeLa.EasyFeedback.Utility;
using ScheduleOne.DevUtilities;
using ScheduleOne.Networking;
using ScheduleOne.Persistence;
using ScheduleOne.PlayerScripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScheduleOne.UI;

public class FeedbackForm : AeLa.EasyFeedback.FeedbackForm
{
	private Coroutine ssCoroutine;

	public CanvasGroup CanvasGroup;

	public Toggle ScreenshotToggle;

	public Toggle SaveFileToggle;

	public TMP_InputField SummaryField;

	public TMP_InputField DescriptionField;

	public RectTransform Cog;

	public TMP_Dropdown CategoryDropdown;

	public override void Awake()
	{
		base.Awake();
		ScreenshotToggle.SetIsOnWithoutNotify(IncludeScreenshot);
		ScreenshotToggle.onValueChanged.AddListener(OnScreenshotToggle);
		SaveFileToggle.SetIsOnWithoutNotify(IncludeSaveFile);
		SaveFileToggle.onValueChanged.AddListener(OnSaveFileToggle);
		OnSubmissionSucceeded.AddListener(Clear);
	}

	private void Update()
	{
		Cog.localEulerAngles += new Vector3(0f, 0f, -180f * Time.unscaledDeltaTime);
	}

	public void PrepScreenshot()
	{
		CurrentReport = new Report();
	}

	private void OnScreenshotToggle(bool value)
	{
		IncludeScreenshot = value;
	}

	private void OnSaveFileToggle(bool value)
	{
		IncludeSaveFile = value;
	}

	public void SetFormData(string title)
	{
		if (CurrentReport == null)
		{
			CurrentReport = new Report();
		}
		CurrentReport.Title = title;
		GetComponentInChildren<ReportTitle>().GetComponent<TMP_InputField>().SetTextWithoutNotify(title);
	}

	public void SetCategory(string categoryName)
	{
		for (int i = 0; i < Config.Board.CategoryNames.Length; i++)
		{
			if (Config.Board.CategoryNames[i].Contains(categoryName))
			{
				CategoryDropdown.SetValueWithoutNotify(i + 1);
				return;
			}
		}
		Console.LogWarning("Category not found: " + categoryName);
	}

	public override void Submit()
	{
		if (IncludeScreenshot)
		{
			PlayerSingleton<PlayerCamera>.Instance.SetDoFActive(active: false, 0f);
			CanvasGroup.alpha = 0f;
			ssCoroutine = Singleton<CoroutineService>.Instance.StartCoroutine(ScreenshotAndOpenForm());
			Singleton<CoroutineService>.Instance.StartCoroutine(Wait());
		}
		if (File.Exists(Application.persistentDataPath + "/Player-prev.log"))
		{
			try
			{
				byte[] data = File.ReadAllBytes(Application.persistentDataPath + "/Player-prev.log");
				CurrentReport.AttachFile("Player-prev.txt", data);
			}
			catch (Exception ex)
			{
				Console.LogError("Failed to attach Player-prev.txt: " + ex.Message);
			}
		}
		if (IncludeSaveFile)
		{
			string loadedGameFolderPath = Singleton<LoadManager>.Instance.LoadedGameFolderPath;
			string text = loadedGameFolderPath + ".zip";
			try
			{
				if (File.Exists(text))
				{
					Console.Log("Deleting prior zip file: " + text);
					File.Delete(text);
				}
				ZipFile.CreateFromDirectory(loadedGameFolderPath, text, System.IO.Compression.CompressionLevel.Optimal, includeBaseDirectory: true);
				byte[] data2 = File.ReadAllBytes(text);
				CurrentReport.AttachFile("SaveGame.zip", data2);
			}
			catch (Exception ex2)
			{
				Console.LogError("Failed to attach save file: " + ex2.Message);
			}
			finally
			{
				if (File.Exists(text))
				{
					File.Delete(text);
				}
			}
		}
		if (Player.Local != null)
		{
			Report currentReport = CurrentReport;
			currentReport.Title = currentReport.Title + " (" + Player.Local.PlayerName + ")";
		}
		CurrentReport.AddSection("Game Info", 2);
		string text2 = "Singleplayer";
		if (Singleton<Lobby>.InstanceExists && Singleton<Lobby>.Instance.IsInLobby)
		{
			text2 = "Multiplayer";
			text2 = ((!Singleton<Lobby>.Instance.IsHost) ? (text2 + " (Client)") : (text2 + " (Host)"));
		}
		CurrentReport["Game Info"].AppendLine("Network Mode: " + text2);
		CurrentReport["Game Info"].AppendLine("Player Count: " + Player.PlayerList.Count);
		CurrentReport["Game Info"].AppendLine("Beta Branch: " + GameManager.IS_BETA);
		CurrentReport["Game Info"].AppendLine("Is Demo: " + false);
		CurrentReport["Game Info"].AppendLine("Load History: " + string.Join(", ", LoadManager.LoadHistory));
		Singleton<CoroutineService>.Instance.StartCoroutine(SubmitAsync());
		base.Submit();
		IEnumerator Wait()
		{
			yield return new WaitForEndOfFrame();
			PlayerSingleton<PlayerCamera>.Instance.SetDoFActive(active: true, 0f);
			CanvasGroup.alpha = 1f;
		}
	}

	protected override string GetTextToAppendToTitle()
	{
		string textToAppendToTitle = base.GetTextToAppendToTitle();
		textToAppendToTitle = textToAppendToTitle + " (" + Application.version + ")";
		if (Player.Local != null)
		{
			textToAppendToTitle = textToAppendToTitle + " (" + Player.Local.PlayerName + ")";
		}
		return textToAppendToTitle;
	}

	private void Clear()
	{
		SummaryField.SetTextWithoutNotify(string.Empty);
		DescriptionField.SetTextWithoutNotify(string.Empty);
	}

	private IEnumerator ScreenshotAndOpenForm()
	{
		if (IncludeScreenshot)
		{
			yield return ScreenshotUtil.CaptureScreenshot(ScreenshotCaptureMode, ResizeLargeScreenshots, delegate(byte[] ss)
			{
				CurrentReport.AttachFile("screenshot.png", ss);
			}, delegate(string err)
			{
				OnSubmissionError.Invoke(err);
			});
		}
		EnableForm();
		Form.gameObject.SetActive(value: true);
		OnFormOpened.Invoke();
		ssCoroutine = null;
	}
}
