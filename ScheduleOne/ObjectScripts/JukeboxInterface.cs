using System;
using System.Collections.Generic;
using ScheduleOne.DevUtilities;
using ScheduleOne.Interaction;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScheduleOne.ObjectScripts;

public class JukeboxInterface : MonoBehaviour
{
	public const float OPEN_TIME = 0.15f;

	[Header("References")]
	public Jukebox Jukebox;

	public Canvas Canvas;

	public Transform CameraPosition;

	public InteractableObject IntObj;

	public Image PausePlayImage;

	public Button ShuffleButton;

	public Button RepeatButton;

	public Button SyncButton;

	public RectTransform EntryContainer;

	public GameObject AmbientDisplayContainer;

	public TextMeshPro AmbientDisplaySongLabel;

	public TextMeshPro AmbientDisplayTimeLabel;

	[Header("Settings")]
	public Sprite PlaySprite;

	public Sprite PauseSprite;

	public Sprite SongEntryPlaySprite;

	public Sprite SongEntryPauseSprite;

	public Sprite RepeatModeSprite_None;

	public Sprite RepeatModeSprite_Track;

	public Sprite RepeatModeSprite_Queue;

	public Color DeselectedColor;

	public Color SelectedColor;

	public GameObject SongEntryPrefab;

	private List<RectTransform> songEntries = new List<RectTransform>();

	public bool IsOpen { get; private set; }

	private void Awake()
	{
		Canvas.enabled = false;
		Jukebox jukebox = Jukebox;
		jukebox.onStateChanged = (Action)Delegate.Combine(jukebox.onStateChanged, new Action(RefreshUI));
		Jukebox jukebox2 = Jukebox;
		jukebox2.onStateChanged = (Action)Delegate.Combine(jukebox2.onStateChanged, new Action(RefreshSongEntries));
		Jukebox jukebox3 = Jukebox;
		jukebox3.onStateChanged = (Action)Delegate.Combine(jukebox3.onStateChanged, new Action(RefreshAmbientDisplay));
		SetupSongEntries();
		RefreshUI();
	}

	private void FixedUpdate()
	{
		UpdateAmbientDisplay();
	}

	private void UpdateAmbientDisplay()
	{
		AmbientDisplaySongLabel.text = Jukebox.currentTrack.TrackName;
		float currentTrackTime = Jukebox.CurrentTrackTime;
		float length = Jukebox.currentTrack.Clip.length;
		int num = Mathf.FloorToInt(currentTrackTime / 60f);
		int num2 = Mathf.FloorToInt(currentTrackTime % 60f);
		int num3 = Mathf.FloorToInt(length / 60f);
		int num4 = Mathf.FloorToInt(length % 60f);
		AmbientDisplayTimeLabel.text = $"{num:D2}:{num2:D2} / {num3:D2}:{num4:D2}";
	}

	private void SetupSongEntries()
	{
		Jukebox.Track[] trackList = Jukebox.TrackList;
		foreach (Jukebox.Track track in trackList)
		{
			GameObject entry = UnityEngine.Object.Instantiate(SongEntryPrefab, EntryContainer);
			entry.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = track.TrackName;
			entry.transform.Find("Artist").GetComponent<TextMeshProUGUI>().text = track.ArtistName;
			entry.transform.SetAsLastSibling();
			entry.transform.Find("PlayPause").GetComponent<Button>().onClick.AddListener(delegate
			{
				SongEntryClicked(entry.GetComponent<RectTransform>());
			});
			songEntries.Add(entry.GetComponent<RectTransform>());
		}
	}

	public void Start()
	{
		GameInput.RegisterExitListener(Exit, 2);
		IntObj.onHovered.AddListener(Hovered);
		IntObj.onInteractStart.AddListener(Interacted);
	}

	private void OnDestroy()
	{
		GameInput.DeregisterExitListener(Exit);
	}

	private void Exit(ExitAction action)
	{
		if (!action.Used && IsOpen)
		{
			action.Used = true;
			Close();
		}
	}

	public void Open()
	{
		RefreshUI();
		RefreshSongEntries();
		IsOpen = true;
		Canvas.enabled = true;
		PlayerSingleton<PlayerCamera>.Instance.AddActiveUIElement(base.name);
		PlayerSingleton<PlayerCamera>.Instance.OverrideTransform(CameraPosition.position, CameraPosition.rotation, 0.15f);
		PlayerSingleton<PlayerCamera>.Instance.OverrideFOV(60f, 0.15f);
		PlayerSingleton<PlayerCamera>.Instance.SetCanLook(c: false);
		PlayerSingleton<PlayerCamera>.Instance.FreeMouse();
		PlayerSingleton<PlayerMovement>.Instance.canMove = false;
		PlayerSingleton<PlayerInventory>.Instance.SetEquippingEnabled(enabled: false);
		Singleton<HUD>.Instance.SetCrosshairVisible(vis: false);
		RefreshAmbientDisplay();
	}

	public void Close()
	{
		IsOpen = false;
		Canvas.enabled = false;
		PlayerSingleton<PlayerCamera>.Instance.RemoveActiveUIElement(base.name);
		PlayerSingleton<PlayerCamera>.Instance.StopTransformOverride(0.15f);
		PlayerSingleton<PlayerCamera>.Instance.StopFOVOverride(0.15f);
		PlayerSingleton<PlayerCamera>.Instance.LockMouse();
		PlayerSingleton<PlayerMovement>.Instance.canMove = true;
		PlayerSingleton<PlayerInventory>.Instance.SetEquippingEnabled(enabled: true);
		Singleton<HUD>.Instance.SetCrosshairVisible(vis: true);
		RefreshAmbientDisplay();
	}

	private void Hovered()
	{
		if (!IsOpen)
		{
			IntObj.SetInteractableState(InteractableObject.EInteractableState.Default);
			IntObj.SetMessage("Use jukebox");
		}
		else
		{
			IntObj.SetInteractableState(InteractableObject.EInteractableState.Disabled);
		}
	}

	private void Interacted()
	{
		if (!IsOpen)
		{
			Open();
		}
	}

	public void PlayPausePressed()
	{
		Jukebox.TogglePlay();
	}

	public void BackPressed()
	{
		Jukebox.Back();
	}

	public void NextPressed()
	{
		Jukebox.Next();
	}

	public void ShufflePressed()
	{
		Jukebox.ToggleShuffle();
	}

	public void RepeatPressed()
	{
		Jukebox.ToggleRepeatMode();
	}

	public void SyncPressed()
	{
		Jukebox.ToggleSync();
	}

	public void SongEntryClicked(RectTransform entry)
	{
		int num = songEntries.IndexOf(entry);
		if (Jukebox.currentTrack == Jukebox.TrackList[num])
		{
			Jukebox.TogglePlay();
		}
		else
		{
			Jukebox.PlayTrack(num);
		}
	}

	private void RefreshSongEntries()
	{
		for (int i = 0; i < songEntries.Count; i++)
		{
			Jukebox.Track track = Jukebox.TrackList[i];
			if (Jukebox.currentTrack == track && Jukebox.IsPlaying)
			{
				songEntries[i].Find("PlayPause/Icon").GetComponent<Image>().sprite = SongEntryPauseSprite;
			}
			else
			{
				songEntries[i].Find("PlayPause/Icon").GetComponent<Image>().sprite = SongEntryPlaySprite;
			}
		}
	}

	private void RefreshUI()
	{
		PausePlayImage.sprite = (Jukebox.IsPlaying ? PauseSprite : PlaySprite);
		ShuffleButton.targetGraphic.color = (Jukebox.Shuffle ? SelectedColor : DeselectedColor);
		SyncButton.targetGraphic.color = (Jukebox.Sync ? SelectedColor : DeselectedColor);
		Sprite sprite = RepeatModeSprite_None;
		switch (Jukebox.RepeatMode)
		{
		case Jukebox.ERepeatMode.None:
			sprite = RepeatModeSprite_None;
			break;
		case Jukebox.ERepeatMode.RepeatTrack:
			sprite = RepeatModeSprite_Track;
			break;
		case Jukebox.ERepeatMode.RepeatQueue:
			sprite = RepeatModeSprite_Queue;
			break;
		}
		(RepeatButton.targetGraphic as Image).sprite = sprite;
		RepeatButton.targetGraphic.color = ((Jukebox.RepeatMode == Jukebox.ERepeatMode.None) ? DeselectedColor : SelectedColor);
	}

	private void RefreshAmbientDisplay()
	{
		AmbientDisplayContainer.gameObject.SetActive(!IsOpen && Jukebox.IsPlaying);
		if (AmbientDisplayContainer.activeSelf)
		{
			UpdateAmbientDisplay();
		}
	}
}
