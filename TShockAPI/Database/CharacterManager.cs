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
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using Terraria;
using TShockAPI.Database.Models;

namespace TShockAPI.Database
{
	public static class CharacterManager
	{
		private static IMongoCollection<PlayerData> playerData => ServerBase.GlobalDatabase.GetCollection<PlayerData>("playerdata");

		public static PlayerData? GetPlayerData(int accountId)
		{
			return playerData.Find<PlayerData>(x => x.UserId == accountId)
				.FirstOrDefault();
		}

		public static PlayerData? DeletePlayerData(int accountId)
		{
			return playerData.FindOneAndDelete<PlayerData>(x => x.UserId == accountId);
		}

		public static bool SeedInitialData(UserAccount account)
		{
			try
			{
				var items = new List<NetItem>(ServerBase.ServerSideCharacterConfig.Settings.StartingInventory);
				if (items.Count < NetItem.MaxInventory)
					items.AddRange(new NetItem[NetItem.MaxInventory - items.Count]);

				var tsSettings = ServerBase.ServerSideCharacterConfig.Settings;

				PlayerData initialData = new()
				{
					inventory = items.ToArray(),
					UserId = account.AccountId,
					health = tsSettings.StartingHealth,
					maxHealth = tsSettings.StartingHealth,
					mana = tsSettings.StartingMana,
					maxMana = tsSettings.StartingMana,
					spawnX = -1,
					spawnY = -1,
					questsCompleted = 0
				};

				playerData.InsertOne(initialData);
				return true;
			}
			catch (Exception ex)
			{
				ServerBase.Log.ConsoleError($"Something went wrong while seeding initial player data: {ex.ToString()}");
				return false;
			}
		}

		/// <summary>
		/// Inserts player data to the tsCharacter database table
		/// </summary>
		/// <param name="player">player to take data from</param>
		/// <returns>true if inserted successfully</returns>
		public static bool InsertPlayerData(ServerPlayer player, bool fromCommand = false)
		{
			PlayerData playersData = player.PlayerData;

			if (!player.IsLoggedIn)
				return false;

			if (player.State < 10)
				return false;

			if (player.HasPermission(Permissions.bypassssc) && !fromCommand)
			{
				ServerBase.Log.ConsoleInfo(GetParticularString("{0} is a player name",
					$"Skipping SSC save (due to tshock.ignore.ssc) for {player.Account.Name}"));
				return false;
			}

			try
			{
				playerData.InsertOne(playersData);
				return true;
			}
			catch (Exception ex)
			{
				ServerBase.Log.Error(ex.ToString());
			}

			return false;
		}

		/// <summary>
		/// Removes a player's data from the tsCharacter database table
		/// </summary>
		/// <param name="userid">User AccountId of the player</param>
		/// <returns>true if removed successfully</returns>
		public static bool RemovePlayer(int userid)
		{
			try
			{
				var playerData = DeletePlayerData(userid);

				if (playerData is null)
				{
					throw new Exception($"The player does not exist!");
				}

				return true;
			}
			catch (Exception ex)
			{
				ServerBase.Log.Error(ex.ToString());
			}

			return false;
		}

		/// <summary>
		/// Inserts a specific PlayerData into the SSC table for a player.
		/// </summary>
		/// <param name="player">The player to store the data for.</param>
		/// <param name="data">The player data to store.</param>
		/// <returns>If the command succeeds.</returns>
		public static bool InsertSpecificPlayerData(ServerPlayer player, PlayerData data)
		{
			PlayerData playersData = data;

			if (!player.IsLoggedIn)
				return false;

			if (player.HasPermission(Permissions.bypassssc))
			{
				ServerBase.Log.ConsoleInfo(GetParticularString("{0} is a player name",
					$"Skipping SSC save (due to tshock.ignore.ssc) for {player.Account.Name}"));
				return true;
			}

			RemovePlayer(player.Account.AccountId);
			playersData.UserId = player.Account.AccountId;
			playerData.InsertOne(playersData);

			return true;
		}
	}
}
