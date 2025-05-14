using System;
using ScheduleOne.Interaction;
using ScheduleOne.PlayerScripts;
using UnityEngine;

namespace ScheduleOne.Casino;

public class CasinoGameInteraction : MonoBehaviour
{
	public string GameName;

	[Header("References")]
	public CasinoGamePlayers Players;

	public InteractableObject IntObj;

	public Action<Player> onLocalPlayerRequestJoin;

	private void Awake()
	{
		IntObj.onHovered.AddListener(Hovered);
		IntObj.onInteractStart.AddListener(Interacted);
	}

	private void Hovered()
	{
		if (Players.CurrentPlayerCount < Players.PlayerLimit)
		{
			IntObj.SetMessage("Play " + GameName);
			IntObj.SetInteractableState(InteractableObject.EInteractableState.Default);
		}
		else
		{
			IntObj.SetMessage("Table is full");
			IntObj.SetInteractableState(InteractableObject.EInteractableState.Invalid);
		}
	}

	private void Interacted()
	{
		if (Players.CurrentPlayerCount < Players.PlayerLimit && onLocalPlayerRequestJoin != null)
		{
			onLocalPlayerRequestJoin(Player.Local);
		}
	}
}
