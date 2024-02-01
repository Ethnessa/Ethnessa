using System;
using Microsoft.Xna.Framework;

namespace TShockAPI.Database.Models;

public class RememberedPosition : MongoDB.Entities.Entity
{
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
