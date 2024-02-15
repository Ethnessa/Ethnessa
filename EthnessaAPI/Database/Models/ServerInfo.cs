using MongoDB.Bson;

namespace EthnessaAPI.Database.Models;

public class ServerInfo
{
	public ObjectId Id { get; set; }

	public bool DefaultGroupsCreatedOnce { get; set; }

	public string ServerOwner { get; set; }
}
