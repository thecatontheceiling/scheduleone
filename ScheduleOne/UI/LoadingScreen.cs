using System.Collections;
using ScheduleOne.Audio;
using ScheduleOne.DevUtilities;
using ScheduleOne.Networking;
using ScheduleOne.Persistence;
using ScheduleOne.ScriptableObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScheduleOne.UI;

public class LoadingScreen : PersistentSingleton<LoadingScreen>
{
	public const float FADE_TIME = 0.25f;

	public const float BACKGROUND_IMAGE_TIME = 8f;

	public const float BACKGROUND_IMAGE_FADE_TIME = 1f;

	public StringDatabase LoadingMessagesDatabase;

	public Sprite[] BackgroundImages;

	public Sprite[] TutorialBackgroundImages;

	[Header("References")]
	public Canvas Canvas;

	public CanvasGroup Group;

	public TextMeshProUGUI LoadStatusLabel;

	public TextMeshProUGUI LoadingMessageLabel;

	public Image BackgroundImage1;

	public Image BackgroundImage2;

	public RectTransform TutorialContainer;

	public RectTransform CoopTutorialHint;

	private string[] loadingMessages;

	private int currentBackgroundImageIndex;

	private Coroutine fadeRoutine;

	private Coroutine animateBackgroundRoutine;

	private Coroutine scaleBackgroundRoutine;

	private bool isLoadingTutorial;

	public bool IsOpen { get; protected set; }

	public Sprite[] ContextualBackgroundImages
	{
		get
		{
			if (!isLoadingTutorial)
			{
				return BackgroundImages;
			}
			return TutorialBackgroundImages;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		if (!(Singleton<LoadingScreen>.Instance == null) && !(Singleton<LoadingScreen>.Instance != this))
		{
			loadingMessages = LoadingMessagesDatabase.Strings;
			currentBackgroundImageIndex = Random.Range(0, ContextualBackgroundImages.Length);
			for (int i = 0; i < ContextualBackgroundImages.Length; i++)
			{
				int num = Random.Range(0, ContextualBackgroundImages.Length);
				Sprite sprite = ContextualBackgroundImages[i];
				ContextualBackgroundImages[i] = ContextualBackgroundImages[num];
				ContextualBackgroundImages[num] = sprite;
			}
			IsOpen = false;
			Canvas.enabled = false;
			Group.alpha = 0f;
		}
	}

	protected void Update()
	{
		if (IsOpen)
		{
			LoadStatusLabel.text = Singleton<LoadManager>.Instance.GetLoadStatusText();
		}
	}

	public void Open(bool loadingTutorial = false)
	{
		if (!IsOpen)
		{
			isLoadingTutorial = loadingTutorial;
			TutorialContainer.gameObject.SetActive(loadingTutorial);
			if (loadingTutorial && Singleton<Lobby>.Instance.IsInLobby && Singleton<Lobby>.Instance.PlayerCount > 1)
			{
				CoopTutorialHint.gameObject.SetActive(value: true);
			}
			else
			{
				CoopTutorialHint.gameObject.SetActive(value: false);
			}
			LoadingMessageLabel.text = loadingMessages[Random.Range(0, loadingMessages.Length)];
			IsOpen = true;
			Singleton<MusicPlayer>.Instance.SetTrackEnabled("Loading Screen", enabled: true);
			Fade(1f);
			AnimateBackground();
		}
	}

	public void Close()
	{
		if (IsOpen)
		{
			IsOpen = false;
			Singleton<MusicPlayer>.Instance.SetTrackEnabled("Loading Screen", enabled: false);
			Singleton<MusicPlayer>.Instance.StopTrack("Loading Screen");
			Fade(0f);
		}
	}

	private void AnimateBackground()
	{
		if (animateBackgroundRoutine != null)
		{
			StopCoroutine(animateBackgroundRoutine);
		}
		if (scaleBackgroundRoutine != null)
		{
			StopCoroutine(scaleBackgroundRoutine);
		}
		animateBackgroundRoutine = StartCoroutine(Routine());
		IEnumerator Routine()
		{
			currentBackgroundImageIndex++;
			BackgroundImage1.color = new Color(1f, 1f, 1f, 0f);
			BackgroundImage2.color = new Color(1f, 1f, 1f, 0f);
			Image prevImage = null;
			Image nextImage = BackgroundImage1;
			while (IsOpen || Group.alpha > 0f)
			{
				currentBackgroundImageIndex %= ContextualBackgroundImages.Length;
				nextImage.sprite = ContextualBackgroundImages[currentBackgroundImageIndex];
				scaleBackgroundRoutine = StartCoroutine(ScaleRoutine(nextImage.transform, 10f));
				for (float i = 0f; i < 1f; i += Time.deltaTime)
				{
					if (prevImage != null)
					{
						prevImage.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0f, i / 1f));
					}
					nextImage.color = new Color(1f, 1f, 1f, Mathf.Lerp(0f, 1f, i / 1f));
					yield return new WaitForEndOfFrame();
					if (prevImage != null)
					{
						prevImage.color = new Color(1f, 1f, 1f, 0f);
					}
					nextImage.color = new Color(1f, 1f, 1f, 1f);
				}
				yield return new WaitForSeconds(8f);
				prevImage = nextImage;
				nextImage = ((nextImage == BackgroundImage1) ? BackgroundImage2 : BackgroundImage1);
				currentBackgroundImageIndex++;
			}
			IEnumerator ScaleRoutine(Transform trans, float lerpTime)
			{
				trans.transform.localScale = Vector3.one;
				for (float i2 = 0f; i2 < lerpTime; i2 += Time.deltaTime)
				{
					float num = Mathf.Lerp(1f, 1.1f, i2 / lerpTime);
					nextImage.transform.localScale = new Vector3(num, num, 1f);
					yield return new WaitForEndOfFrame();
				}
			}
		}
	}

	private void Fade(float endAlpha)
	{
		if (fadeRoutine != null)
		{
			StopCoroutine(fadeRoutine);
		}
		fadeRoutine = StartCoroutine(Routine());
		IEnumerator Routine()
		{
			if (endAlpha > 0f)
			{
				Canvas.enabled = true;
			}
			float startAlpha = Group.alpha;
			for (float i = 0f; i < 0.25f; i += Time.deltaTime)
			{
				Group.alpha = Mathf.Lerp(startAlpha, endAlpha, i / 0.25f);
				yield return new WaitForEndOfFrame();
			}
			Group.alpha = endAlpha;
			if (endAlpha == 0f)
			{
				Canvas.enabled = false;
			}
			fadeRoutine = null;
		}
	}
}
