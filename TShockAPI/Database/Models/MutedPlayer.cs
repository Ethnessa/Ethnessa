using System;
using MongoDB.Bson;

namespace TShockAPI.Database.Models;

public class MutedPlayer
{
	public ObjectId Id { get; set; }
	public string? IpAddress { get; set; }
	public string? AccountName { get; set; }
	public string? Uuid { get; set; }
	public DateTime MuteTime { get; set; } = DateTime.UtcNow;
	public DateTime ExpiryTime { get; set; }

	public MutedPlayer(IdentifierType type, string value, DateTime? expiryTime = null)
	{
		switch (type)
		{
			case IdentifierType.AccountName:
			{
				AccountName = value;
				break;
			}
			case IdentifierType.Uuid:
			{
				Uuid = value;
				break;
			}
			case IdentifierType.IpAddress:
			{
				IpAddress = value;
				break;
			}
		}
		ExpiryTime = expiryTime ?? DateTime.MaxValue;
	}
}
