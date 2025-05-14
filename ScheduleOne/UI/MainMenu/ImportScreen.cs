using System.IO;
using ScheduleOne.DevUtilities;
using ScheduleOne.Money;
using ScheduleOne.Persistence;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScheduleOne.UI.MainMenu;

public class ImportScreen : MainMenuScreen
{
	[Header("References")]
	public GameObject MainContainer;

	public GameObject FailContainer;

	public Button ConfirmButton;

	public TextMeshProUGUI OrganisationNameLabel;

	public TextMeshProUGUI NetworthLabel;

	public TextMeshProUGUI VersionLabel;

	public TextMeshProUGUI WarningLabel;

	private int slotToOverwrite;

	private SaveInfo saveInfo;

	public void Initialize(int _slotToOverwrite, MainMenuScreen previousScreen)
	{
		slotToOverwrite = _slotToOverwrite;
		PreviousScreen = previousScreen;
		bool flag = false;
		string tempImportPath = SaveImportButton.TempImportPath;
		string[] directories = Directory.GetDirectories(tempImportPath);
		if (directories.Length != 0)
		{
			string fileName = Path.GetFileName(directories[0]);
			string text = Path.Combine(tempImportPath, fileName);
			if (LoadManager.TryLoadSaveInfo(text, -1, out var saveInfo, requireGameFile: true))
			{
				Console.Log("Loaded save info from: " + text);
				this.saveInfo = saveInfo;
				flag = true;
				OrganisationNameLabel.text = saveInfo.OrganisationName;
				NetworthLabel.text = MoneyManager.FormatAmount(saveInfo.Networth);
				VersionLabel.text = "v" + saveInfo.SaveVersion;
				if (LoadManager.SaveGames[slotToOverwrite] != null)
				{
					WarningLabel.text = "Warning: This will overwrite the current save in slot " + (slotToOverwrite + 1) + " (" + LoadManager.SaveGames[slotToOverwrite].OrganisationName + ").";
					WarningLabel.enabled = true;
				}
				else
				{
					WarningLabel.enabled = false;
				}
			}
		}
		ConfirmButton.interactable = flag;
		MainContainer.SetActive(flag);
		FailContainer.SetActive(!flag);
	}

	public void Cancel()
	{
		Close(openPrevious: true);
	}

	public void Confirm()
	{
		if (saveInfo == null)
		{
			Console.LogError("No save info found to import.");
			return;
		}
		string text = Path.Combine(Singleton<SaveManager>.Instance.IndividualSavesContainerPath, "SaveGame_" + (slotToOverwrite + 1));
		string savePath = saveInfo.SavePath;
		if (Directory.Exists(text))
		{
			Directory.Delete(text, recursive: true);
		}
		Directory.CreateDirectory(text);
		CopyFilesRecursively(savePath, text);
		string tempImportPath = SaveImportButton.TempImportPath;
		if (Directory.Exists(tempImportPath))
		{
			Directory.Delete(tempImportPath, recursive: true);
		}
		Singleton<LoadManager>.Instance.RefreshSaveInfo();
		Close(openPrevious: true);
	}

	private static void CopyFilesRecursively(string sourcePath, string targetPath)
	{
		string[] directories = Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories);
		for (int i = 0; i < directories.Length; i++)
		{
			Directory.CreateDirectory(directories[i].Replace(sourcePath, targetPath));
		}
		directories = Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories);
		foreach (string text in directories)
		{
			if (!text.EndsWith(".meta"))
			{
				File.Copy(text, text.Replace(sourcePath, targetPath), overwrite: true);
			}
		}
	}
}
