using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class Player : MonoBehaviour
{
	[Header("Ignore")] 
	public byte ID;
	public bool IsControlsEnabled = false;
	public ClientNetworkManager ClientNetworkManager;
	public ServerNetworkManager ServerNetworkManager;
	public Rigidbody2D Rigidbody;

	[Header("CHange this ones")]
	public GameObject Aimer;

	public float aimAngle = 0;

	public float syncMovementTimer = 0;

	private void Awake()
	{
		var networkManager = GameObject.FindObjectOfType<NetworkManager>();
		if (networkManager is ClientNetworkManager)
		{
			ClientNetworkManager = NetworkManager.GetInstance<ClientNetworkManager>();
		}
		else
		{
			ServerNetworkManager = NetworkManager.GetInstance<ServerNetworkManager>();
		}
	}

	private void Update()
	{
		if (!IsControlsEnabled)
			return;

		Move();
		Aim();
	}

	private void LateUpdate()
	{
		if(!IsControlsEnabled)
			return;

		syncMovementTimer += Time.deltaTime;

		if (syncMovementTimer > 0.1)
		{
			syncMovementTimer = 0;
			ClientNetworkManager.SendPlayerMovement(transform.position, aimAngle);
		}
	}

	private void Move()
	{
		var dir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
		Rigidbody.velocity = dir * 500 * Time.deltaTime;
	}

	private void Aim()
	{
		Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		Vector2 direction = mousePosition - transform.position;
		aimAngle = Vector2.SignedAngle(Vector2.right, direction);
		Aimer.transform.eulerAngles = new Vector3(0, 0, aimAngle);
	}

	public void UpdatePosition(Vector2 pos, float angle)
	{
		transform.position = pos;
		Aimer.transform.eulerAngles = new Vector3(0, 0, angle);
	}
}
