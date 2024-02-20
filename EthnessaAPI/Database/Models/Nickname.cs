using MongoDB.Bson;

namespace EthnessaAPI.Database.Models;

public class Nickname
{
	public ObjectId Id { get; set; }
	public int AccountId { get; set; }
	public string AccountNickname { get; set; }

	public Nickname(int accountId, string accountNickname)
	{
		AccountId = accountId;
		AccountNickname = accountNickname;
	}
}
