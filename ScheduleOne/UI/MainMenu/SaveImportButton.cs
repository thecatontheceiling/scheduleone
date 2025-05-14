using System.IO;
using System.IO.Compression;
using System.Linq;
using SFB;
using ScheduleOne.DevUtilities;
using ScheduleOne.Persistence;
using UnityEngine;
using UnityEngine.UI;

namespace ScheduleOne.UI.MainMenu;

[RequireComponent(typeof(Button))]
public class SaveImportButton : MonoBehaviour
{
	public ImportScreen ImportScreen;

	public MainMenuScreen ParentScreen;

	public int SaveSlotIndex;

	public static string TempImportPath => Path.Combine(Singleton<SaveManager>.Instance.IndividualSavesContainerPath, "TempImport");

	private void Awake()
	{
		GetComponent<Button>().onClick.AddListener(Clicked);
	}

	private void Clicked()
	{
		(new ExtensionFilter[1])[0] = new ExtensionFilter("Zip Files", "zip");
		string text = ShowOpenFileDialog();
		if (!string.IsNullOrEmpty(text))
		{
			string tempImportPath = TempImportPath;
			if (Directory.Exists(tempImportPath))
			{
				Directory.Delete(tempImportPath, recursive: true);
			}
			UnzipSaveFile(text, tempImportPath);
			ImportScreen.Initialize(SaveSlotIndex, ParentScreen);
			ImportScreen.Open(closePrevious: true);
		}
	}

	public static void UnzipSaveFile(string zipFilePath, string destinationFolderPath)
	{
		Console.Log("Unzipping from " + zipFilePath + " to " + destinationFolderPath);
		if (Directory.Exists(destinationFolderPath))
		{
			Directory.Delete(destinationFolderPath, recursive: true);
		}
		Directory.CreateDirectory(destinationFolderPath);
		ZipFile.ExtractToDirectory(zipFilePath, destinationFolderPath);
	}

	public static string ShowOpenFileDialog()
	{
		ExtensionFilter[] extensions = new ExtensionFilter[1]
		{
			new ExtensionFilter("Zip Files", "zip")
		};
		return StandaloneFileBrowser.OpenFilePanel("Import Save File", "", extensions, multiselect: false).FirstOrDefault();
	}
}
