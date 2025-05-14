using ScheduleOne.DevUtilities;
using ScheduleOne.PlayerScripts;
using TMPro;
using UnityEngine;

namespace ScheduleOne.UI;

public class TextInputScreen : Singleton<TextInputScreen>
{
	public delegate void OnSubmit(string text);

	public Canvas Canvas;

	public TextMeshProUGUI HeaderLabel;

	public TMP_InputField InputField;

	private OnSubmit onSubmit;

	public bool IsOpen => Canvas.enabled;

	protected override void Awake()
	{
		base.Awake();
		GameInput.RegisterExitListener(Exit, 2);
	}

	public void Submit()
	{
		Close(submit: true);
	}

	public void Cancel()
	{
		Close(submit: false);
	}

	private void Update()
	{
		if (IsOpen && UnityEngine.Input.GetKeyDown(KeyCode.Return))
		{
			Submit();
		}
	}

	public void Exit(ExitAction action)
	{
		if (!action.Used && IsOpen && action.exitType == ExitType.Escape)
		{
			action.Used = true;
			Close(submit: false);
		}
	}

	public void Open(string header, string text, OnSubmit _onSubmit, int maxChars = 10000)
	{
		HeaderLabel.text = header;
		InputField.SetTextWithoutNotify(text);
		Canvas.enabled = true;
		InputField.characterLimit = maxChars;
		InputField.ActivateInputField();
		PlayerSingleton<PlayerInventory>.Instance.SetEquippingEnabled(enabled: false);
		PlayerSingleton<PlayerCamera>.Instance.FreeMouse();
		PlayerSingleton<PlayerCamera>.Instance.SetCanLook(c: false);
		PlayerSingleton<PlayerCamera>.Instance.AddActiveUIElement(base.name);
		PlayerSingleton<PlayerMovement>.Instance.canMove = false;
		onSubmit = _onSubmit;
	}

	private void Close(bool submit)
	{
		Canvas.enabled = false;
		InputField.DeactivateInputField();
		PlayerSingleton<PlayerInventory>.Instance.SetEquippingEnabled(enabled: true);
		PlayerSingleton<PlayerCamera>.Instance.LockMouse();
		PlayerSingleton<PlayerCamera>.Instance.SetCanLook(c: true);
		PlayerSingleton<PlayerCamera>.Instance.RemoveActiveUIElement(base.name);
		PlayerSingleton<PlayerMovement>.Instance.canMove = true;
		if (submit)
		{
			string text = InputField.text;
			if (onSubmit != null)
			{
				onSubmit(text);
			}
		}
	}
}
