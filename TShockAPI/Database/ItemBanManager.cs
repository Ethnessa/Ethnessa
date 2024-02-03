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
using TShockAPI.Hooks;

namespace TShockAPI.Database
{
	public static class ItemBanManager
	{

		public static async Task AddNewBan(string itemname = "")
		{
			try
			{
				ItemBan itemban = new ItemBan(itemname);

				if (!(await ItemIsBanned(itemname, null)))
					await itemban.SaveAsync();
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}
		}

		public static async Task RemoveBan(string itemname)
		{
			if (!(await ItemIsBanned(itemname, null)))
				return;
			try
			{
				await DB.DeleteAsync<ItemBan>(x => x.Name == itemname);
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}
		}

		public static async Task<bool> ItemIsBanned(string name)
		{
			return await DB.CountAsync<ItemBan>(x => x.Name == name) > 0;
		}

		public static async Task<bool> ItemIsBanned(string name, ServerPlayer ply)
		{
			ItemBan b = await GetItemBanByName(name);
			return b != null &&!(await b.HasPermissionToUseItem(ply));
		}

		public static async Task<bool> AllowGroup(string item, string name)
		{
			string groupsNew = "";
			ItemBan b = await GetItemBanByName(item);
			if (b != null)
			{
				try
				{
					b.AllowedGroups.Add(name);
					await b.SaveAsync();
				}
				catch (Exception ex)
				{
					TShock.Log.Error(ex.ToString());
				}
			}

			return false;
		}

		public static async Task<bool> RemoveGroup(string item, string group)
		{
			ItemBan b = await GetItemBanByName(item);
			if (b != null)
			{
				try
				{
					b.AllowedGroups.Remove(group);
					await b.SaveAsync();

					return true;
				}
				catch (Exception ex)
				{
					TShock.Log.Error(ex.ToString());
				}
			}

			return false;
		}

		public static async Task<ItemBan?> GetItemBanByName(string name)
		{
			return await DB.Find<ItemBan>().Match(x => x.Name == name).ExecuteFirstAsync();
		}
	}

	public class ItemBan : MongoDB.Entities.Entity, IEquatable<ItemBan>
	{
		public string Name { get; set; }
		public List<string> AllowedGroups { get; set; }

		public ItemBan(string name)
			: this()
		{
			Name = name;
			AllowedGroups = new List<string>();
		}

		public ItemBan()
		{
			Name = "";
			AllowedGroups = new List<string>();
		}

		public bool Equals(ItemBan other)
		{
			return Name == other.Name;
		}

		public async Task<bool> HasPermissionToUseItem(ServerPlayer ply)
		{
			if (ply == null)
				return false;

			if (await ply.HasPermission(Permissions.usebanneditem))
				return true;

			PermissionHookResult hookResult = PlayerHooks.OnPlayerItembanPermission(ply, this);
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
					throw new InvalidOperationException("Infinite group parenting ({0})".SFormat(cur.Name));
				}
				traversed.Add(cur);
				cur = await GroupManager.GetGroupByName(cur.ParentGroupName);
			}
			return false;
			// could add in the other permissions in this class instead of a giant if switch.
		}

		public override string ToString()
		{
			return Name + (AllowedGroups.Count > 0 ? " (" + String.Join(",", AllowedGroups) + ")" : "");
		}
	}
}
