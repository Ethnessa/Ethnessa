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
using MongoDB.Entities;
using Terraria;
using TShockAPI.Database.Models;

namespace TShockAPI.Database
{
	public static class CharacterManager
	{
		public static async Task<PlayerData?> GetPlayerData(int accountId)
		{
			return await DB.Find<PlayerData>()
				.Match(x => x.UserId == accountId)
				.ExecuteFirstAsync();
		}

		public static async Task<bool> SeedInitialData(UserAccount account)
		{
			try
			{
				var items = new List<NetItem>(TShock.ServerSideCharacterConfig.Settings.StartingInventory);
				if (items.Count < NetItem.MaxInventory)
					items.AddRange(new NetItem[NetItem.MaxInventory - items.Count]);

				var tsSettings = TShock.ServerSideCharacterConfig.Settings;

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

				await initialData.SaveAsync();
				return true;
			}
			catch (Exception ex)
			{
				TShock.Log.ConsoleError($"Something went wrong while seeding initial player data: {ex.ToString()}");
				return false;
			}
		}

		/// <summary>
		/// Inserts player data to the tsCharacter database table
		/// </summary>
		/// <param name="player">player to take data from</param>
		/// <returns>true if inserted successfully</returns>
		public static async Task<bool> InsertPlayerData(TSPlayer player, bool fromCommand = false)
		{
			PlayerData playerData = player.PlayerData;

			if (!player.IsLoggedIn)
				return false;

			if (player.State < 10)
				return false;

			if (await player.HasPermission(Permissions.bypassssc) && !fromCommand)
			{
				TShock.Log.ConsoleInfo(GetParticularString("{0} is a player name",
					$"Skipping SSC save (due to tshock.ignore.ssc) for {player.Account.Name}"));
				return false;
			}

			try
			{
				await playerData.SaveAsync();
				return true;
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}

			return false;
		}

		/// <summary>
		/// Removes a player's data from the tsCharacter database table
		/// </summary>
		/// <param name="userid">User AccountId of the player</param>
		/// <returns>true if removed successfully</returns>
		public static async Task<bool> RemovePlayer(int userid)
		{
			try
			{
				var playerData = (await GetPlayerData(userid));

				if (playerData is null)
				{
					throw new Exception($"The player does not exist!");
				}

				await playerData.DeleteAsync();
				return true;
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}

			return false;
		}

		/// <summary>
		/// Inserts a specific PlayerData into the SSC table for a player.
		/// </summary>
		/// <param name="player">The player to store the data for.</param>
		/// <param name="data">The player data to store.</param>
		/// <returns>If the command succeeds.</returns>
		public static async Task<bool> InsertSpecificPlayerData(TSPlayer player, PlayerData data)
		{
			PlayerData playerData = data;

			if (!player.IsLoggedIn)
				return false;

			if (await player.HasPermission(Permissions.bypassssc))
			{
				TShock.Log.ConsoleInfo(GetParticularString("{0} is a player name",
					$"Skipping SSC save (due to tshock.ignore.ssc) for {player.Account.Name}"));
				return true;
			}

			await RemovePlayer(player.Account.AccountId);
			playerData.UserId = player.Account.AccountId;
			await playerData.SaveAsync();

			return true;
		}
	}
}
