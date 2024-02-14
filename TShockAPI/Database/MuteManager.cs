using System;
using HttpServer;
using MongoDB.Driver;
using TShockAPI.Database.Models;

namespace TShockAPI.Database;

public class MuteManager
{
	private static IMongoCollection<MutedPlayer> mutes =>
		ServerBase.GlobalDatabase.GetCollection<MutedPlayer>("mutedplayers");

	public static event Action<MutedPlayer> OnMuteAdd;
	public static event Action<MutedPlayer> OnMuteRemove;

	public static long CountMutes()
	{
		return mutes.CountDocuments(Builders<MutedPlayer>.Filter.Empty);
	}

	public static bool IsPlayerMuted(ServerPlayer player)
	{
		var ip = player.IP;
		var uuid = player.UUID;
		var accountName = player.Account?.Name;

		return mutes.Find(x=>x.IpAddress == ip || x.Uuid == uuid || x.AccountName == accountName).Any();
	}

	public static bool CreateMute(IdentifierType type, string value, DateTime? expiryTime = null)
	{
		var mute = new MutedPlayer(type, value, expiryTime);
		mutes.InsertOne(mute);
		OnMuteAdd?.Invoke(mute);
		return true;
	}

	public static bool RemoveMute(IdentifierType type, string value)
	{
		var filter = Builders<MutedPlayer>.Filter.Eq(type.ToString(), value);
		var mute = mutes.FindOneAndDelete(filter);
		if (mute != null)
		{
			OnMuteRemove?.Invoke(mute);
			return true;
		}
		return false;
	}

	public static bool MutePlayer(ServerPlayer player, DateTime? expiryTime = null)
	{
		var ip = player.IP;
		var uuid = player.UUID;
		var accountName = player.Account?.Name;

		var success = CreateMute(IdentifierType.Uuid, uuid, expiryTime) &&
		              CreateMute(IdentifierType.IpAddress, ip, expiryTime);

		if (accountName is not null)
		{
			success = (success & CreateMute(IdentifierType.AccountName, accountName, expiryTime));
		}

		return success;
	}

	public static bool UnmutePlayer(ServerPlayer player)
	{
		var ip = player.IP;
		var uuid = player.UUID;
		var accountName = player.Account?.Name;

		try
		{
			RemoveMute(IdentifierType.Uuid, uuid);
			if (accountName is not null)
			{
				RemoveMute(IdentifierType.AccountName, accountName);
			}

			RemoveMute(IdentifierType.IpAddress, ip);
			return true;
		}
		catch (Exception ex)
		{
			ServerBase.Log.ConsoleError($"Could not remove mute for {player.Name}. {ex}");
			return false;
		}

	}



}
