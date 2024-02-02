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
using System.Threading.Tasks;
using MongoDB.Entities;
using Terraria;
using TShockAPI.Hooks;

namespace TShockAPI.Database
{
	public static class TileManager
	{
		public static async Task<List<TileBan>> ToListAsync()
		{
			return await DB.Find<TileBan>().ExecuteAsync();
		}
		public static async Task AddNewBan(short id = 0)
		{
			try
			{
				TileBan tileban = new TileBan(id);
				await tileban.SaveAsync();
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}
		}

		public static async Task RemoveBan(short id)
		{
			if (!(await TileIsBanned(id, null)))
				return;
			try
			{
				await DB.DeleteAsync<TileBan>(x => x.Eq(y=>y.ID, id));
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}
		}

		public static async Task<bool> TileIsBanned(short id)
		{
			return (await DB.CountAsync<TileBan>(x=>x.ID==id)) > 0;
		}

		public static async Task<bool> TileIsBanned(short id, TSPlayer ply)
		{
			TileBan b = await GetBanById(id);
			return !(await b.HasPermissionToPlaceTile(ply));

			return false;
		}

		public static async Task<bool> AllowGroup(short id, string name)
		{
			string groupsNew = "";
			var b = await GetBanById(id);
			if (b != null)
			{
				if (!b.AllowedGroups.Contains(name))
				{
					b.AllowedGroups.Add(name);
					groupsNew = string.Join(",", b.AllowedGroups);
					await b.SaveAsync();
					return true;
				}
			}

			return false;
		}

		public static async Task<bool> RemoveGroup(short id, string name)
		{
			var b = await GetBanById(id);
			if (b != null)
			{
				if (b.RemoveGroup(name))
				{
					await b.SaveAsync();
					return true;
				}
			}

			return false;
		}

		public static async Task<TileBan?> GetBanById(short id)
		{
			return await DB.Find<TileBan>().Match(x=>x.ID==id).ExecuteFirstAsync();
		}
	}

	public class TileBan : MongoDB.Entities.Entity, IEquatable<TileBan>
	{
		public short ID { get; set; }
		public List<string> AllowedGroups { get; set; }

		public TileBan(short id)
			: this()
		{
			ID = id;
			AllowedGroups = new List<string>();
		}

		public TileBan()
		{
			ID = 0;
			AllowedGroups = new List<string>();
		}

		public bool Equals(TileBan other)
		{
			return ID == other.ID;
		}

		public async Task<bool> HasPermissionToPlaceTile(TSPlayer ply)
		{
			if (ply == null)
				return false;

			if (await ply.HasPermission(Permissions.canusebannedtiles))
				return true;

			PermissionHookResult hookResult = PlayerHooks.OnPlayerTilebanPermission(ply, this);
			if (hookResult != PermissionHookResult.Unhandled)
				return hookResult == PermissionHookResult.Granted;

			var cur = ply.Group;
			var traversed = new List<Group>();
			while (cur != null)
			{
				if (AllowedGroups.Contains(cur.Name))
				{
					return true;
				}
				if (traversed.Contains(cur))
				{
					throw new InvalidOperationException(GetString($"Infinite group parenting ({cur.Name})"));
				}
				traversed.Add(cur);
				cur = await GroupManager.GetGroupByName(cur.ParentGroupName);
			}
			return false;
			// could add in the other permissions in this class instead of a giant if switch.
		}

		public void SetAllowedGroups(String groups)
		{
			// prevent null pointer exceptions
			if (!string.IsNullOrEmpty(groups))
			{
				List<String> groupArr = groups.Split(',').ToList();

				for (int i = 0; i < groupArr.Count; i++)
				{
					groupArr[i] = groupArr[i].Trim();
					//Console.WriteLine(groupArr[i]);
				}
				AllowedGroups = groupArr;
			}
		}

		public bool RemoveGroup(string groupName)
		{
			return AllowedGroups.Remove(groupName);
		}

		public override string ToString()
		{
			return ID + (AllowedGroups.Count > 0 ? " (" + String.Join(",", AllowedGroups) + ")" : "");
		}
	}
}
