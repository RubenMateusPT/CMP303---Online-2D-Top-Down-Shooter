using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSettings : MonoBehaviour
{
	public static PlayerSettings Instance { get; private set; }

	private string _username;
	private string _playerColor;

	public string Username
	{
		get
		{
			return _username;
		}
		set
		{
			PlayerPrefs.SetString("USERNAME", value);
			_username = value;
		}
	}

	public string PlayerColor
	{
		get
		{
			return _playerColor;
		}
		set
		{
			PlayerPrefs.SetString("PLAYERCOLOR", value);
			_playerColor = value;
		}
	}

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
		}
		else
		{
			Instance = this;
		}

		DontDestroyOnLoad(gameObject);
	}
}
