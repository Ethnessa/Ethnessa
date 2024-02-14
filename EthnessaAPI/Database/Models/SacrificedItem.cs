using System;
using MongoDB.Bson;

namespace EthnessaAPI.Database.Models;

public class SacrificedItem
{
	public ObjectId Id { get; set; }
	public int WorldId { get; set; }
	public int AccountId { get; set; }
	public int ItemId { get; set; }
	public int AmountSacrified { get; set; }
	public DateTime TimeSacrificed { get; set; }

	public SacrificedItem(int worldId, int accountId, int itemId, int amountSacrified)
	{
		WorldId = worldId;
		AccountId = accountId;
		ItemId = itemId;
		AmountSacrified = amountSacrified;
		TimeSacrificed = DateTime.UtcNow;
	}
}
