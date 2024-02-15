using EthnessaAPI.Database.Models;
using MongoDB.Driver;

namespace EthnessaAPI.Database;

public class ServerInfoManager
{
	private static IMongoCollection<ServerInfo> _collection => ServerBase.GlobalDatabase.GetCollection<ServerInfo>("server_info");

	public static ServerInfo RetrieveServerInfo()
	{
		var serverInfo = _collection.Find(x => true).FirstOrDefault();

		if (serverInfo is null)
		{
			ServerInfo newInfo = new ServerInfo();
			_collection.InsertOne(newInfo);
			return newInfo;
		}
		else
		{
			return serverInfo;
		}
	}

	public static void SaveServerInfo(ServerInfo info) => _collection.ReplaceOne(x => x.Id == info.Id, info);
}
