using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
	[SerializeField] private TMP_InputField playerName;
	[SerializeField] private TMP_Text playerColor;
	[SerializeField] private Image playerSprite;

	Color[] colors ={
		Color.black,
		Color.white,
		Color.blue,
		Color.green,
		Color.red
	};
	private int currentColor;

	private void Start()
	{
		ChangeColor(0);
	}

	public void ChangeColor(int value){

		currentColor += value;

		if (currentColor >= colors.Length)
			currentColor = 0;
		else if (currentColor < 0)
			currentColor = colors.Length - 1;

		playerSprite.color = colors[currentColor];
		playerColor.text = colors[currentColor].ToString();
	}

	public void Connect()
	{
		PlayerPrefs.SetString("PlayerName",playerName.text);
		PlayerPrefs.SetString("PlayerColor", ColorUtility.ToHtmlStringRGBA(colors[currentColor]));
		PlayerPrefs.Save();
		FindObjectOfType<ClientManager>().Connect();
	}
}
