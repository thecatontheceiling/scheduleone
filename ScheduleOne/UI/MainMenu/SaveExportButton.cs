using System.IO;
using System.IO.Compression;
using SFB;
using ScheduleOne.Persistence;
using UnityEngine;
using UnityEngine.UI;

namespace ScheduleOne.UI.MainMenu;

[RequireComponent(typeof(Button))]
public class SaveExportButton : MonoBehaviour
{
	public int SaveSlotIndex;

	private void Awake()
	{
		GetComponent<Button>().onClick.AddListener(Clicked);
	}

	private void Clicked()
	{
		(new ExtensionFilter[1])[0] = new ExtensionFilter("Zip Files", "zip");
		SaveInfo saveInfo = LoadManager.SaveGames[SaveSlotIndex];
		string text = ShowSaveFileDialog(SaveManager.MakeFileSafe(saveInfo.OrganisationName));
		if (!string.IsNullOrEmpty(text))
		{
			Console.Log("Exporting save file to: " + text);
			ZipSaveFolder(saveInfo.SavePath, text);
			Debug.Log("Save exported to: " + text);
		}
	}

	public static string ShowSaveFileDialog(string fileName)
	{
		ExtensionFilter[] extensions = new ExtensionFilter[1]
		{
			new ExtensionFilter("Zip Files", "zip")
		};
		return StandaloneFileBrowser.SaveFilePanel("Export Save File", "", fileName + ".zip", extensions);
	}

	public static void ZipSaveFolder(string sourceFolderPath, string destinationZipPath)
	{
		if (File.Exists(destinationZipPath))
		{
			File.Delete(destinationZipPath);
		}
		ZipFile.CreateFromDirectory(sourceFolderPath, destinationZipPath, System.IO.Compression.CompressionLevel.Optimal, includeBaseDirectory: true);
	}
}
