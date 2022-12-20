using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Experimental.AI;

public class GameManager : MonoBehaviour
{
	private List<Player> players = new List<Player>();

	public GameObject PopUp;

	public Transform PlayersParentGroup;
	public GameObject PlayerPrefab;
	public Transform[] PlayersSpawns;

	private void Awake()
	{
		PopUp.SetActive(false);
	}

	public void CreatePlayer(NetworkClient networkPlayer, bool isLocalPlayer)
	{
		var newPlayer = GameObject.Instantiate(PlayerPrefab,PlayersSpawns[players.Count].position, Quaternion.identity, PlayersParentGroup);
		newPlayer.gameObject.name = $"{networkPlayer.GetId}:{networkPlayer.GetName}";

		newPlayer.GetComponentInChildren<TMP_Text>().text = networkPlayer.GetName;

		Color color;
		ColorUtility.TryParseHtmlString(networkPlayer.Color, out color);
		newPlayer.GetComponent<SpriteRenderer>().color = color;

		networkPlayer.PlayerGO = newPlayer.GetComponent<Player>();
		networkPlayer.PlayerGO.IsControlsEnabled = isLocalPlayer;
		if(!isLocalPlayer)
			Destroy(networkPlayer.PlayerGO.Rigidbody);
		networkPlayer.PlayerGO.ID = networkPlayer.GetId;

		players.Add(newPlayer.GetComponent<Player>());
	}

	public void DeletePlayer(NetworkClient networkPlayer)
	{
		if (networkPlayer == null || networkPlayer.PlayerGO == null)
			return;
		if (!players.Contains(networkPlayer.PlayerGO))
			return;

		players.Remove(networkPlayer.PlayerGO);
		Destroy(networkPlayer.PlayerGO.gameObject);
	}

	public void ShowPopup()
	{
		PopUp.SetActive(true);
	}
}
