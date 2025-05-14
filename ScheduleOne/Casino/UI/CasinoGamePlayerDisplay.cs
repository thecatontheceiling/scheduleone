using ScheduleOne.Money;
using ScheduleOne.PlayerScripts;
using TMPro;
using UnityEngine;

namespace ScheduleOne.Casino.UI;

public class CasinoGamePlayerDisplay : MonoBehaviour
{
	public CasinoGamePlayers BindedPlayers;

	[Header("References")]
	public TextMeshProUGUI TitleLabel;

	public RectTransform[] PlayerEntries;

	public void RefreshPlayers()
	{
		int currentPlayerCount = BindedPlayers.CurrentPlayerCount;
		TitleLabel.text = "Players (" + currentPlayerCount + "/" + BindedPlayers.PlayerLimit + ")";
		for (int i = 0; i < PlayerEntries.Length; i++)
		{
			Player player = BindedPlayers.GetPlayer(i);
			if (player != null)
			{
				PlayerEntries[i].Find("Container/Name").GetComponent<TextMeshProUGUI>().text = player.PlayerName;
				PlayerEntries[i].Find("Container").gameObject.SetActive(value: true);
			}
			else
			{
				PlayerEntries[i].Find("Container").gameObject.SetActive(value: false);
			}
		}
		RefreshScores();
	}

	public void RefreshScores()
	{
		int currentPlayerCount = BindedPlayers.CurrentPlayerCount;
		for (int i = 0; i < PlayerEntries.Length; i++)
		{
			if (currentPlayerCount > i)
			{
				PlayerEntries[i].Find("Container/Score").GetComponent<TextMeshProUGUI>().text = MoneyManager.FormatAmount(BindedPlayers.GetPlayerScore(BindedPlayers.GetPlayer(i)));
			}
		}
	}

	public void Bind(CasinoGamePlayers players)
	{
		BindedPlayers = players;
		BindedPlayers.onPlayerListChanged.AddListener(RefreshPlayers);
		BindedPlayers.onPlayerScoresChanged.AddListener(RefreshScores);
		RefreshPlayers();
	}

	public void Unbind()
	{
		if (!(BindedPlayers == null))
		{
			BindedPlayers.onPlayerListChanged.RemoveListener(RefreshPlayers);
			BindedPlayers.onPlayerScoresChanged.RemoveListener(RefreshScores);
			BindedPlayers = null;
		}
	}
}
