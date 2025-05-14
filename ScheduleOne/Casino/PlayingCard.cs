using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EasyButtons;
using ScheduleOne.Audio;
using UnityEngine;

namespace ScheduleOne.Casino;

public class PlayingCard : MonoBehaviour
{
	[Serializable]
	public class CardSprite
	{
		public ECardSuit Suit;

		public ECardValue Value;

		public Sprite Sprite;
	}

	public struct CardData
	{
		public ECardSuit Suit;

		public ECardValue Value;

		public CardData(ECardSuit suit, ECardValue value)
		{
			Suit = suit;
			Value = value;
		}
	}

	public enum ECardSuit
	{
		Spades = 0,
		Hearts = 1,
		Diamonds = 2,
		Clubs = 3
	}

	public enum ECardValue
	{
		Blank = 0,
		Ace = 1,
		Two = 2,
		Three = 3,
		Four = 4,
		Five = 5,
		Six = 6,
		Seven = 7,
		Eight = 8,
		Nine = 9,
		Ten = 10,
		Jack = 11,
		Queen = 12,
		King = 13
	}

	public string CardID = "card_1";

	[Header("References")]
	public SpriteRenderer CardSpriteRenderer;

	public CardSprite[] CardSprites;

	public Animation FlipAnimation;

	public AnimationClip FlipFaceUpClip;

	public AnimationClip FlipFaceDownClip;

	[Header("Sound")]
	public AudioSourceController FlipSound;

	public AudioSourceController LandSound;

	private Coroutine moveRoutine;

	private Tuple<Vector3, Quaternion> lastGlideTarget;

	public bool IsFaceUp { get; private set; }

	public ECardSuit Suit { get; private set; }

	public ECardValue Value { get; private set; }

	public CardController CardController { get; private set; }

	private void OnValidate()
	{
		base.gameObject.name = "PlayingCard (" + CardID + ")";
	}

	public void SetCardController(CardController cardController)
	{
		CardController = cardController;
	}

	public void SetCard(ECardSuit suit, ECardValue value, bool network = true)
	{
		if (network && CardController != null)
		{
			CardController.SendCardValue(CardID, suit, value);
			return;
		}
		Suit = suit;
		Value = value;
		CardSprite cardSprite = GetCardSprite(suit, value);
		if (cardSprite != null)
		{
			CardSpriteRenderer.sprite = cardSprite.Sprite;
		}
	}

	public void ClearCard()
	{
		SetCard(ECardSuit.Spades, ECardValue.Blank);
	}

	public void SetFaceUp(bool faceUp, bool network = true)
	{
		if (network && CardController != null)
		{
			CardController.SendCardFaceUp(CardID, faceUp);
		}
		if (IsFaceUp != faceUp)
		{
			IsFaceUp = faceUp;
			if (IsFaceUp)
			{
				FlipAnimation.Play(FlipFaceUpClip.name);
			}
			else
			{
				FlipAnimation.Play(FlipFaceDownClip.name);
			}
			FlipSound.Play();
		}
	}

	public void GlideTo(Vector3 position, Quaternion rotation, float duration = 0.5f, bool network = true)
	{
		float verticalOffset;
		if (network && CardController != null)
		{
			CardController.SendCardGlide(CardID, position, rotation, duration);
		}
		else if (lastGlideTarget == null || !lastGlideTarget.Item1.Equals(position) || !lastGlideTarget.Item2.Equals(rotation))
		{
			lastGlideTarget = new Tuple<Vector3, Quaternion>(position, rotation);
			verticalOffset = 0.02f;
			if (moveRoutine != null)
			{
				StopCoroutine(moveRoutine);
			}
			moveRoutine = StartCoroutine(MoveRoutine());
		}
		IEnumerator MoveRoutine()
		{
			Vector3 startPosition = base.transform.position;
			Quaternion startRotation = base.transform.rotation;
			LandSound.Play();
			float time = 0f;
			while (time < duration)
			{
				time += Time.deltaTime;
				float num = Mathf.SmoothStep(0f, 1f, time / duration);
				Vector3 position2 = Vector3.Lerp(startPosition, position, num);
				position2.y += Mathf.Sin(num * MathF.PI) * verticalOffset;
				base.transform.position = position2;
				base.transform.rotation = Quaternion.Lerp(startRotation, rotation, num);
				yield return null;
			}
			base.transform.position = position;
			base.transform.rotation = rotation;
		}
	}

	private CardSprite GetCardSprite(ECardSuit suit, ECardValue val)
	{
		return CardSprites.FirstOrDefault((CardSprite x) => x.Suit == suit && x.Value == val);
	}

	[Button]
	public void VerifyCardSprites()
	{
		List<CardSprite> list = new List<CardSprite>(CardSprites);
		foreach (ECardSuit value in Enum.GetValues(typeof(ECardSuit)))
		{
			foreach (ECardValue value2 in Enum.GetValues(typeof(ECardValue)))
			{
				CardSprite cardSprite = GetCardSprite(value, value2);
				if (cardSprite == null)
				{
					Debug.LogError($"Card sprite for {value} {value2} is missing.");
				}
				else if (list.Contains(cardSprite))
				{
					Debug.LogError($"Card sprite for {value} {value2} is duplicated.");
				}
				else
				{
					list.Add(cardSprite);
				}
			}
		}
	}
}
