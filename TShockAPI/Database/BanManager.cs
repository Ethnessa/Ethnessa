/*
TShock, a server mod for Terraria
Copyright (C) 2011-2019 Pryaxis & TShock Contributors

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using HttpServer;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using TShockAPI.Database.Models;
using Entity = Terraria.Entity;
using MongoDB.Driver;

namespace TShockAPI.Database
{
	/// <summary>
	/// Class that manages bans.
	/// </summary>
	public static class BanManager
	{
		internal static readonly string CollectionName = "bans";
		private static IMongoCollection<Ban> bans => ServerBase.GlobalDatabase.GetCollection<Ban>(CollectionName);

		/// <summary>
		/// Returns the number of bans that already exist
		/// </summary>
		/// <returns>Total number of bans</returns>
		public static long CountBans()
		{
			return bans.CountDocuments(Builders<Ban>.Filter.Empty);
		}

		/// <summary>
		/// Event invoked after a ban is added
		/// </summary>
		public static event Action<Ban> OnBanAdd;

		/// <summary>
		/// Event invoked after a user is unbanned
		/// </summary>
		public static event Action<Ban> OnBanRemove;

		public static List<Ban> GetPaginatedBans(int page, int perPageCount)
		{
			return bans.Find(Builders<Ban>.Filter.Empty).Skip((page - 1) * perPageCount).Limit(perPageCount).ToList();
		}
		internal static bool IsPlayerBanned(ServerPlayer player)
		{
			// Attempt to find a ban by account name if the player is logged in,
			// otherwise, find by IP address or UUID.
			var banFilter = player.IsLoggedIn
				? Builders<Ban>.Filter.Eq(b => b.AccountName, player.Account.Name)
				: Builders<Ban>.Filter.Or(
					Builders<Ban>.Filter.Eq(b => b.IpAddress, player.IP),
					Builders<Ban>.Filter.Eq(b => b.Uuid, player.UUID));

			// Execute the database query to find a matching ban.
			var ban = bans.Find(banFilter).FirstOrDefault();

			// If no ban is found, the player is not banned.
			if (ban == null) return false;

			// Disconnect the player with an appropriate message based on the ban expiration.
			var disconnectMessage = GetBanDisconnectMessage(ban);
			player.Disconnect(disconnectMessage);
			return true;
		}

		private static string GetBanDisconnectMessage(Ban ban)
		{
			var baseMessage = $"#{ban.BanId} - You are banned: {ban.Reason}";

			// If the ban is permanent.
			if (ban.ExpirationDateTime == DateTime.MaxValue)
			{
				return GetParticularString("{0} is ban number, {1} is ban reason", baseMessage);
			}

			// If the ban has an expiration date.
			var timeRemaining = ban.ExpirationDateTime - DateTime.UtcNow;
			var prettyExpiration =
				ban.GetPrettyExpirationString(); // Assuming this method exists and formats the expiration nicely.
			return GetParticularString("{0} is ban number, {1} is ban reason, {2} is a timestamp",
				$"{baseMessage} ({prettyExpiration} remaining)");
		}

		/// <summary>
		/// Retrieves a single ban from a ban's AccountId
		/// </summary>
		/// <param name="id">The ban identifier</param>
		/// <returns>The requested ban</returns>
		public static Ban? GetBanById(int id)
		{
			return bans.Find<Ban>(x => x.BanId == id)
				.FirstOrDefault();
		}

		/// <summary>
		/// Retrieves a list of bans from the database, sorted by their addition date from newest to oldest
		/// </summary>
		public static IEnumerable<Ban> RetrieveAllBans() => RetrieveAllBansSorted(BanSortMethod.DateBanned, true);

		/// <summary>
		/// Retrieves an enumerable of Bans from the database, sorted using the provided sort method
		/// </summary>
		/// <param name="sortMethod">The method to sort the bans.</param>
		/// <param name="descending">Whether the sort should be in descending order.</param>
		/// <returns>A sorted enumerable of Ban objects.</returns>
		public static IEnumerable<Ban> RetrieveAllBansSorted(BanSortMethod sortMethod, bool descending = true)
		{
			var sortDefinition = descending
				? Builders<Ban>.Sort.Descending(GetSortField(sortMethod))
				: Builders<Ban>.Sort.Ascending(GetSortField(sortMethod));

			var banList = bans.Find(Builders<Ban>.Filter.Empty).Sort(sortDefinition).ToList();
			return banList;
		}

		private static string GetSortField(BanSortMethod sortMethod)
		{
			return sortMethod switch
			{
				BanSortMethod.TicketNumber => nameof(BanSortMethod.TicketNumber),
				BanSortMethod.DateBanned => nameof(BanSortMethod.DateBanned),
				BanSortMethod.EndDate => nameof(BanSortMethod.EndDate),
				_ => throw new ArgumentOutOfRangeException(nameof(sortMethod), $"Not expected sort method value: {sortMethod}"),
			};
		}

		public static Ban CreateBan(IdentifierType type, string value, string reason, string banningUser, DateTime start,
			DateTime? endDate = null)
		{
			Ban ban = new()
			{
				Reason = reason,
				BanningUser = banningUser,
				BanDateTime = start,
				ExpirationDateTime = endDate ?? DateTime.MaxValue
			};

			var banMessage = BanManager.GetBanDisconnectMessage(ban);

			switch (type)
			{
				case IdentifierType.Uuid:
				{
					ban.Uuid = value;

					// find player with uuid and disconnect
					ServerBase.Players.FirstOrDefault(x=>x.UUID == value)?.Disconnect(banMessage);
					break;
				}
				case IdentifierType.AccountName:
				{
					ban.AccountName = value;

					// find player with account name and disconnect
					ServerBase.Players.FirstOrDefault(x=>x.Account?.Name == value)?.Disconnect(banMessage);
					break;
				}
				case IdentifierType.IpAddress:
				{
					ban.IpAddress = value;

					// find all players with ip address, and disconnect
					var players = ServerBase.Players.Where(x=>x?.IP == value);
					foreach (var player in players)
					{
						if (player is null)
						{
							continue;
						}

						if (player?.RealPlayer is false)
						{
							continue;
						}

						player?.Disconnect(banMessage);
					}

					break;
				}
				default: throw new Exception("Invalid ban type!");
			}
			bans.InsertOne(ban);

			ServerBase.Log.Info("A new ban has been created, AccountId:  " + ban.BanId);
			OnBanAdd?.Invoke(ban);
			return ban;
		}

		/// <summary>
		/// Remove a ban from the database
		/// </summary>
		/// <param name="value">An account name, ban AccountId, Ip Address, or Uuid</param>
		/// <returns>True if the ban was found and removed</returns>
		public static bool RemoveBan(string value)
		{

			try
			{
				var ban = bans.FindOneAndDelete<Ban>(x =>
					x.AccountName == value || x.BanId.ToString() == value || x.IpAddress == value || x.Uuid == value);
				OnBanRemove?.Invoke(ban);
				return true;
			}
			catch (Exception ex)
			{
				ServerBase.Log.Error("There was an error removing the ban with value " + value);
				ServerBase.Log.ConsoleError(ex.ToString());
				return false;
			}
		}

		/// <summary>
		/// Removes all bans from the database
		/// </summary>
		public static void ClearBans() => bans.DeleteMany(Builders<Ban>.Filter.Empty);

	}

	/// <summary>
	/// Enum containing sort options for ban retrieval
	/// </summary>
	public enum BanSortMethod
	{
		DateBanned,
		EndDate,
		TicketNumber
	}

	/// <summary>
	/// Result of an attempt to add a ban
	/// </summary>
	public class AddBanResult
	{
		/// <summary>
		/// Message generated from the attempt
		/// </summary>
		public string Message { get; set; }

		/// <summary>
		/// Ban object generated from the attempt, or null if the attempt failed
		/// </summary>
		public Ban Ban { get; set; }
	}

	/// <summary>
	/// Event args used for completed bans
	/// </summary>
	public class BanEventArgs : EventArgs
	{
		/// <summary>
		/// Complete ban object
		/// </summary>
		public Ban Ban { get; set; }

		/// <summary>
		/// Player ban is being applied to
		/// </summary>
		public ServerPlayer Player { get; set; }

		/// <summary>
		/// Whether or not the operation should be considered to be valid
		/// </summary>
		public bool Valid { get; set; } = true;
	}

	/// <summary>
	/// Event args used for ban data prior to a ban being formalized
	/// </summary>
	public class BanPreAddEventArgs : EventArgs
	{
		/// <summary>
		/// An identifiable piece of information to ban
		/// </summary>
		public string Identifier { get; set; }

		/// <summary>
		/// Gets or sets the ban reason.
		/// </summary>
		/// <value>The ban reason.</value>
		public string Reason { get; set; }

		/// <summary>
		/// Gets or sets the name of the user who added this ban entry.
		/// </summary>
		/// <value>The banning user.</value>
		public string BanningUser { get; set; }

		/// <summary>
		/// DateTime from which the ban will take effect
		/// </summary>
		public DateTime BanDateTime { get; set; }

		/// <summary>
		/// DateTime at which the ban will end
		/// </summary>
		public DateTime ExpirationDateTime { get; set; }

		/// <summary>
		/// Whether or not the operation should be considered to be valid
		/// </summary>
		public bool Valid { get; set; } = true;

		/// <summary>
		/// Optional message to explain why the event was invalidated, if it was
		/// </summary>
		public string Message { get; set; }
	}

	/// <summary>
	/// Model class that represents a ban entry in the database.
	/// </summary>
	public class Ban
	{
		/// <summary>
		/// MongoDB ObjectId
		/// </summary>
		public ObjectId Id { get; set; }

		/// <summary>
		/// A unique id assigned to this ban
		/// </summary>
		public int BanId { get; set; }

		/// <summary>
		/// A possible IP address we are banning
		/// </summary>
		public string? IpAddress { get; set; }

		/// <summary>
		/// A possible UUID we are banning
		/// </summary>
		public string? Uuid { get; set; }

		/// <summary>
		/// A possible account name we are banning
		/// </summary>
		public string? AccountName { get; set; }

		/// <summary>
		/// Gets or sets the ban reason.
		/// </summary>
		/// <value>The ban reason.</value>
		public string Reason { get; set; }

		/// <summary>
		/// Gets or sets the name of the user who added this ban entry.
		/// </summary>
		/// <value>The banning user.</value>
		public string BanningUser { get; set; }

		/// <summary>
		/// DateTime from which the ban will take effect
		/// </summary>
		public DateTime BanDateTime { get; set; }

		/// <summary>
		/// DateTime at which the ban will end
		/// </summary>
		public DateTime ExpirationDateTime { get; set; }

		/// <summary>
		/// Returns whether or not the ban is still in effect
		/// </summary>
		[BsonIgnore] public bool Valid => ExpirationDateTime > BanDateTime;

		public Ban()
		{
			BanId = CounterManager.GetAndIncrement(BanManager.CollectionName);
		}

		/// <summary>
		/// Returns a string in the format dd:mm:hh:ss indicating the time until the ban expires.
		/// If the ban is not set to expire (ExpirationDateTime == DateTime.MaxValue), returns the string 'Never'
		/// </summary>
		/// <returns></returns>
		public string GetPrettyExpirationString()
		{
			if (ExpirationDateTime == DateTime.MaxValue)
			{
				return "Never";
			}

			TimeSpan
				ts = (ExpirationDateTime - DateTime.UtcNow)
					.Duration(); // Use duration to avoid pesky negatives for expired bans
			return $"{ts.Days:00}:{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
		}

		/// <summary>
		/// Returns a string in the format dd:mm:hh:ss indicating the time elapsed since the ban was added.
		/// </summary>
		/// <returns></returns>
		public string GetPrettyTimeSinceBanString()
		{
			TimeSpan ts = (DateTime.UtcNow - BanDateTime).Duration();
			return $"{ts.Days:00}:{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
		}

	}

}
