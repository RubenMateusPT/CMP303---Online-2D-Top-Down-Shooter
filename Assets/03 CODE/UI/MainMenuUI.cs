using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
	[Header("Player Avatar Configuration")]
	[SerializeField] private Image playerAvatarImage;
	[SerializeField] private TMP_Text playerUsernameText;

	[Header("Player Avatar Inputs")]
	[SerializeField] private TMP_InputField playerUsernameInputField;
	[SerializeField] private TMP_Text playerAvatarColor;

	[Header("Server Configuration")]
	[SerializeField] private TMP_InputField serverIPInputField;
	[SerializeField] private TMP_InputField serverPortInputField;

	private int currentColor = 0;
	private string[] colors =
	{
		"yellow",
		"green",
		"blue",
		"purple",
		"red",
		"orange"
	};

	private string hostname = "127.0.0.1";
	private int port = 50000;

	private void Awake()
	{
		playerUsernameInputField.onValueChanged.AddListener(OnUsernameChange);
		ChangeColor(0);
	}

	void OnUsernameChange(string username)
	{
		playerUsernameText.text = username;

		if (string.IsNullOrEmpty(username))
		{
			playerUsernameText.text = "Username";
		}
	}

	private void ChangeColor(int value)
	{
		currentColor += value;

		if(currentColor < 0)
			currentColor = colors.Length-1;
		else if (currentColor >= colors.Length)
			currentColor = 0;

		string colorText = colors[currentColor];

		Color color;
		ColorUtility.TryParseHtmlString(colorText, out color);

		playerAvatarImage.color = color;
		playerAvatarColor.text = colorText;

	}

	public void PreviousColor()
	{
		ChangeColor(-1);
	}

	public void NextColor()
	{
		ChangeColor(1);
	}

	public void ConnectToServer()
	{
		PlayerSettings.Instance.Username = playerUsernameText.text;
		PlayerSettings.Instance.PlayerColor = colors[currentColor];

		if (!string.IsNullOrEmpty(serverIPInputField.text))
		{
			hostname = serverIPInputField.text;
			port = int.Parse(serverPortInputField.text);
		}

		NetworkManager.GetInstance<ClientNetworkManager>().ConnectToServer(hostname,port);
	}
}
