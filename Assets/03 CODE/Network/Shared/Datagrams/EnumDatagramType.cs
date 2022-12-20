namespace OnlineShooter.Network.Shared.Datagrams
{
	public enum DatagramType
	{
		Acknowledge,
		Error,
		ConnectionRequest,
		ConnectionRequestResponse,
		GameDataRequest,
		GameDataResponse,
		NewPlayerJoin,
		NewPlayerJoinResponse,
		NewPlayerGroupRequest,
		NewPlayerGroupResponse,
		AreYouAlive,
		RemoveClient,
		PlayerMovement
	}
}