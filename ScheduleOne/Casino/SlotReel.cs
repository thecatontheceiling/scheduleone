using System;
using ScheduleOne.Audio;
using UnityEngine;
using UnityEngine.Events;

namespace ScheduleOne.Casino;

public class SlotReel : MonoBehaviour
{
	[Serializable]
	public class SymbolRotation
	{
		public SlotMachine.ESymbol Symbol;

		public float Rotation;
	}

	[Header("Settings")]
	public SymbolRotation[] SymbolRotations;

	public float SpinSpeed = 1000f;

	[Header("References")]
	public AudioSourceController StopSound;

	public UnityEvent onStart;

	public UnityEvent onStop;

	public bool IsSpinning { get; private set; }

	public SlotMachine.ESymbol CurrentSymbol { get; private set; } = SlotMachine.ESymbol.Seven;

	public float CurrentRotation { get; private set; }

	private void Awake()
	{
		SetSymbol(SlotMachine.GetRandomSymbol());
	}

	private void Update()
	{
		if (IsSpinning)
		{
			SetReelRotation(CurrentRotation + SpinSpeed * Time.deltaTime);
		}
		else
		{
			SetReelRotation(GetSymbolRotation(CurrentSymbol));
		}
	}

	public void Spin()
	{
		IsSpinning = true;
		if (onStart != null)
		{
			onStart.Invoke();
		}
	}

	public void Stop(SlotMachine.ESymbol endSymbol)
	{
		CurrentSymbol = endSymbol;
		IsSpinning = false;
		StopSound.Play();
		if (onStop != null)
		{
			onStop.Invoke();
		}
	}

	public void SetSymbol(SlotMachine.ESymbol symbol)
	{
		CurrentSymbol = symbol;
	}

	private void SetReelRotation(float rotation)
	{
		base.transform.localRotation = Quaternion.Euler(rotation, 0f, 0f);
		CurrentRotation = rotation % 360f;
	}

	private float GetSymbolRotation(SlotMachine.ESymbol symbol)
	{
		SymbolRotation[] symbolRotations = SymbolRotations;
		foreach (SymbolRotation symbolRotation in symbolRotations)
		{
			if (symbolRotation.Symbol == symbol)
			{
				return symbolRotation.Rotation;
			}
		}
		Console.LogWarning("SlotReel.GetSymbolRotation: Symbol not found: " + symbol);
		return 0f;
	}
}
