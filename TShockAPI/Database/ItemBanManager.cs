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
using MongoDB.Bson;
using MongoDB.Driver;
using TShockAPI.Hooks;

namespace TShockAPI.Database
{
	public static class ItemBanManager
	{
		private static IMongoCollection<ItemBan> itemBans => ServerBase.GlobalDatabase.GetCollection<ItemBan>("itembans");

		public static void AddNewBan(string itemname = "")
		{
			try
			{
				ItemBan itemban = new ItemBan(itemname);

				if (!(ItemIsBanned(itemname, null)))
				{
					itemBans.InsertOne(itemban);
				}
			}
			catch (Exception ex)
			{
				ServerBase.Log.Error(ex.ToString());
			}
		}

		public static void RemoveBan(string itemname)
		{
			if (!(ItemIsBanned(itemname, null)))
				return;
			try
			{
				itemBans.DeleteMany<ItemBan>(x => x.Name == itemname);
			}
			catch (Exception ex)
			{
				ServerBase.Log.Error(ex.ToString());
			}
		}

		public static bool ItemIsBanned(string name)
		{
			return itemBans.CountDocuments<ItemBan>(x => x.Name == name) > 0;
		}

		public static bool ItemIsBanned(string name, ServerPlayer ply)
		{
			ItemBan b = GetItemBanByName(name);
			return b != null &&!(b.HasPermissionToUseItem(ply));
		}

		public static bool AllowGroup(string item, string name)
		{
			string groupsNew = "";
			ItemBan b = GetItemBanByName(item);
			if (b != null)
			{
				try
				{
					b.AllowedGroups.Add(name);
					itemBans.FindOneAndReplace(x=> x.Id == b.Id, b);
					return true;
				}
				catch (Exception ex)
				{
					ServerBase.Log.Error(ex.ToString());
				}
			}

			return false;
		}

		public static bool RemoveGroup(string item, string group)
		{
			ItemBan b = GetItemBanByName(item);
			if (b != null)
			{
				try
				{
					b.AllowedGroups.Remove(group);
					itemBans.FindOneAndReplace(x => x.Id == b.Id, b);
					return true;
				}
				catch (Exception ex)
				{
					ServerBase.Log.Error(ex.ToString());
				}
			}

			return false;
		}

		public static ItemBan? GetItemBanByName(string name)
		{
			return itemBans.Find<ItemBan>(x => x.Name == name).FirstOrDefault();
		}
	}

	public class ItemBan : IEquatable<ItemBan>
	{
		public ObjectId Id { get; set; }

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

		public bool HasPermissionToUseItem(ServerPlayer ply)
		{
			if (ply == null)
				return false;

			if (ply.HasPermission(Permissions.usebanneditem))
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
				cur = GroupManager.GetGroupByName(cur.ParentGroupName);
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
