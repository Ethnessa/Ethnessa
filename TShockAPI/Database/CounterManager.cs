using MongoDB.Driver;
using TShockAPI.Database.Models;

namespace TShockAPI.Database;

public static class CounterManager
{
	private static IMongoCollection<Counter> _collection =>
		ServerBase.GlobalDatabase.GetCollection<Counter>("counters");

	public static void Increment(string collection, int amount = 1)
	{
		var filter = Builders<Counter>.Filter.Eq(c => c.Collection, collection);
		var update = Builders<Counter>.Update.Inc(c => c.Increment, amount);
		_collection.UpdateOne(filter, update, new UpdateOptions { IsUpsert = true });
	}

	public static int Get(string collection)
	{
		var filter = Builders<Counter>.Filter.Eq(c => c.Collection, collection);
		var counter = _collection.Find(filter).FirstOrDefault();
		return counter?.Increment ?? 0;
	}

	public static int GetAndIncrement(string collection, int amount = 1)
	{
		var increment = Get(collection);
		Increment(collection);
		return increment;
	}
}
