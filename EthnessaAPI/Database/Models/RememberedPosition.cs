using System;
using Microsoft.Xna.Framework;
using MongoDB.Bson;

namespace EthnessaAPI.Database.Models;

public class RememberedPosition
{
	public ObjectId Id { get; set; }
	public int AccountId { get; set; }

	public int X { get; set; }
	public int Y { get; set; }

	public int WorldId { get; set; }

	public Vector2 Position => new(X, Y);

	public DateTime Created { get; set; } = DateTime.UtcNow;

	public RememberedPosition(int accountId, int x, int y, int worldId)
	{
		AccountId = accountId;
		X = x;
		Y = y;
		WorldId = worldId;
	}
}
