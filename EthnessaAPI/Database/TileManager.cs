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
using EthnessaAPI.Hooks;
using MongoDB.Bson;
using MongoDB.Driver;
using Terraria;

namespace EthnessaAPI.Database
{
	public static class TileManager
	{
		private static IMongoCollection<TileBan> tileBans => ServerBase.GlobalDatabase.GetCollection<TileBan>("tilebans");
		public static List<TileBan> ToListAsync()
		{
			return tileBans.Find(Builders<TileBan>.Filter.Empty).ToList();
		}
		public static void AddNewBan(short id = 0)
		{
			try
			{
				TileBan tileban = new TileBan(id);
				tileBans.InsertOne(tileban);
			}
			catch (Exception ex)
			{
				ServerBase.Log.Error(ex.ToString());
			}
		}

		public static void RemoveBan(short id)
		{
			if (!(TileIsBanned(id, null)))
				return;
			try
			{
				tileBans.FindOneAndDelete<TileBan>(x=>x.Type==id);
			}
			catch (Exception ex)
			{
				ServerBase.Log.Error(ex.ToString());
			}
		}

		public static bool TileIsBanned(short id)
		{
			return (tileBans.Count<TileBan>(x=>x.Type==id)) > 0;
		}

		public static bool TileIsBanned(short id, ServerPlayer ply)
		{
			TileBan b = GetBanById(id);
			return !(b.HasPermissionToPlaceTile(ply));

			return false;
		}

		public static bool AllowGroup(short id, string name)
		{
			string groupsNew = "";
			var b = GetBanById(id);
			if (b != null)
			{
				if (!b.AllowedGroups.Contains(name))
				{
					b.AllowedGroups.Add(name);
					groupsNew = string.Join(",", b.AllowedGroups);
					tileBans.ReplaceOne(x => x.Type == id, b);
					return true;
				}
			}

			return false;
		}

		public static bool RemoveGroup(short id, string name)
		{
			var b = GetBanById(id);
			if (b != null)
			{
				if (b.RemoveGroup(name))
				{
					tileBans.ReplaceOne(x=> x.Type == id, b);
					return true;
				}
			}

			return false;
		}

		public static TileBan? GetBanById(short id)
		{
			return tileBans.Find<TileBan>(x=>x.Type==id).FirstOrDefault();
		}
	}

	public class TileBan : IEquatable<TileBan>
	{
		public ObjectId Id { get; set; }
		public short Type { get; set; }
		public List<string> AllowedGroups { get; set; }

		public TileBan(short type)
			: this()
		{
			Type = type;
			AllowedGroups = new List<string>();
		}

		public TileBan()
		{
			Type = 0;
			AllowedGroups = new List<string>();
		}

		public bool Equals(TileBan other)
		{
			return Type == other.Type;
		}

		public bool HasPermissionToPlaceTile(ServerPlayer ply)
		{
			if (ply == null)
				return false;

			if (ply.HasPermission(Permissions.canusebannedtiles))
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
				cur = GroupManager.GetGroupByName(cur.ParentGroupName);
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
			return Type + (AllowedGroups.Count > 0 ? " (" + String.Join(",", AllowedGroups) + ")" : "");
		}
	}
}
