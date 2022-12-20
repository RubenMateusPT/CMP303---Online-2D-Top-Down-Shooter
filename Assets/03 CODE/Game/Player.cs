using System;
using OnlineShooter.Network.Shared.Datagrams;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Player : MonoBehaviour
{
	[Header("Ignore")]
	public byte ID;
	public bool IsControlsEnabled = false;
	public ClientNetworkManager ClientNetworkManager;
	public ServerNetworkManager ServerNetworkManager;
	public Rigidbody2D Rigidbody;

	public float speed = 250;
	public bool isDisabled;

	[Header("CHange this ones")]
	public GameObject Aimer;

	public float aimAngle = 0;

	public float syncMovementTimer = 0;


	private Queue<PositionData> positionDataQueue = new Queue<PositionData>();
	private Vector2 lastPrediction, currentPrediction;

	private bool firstPacket = true;
	private float syncTimer = 0.1f;
	private int ticks = 0;

	private void Awake()
	{
#if UNITY_EDITOR
		speed *= 2;
#endif
		isDisabled = false;

		var networkManager = GameObject.FindObjectOfType<NetworkManager>();
		if (networkManager is ClientNetworkManager)
		{
			ClientNetworkManager = NetworkManager.GetInstance<ClientNetworkManager>();
		}
		else
		{
			ServerNetworkManager = NetworkManager.GetInstance<ServerNetworkManager>();
		}

		lastPrediction = transform.position;
	}

	private void Update()
	{
		if (isDisabled)
			return;
		OnlinePlayer();
		LocalPlayerControls();
	}

	private void OnlinePlayer()
	{
		if (IsControlsEnabled || firstPacket)
			return;

		syncMovementTimer += Time.deltaTime;
		if (syncMovementTimer > syncTimer)
		{
			ticks++;
			syncMovementTimer = 0;
		}
		
		if (positionDataQueue.Count >= 2)
		{
			var past = positionDataQueue.Dequeue();
			var present = positionDataQueue.Peek();
			float timeBetweenPresentAndPast = present.Tick - past.Tick;

			float speedX = present.Position.x - past.Position.x / (float)timeBetweenPresentAndPast;
			float speedY = present.Position.y - past.Position.y / (float)timeBetweenPresentAndPast;

			float timeSinceLastMessage = present.Tick - past.Tick;
			float displacementX = speedX * timeSinceLastMessage;
			float displacementY = speedY * timeSinceLastMessage;

			Vector2 displacement = new Vector2(displacementX, displacementY);

			lastPrediction = currentPrediction;
			currentPrediction = past.Position + displacement;

			transform.position = Vector2.Lerp(lastPrediction, currentPrediction, 0.1f * Time.deltaTime);
			Aimer.transform.eulerAngles = new Vector3(0, 0, present.Angle);
		}
	}

	private void LocalPlayerControls()
	{
		if (!IsControlsEnabled)
			return;

		//Exit Game
		if (Input.GetKeyUp(KeyCode.Escape))
		{
			IsControlsEnabled = false;
			GameObject.FindObjectOfType<GameManager>().ShowPopup();
			ClientNetworkManager.Disconnect(true);
		}
		Move();
		Aim();

		syncMovementTimer += Time.deltaTime;
		if (syncMovementTimer > syncTimer)
		{
			ticks++;
			syncMovementTimer = 0;
			ClientNetworkManager.SendPlayerMovement(transform.position, aimAngle, ticks);
		}
	}

	private void Move()
	{
		var dir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
		Rigidbody.velocity = dir * speed * Time.deltaTime;
	}

	private void Aim()
	{
		Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		Vector2 direction = mousePosition - transform.position;
		aimAngle = Vector2.SignedAngle(Vector2.right, direction);
		Aimer.transform.eulerAngles = new Vector3(0, 0, aimAngle);
	}

	public void UpdatePosition(Datagram baseDatagram, Datagrams.PlayerMovement rawData)
	{
		if (firstPacket)
		{
			firstPacket = false;

			int timeDiffRemotePlayerToServer = baseDatagram.Ticks - rawData.PlayerGameTick;
			int timeDiffLLocalPlayerToServer = ClientNetworkManager.Ticks - rawData.PlayerGameTick;
			int totalTimeDiff = timeDiffRemotePlayerToServer + timeDiffLLocalPlayerToServer;

			ticks = totalTimeDiff + rawData.PlayerTicks;
		}

		if (positionDataQueue.Any(p => p.Tick > rawData.PlayerTicks))
			return;

		positionDataQueue.Enqueue(new PositionData
		{
			Tick = rawData.PlayerTicks,
			Position = rawData.Pos,
			Angle = rawData.Angle
		});
	}

	private struct PositionData
	{
		public int Tick;
		public Vector2 Position;
		public float Angle;
	}


}
