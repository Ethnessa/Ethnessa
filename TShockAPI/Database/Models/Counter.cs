using MongoDB.Bson;

namespace TShockAPI.Database.Models;

public class Counter
{
	public ObjectId Id { get; set; }
	public string Collection { get; set; }
	public int Increment { get; set; }

	public Counter(string coll)
	{
		Collection = coll;
		Increment = 0;
	}

}
