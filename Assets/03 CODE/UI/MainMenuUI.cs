using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
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
	[SerializeField] private Button serverConnectButton;
	public TMP_Text ipPlaceholder;
	public TMP_Text portPlaceholder;

	[Header("Loading Pop Up")]
	[SerializeField] private GameObject _loadingPopup;
	[SerializeField] private TMP_Text _loadingText;
	

	private ClientNetworkManager _networkManager;

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

	private bool animateText;

	private void OnEnable()
	{
		playerUsernameInputField.onValueChanged.AddListener(OnUsernameChange);
	}

	private void OnDisable()
	{
		playerUsernameInputField.onValueChanged.RemoveListener(OnUsernameChange);
		_networkManager.NetworkStatus -= UpdatePopupText;
	}

	private void Awake()
	{
		_loadingPopup.SetActive(false);
		
		ChangeColor(0);
		animateText = false;
	}

	private void Start()
	{
		_networkManager = NetworkManager.GetInstance<ClientNetworkManager>();
		_networkManager.NetworkStatus += UpdatePopupText;
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

		hostname = string.IsNullOrEmpty(serverIPInputField.text) ? ipPlaceholder.text : serverIPInputField.text;
		port = string.IsNullOrEmpty(serverPortInputField.text) ? int.Parse(portPlaceholder.text) : int.Parse(serverPortInputField.text);

		serverConnectButton.gameObject.SetActive(false);
		_loadingPopup.SetActive(true);
		
		_networkManager.ConnectToServer(hostname, port);
	}

	public void UpdatePopupText(ClientNetworkManager.NetworkStatusCode status)
	{
		animateText = false;
		StopAllCoroutines();

		switch (status)
		{
			case ClientNetworkManager.NetworkStatusCode.Connecting:
				animateText = true;
				StartCoroutine(AnimateConnectingText());
				break;

			case ClientNetworkManager.NetworkStatusCode.NoResponseFromServer:
				_loadingText.text = "No Response From Server";
				StartCoroutine(ClosePopUp());
				break;

			case ClientNetworkManager.NetworkStatusCode.ServerIsFull:
				_loadingText.text = "Server is Full";
				StartCoroutine(ClosePopUp());
				break;

			case ClientNetworkManager.NetworkStatusCode.SuccessfullConnection:
				_loadingText.text = "Connected to Server";
				break;

			case ClientNetworkManager.NetworkStatusCode.RequestingGameData:
				_loadingText.text = "Requesting Game Data";
				break;

			case ClientNetworkManager.NetworkStatusCode.FailedToGetGameData:
				_loadingText.text = "Failed to Get Game data\nDisconnecting";
				StartCoroutine(ClosePopUp());
				break;

			case ClientNetworkManager.NetworkStatusCode.SuccesfullyGotGameData:
				_loadingText.text = "Loading Level...";
				break;

			case ClientNetworkManager.NetworkStatusCode.PlayerConfirmationFailed:
				_loadingText.text = "Error Loading Level\nReturning";
				StartCoroutine(ClosePopUp());
				break;
		}
	}

	private IEnumerator ClosePopUp()
	{
		yield return new WaitForSeconds(3);
		_loadingPopup.SetActive(false);
		serverConnectButton.gameObject.SetActive(true);
	}

	private IEnumerator AnimateConnectingText()
	{
		int dots = 0;

		while (animateText)
		{
			if (dots > 3)
				dots = 0;

			_loadingText.text = "Connecting to Server";
			for (int i = 0; i < dots; i++)
			{
				_loadingText.text += ".";
			}

			_loadingText.text += $"\n{hostname}:{port}";
			yield return new WaitForSeconds(0.5f);
			dots++;
		}
	}
}
