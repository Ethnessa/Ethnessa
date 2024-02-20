using System;
using EthnessaAPI.Database.Models;
using HttpServer;
using Microsoft.Xna.Framework;
using MongoDB.Driver;

namespace EthnessaAPI.Database;

public static class NicknameManager
{
	internal static string CollectionName = "nicknames";

	public static IMongoCollection<Nickname> Nicknames =>
		ServerBase.GlobalDatabase.GetCollection<Nickname>(CollectionName);

	public static bool ClearNickname(UserAccount account)
	{
		var result = Nicknames.DeleteOne(x => x.AccountId == account.AccountId);
		if (result.DeletedCount > 0)
		{
			UpdateIngameName(account, account.Name);
		}

		return result.DeletedCount > 0;
	}

	public static Nickname? GetNickname(UserAccount account)
	{
		return Nicknames.Find(x => x.AccountId == account.AccountId).FirstOrDefault();
	}

	public static bool SetNickname(UserAccount account, string nickName)
	{
		var existingNickname = GetNickname(account);

		if (existingNickname is null)
		{
			try
			{
				var newNickname = new Nickname(account.AccountId, nickName);

				Nicknames.InsertOne(newNickname);
				return true;
			}
			catch (Exception e)
			{
				ServerBase.Log.ConsoleError("Error setting nickname: " + e.Message);
				return false;
			}
		}

		// if existing nickname is not null, modify existing record
		existingNickname.AccountNickname = nickName;
		var result = Nicknames.ReplaceOne(x=>x.AccountId==account.AccountId, existingNickname);

		if (result.ModifiedCount > 0)
		{
			UpdateIngameName(account, nickName);
		}

		return result.ModifiedCount > 0;
	}

	internal static void UpdateIngameName(UserAccount account, string newName)
	{
		var onlinePlayer = ServerPlayer.GetPlayerOfUserAccount(account);
		if (onlinePlayer is not null)
		{
			ServerBase.Utils.Broadcast($"{onlinePlayer.Name} is now known as {newName}!", Color.LightYellow);
			onlinePlayer.TPlayer.name = newName;
			ServerPlayer.All.SendData(PacketTypes.PlayerUpdate, "", onlinePlayer.Index);
		}
	}
}
