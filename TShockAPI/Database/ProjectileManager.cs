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
using System.Threading.Tasks;
using MongoDB.Entities;
using TShockAPI.Hooks;

namespace TShockAPI.Database
{
	public static class ProjectileManager
	{
		public static async Task AddNewBan(short id = 0)
		{
			try
			{
				ProjectileBan ban = new ProjectileBan(id);
				await ban.SaveAsync();
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}
		}

		public static async Task RemoveBan(short id)
		{
			if (!(await ProjectileIsBanned(id, null)))
				return;
			try
			{
				await DB.DeleteAsync<ProjectileBan>(x => x.Type == id);
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}
		}

		public static async Task<bool> ProjectileIsBanned(short id)
		{
			return await DB.CountAsync<ProjectileBan>(x => x.Type == id) > 0;
		}

		public static async Task<bool> ProjectileIsBanned(short id, TSPlayer ply)
		{
			if (await ProjectileIsBanned(id))
			{
				ProjectileBan b = await GetBanById(id);
				return !(await b.HasPermissionToCreateProjectile(ply));
			}

			return false;
		}

		public static async Task<bool> AllowGroup(short id, string name)
		{
			string groupsNew = "";
			ProjectileBan b = await GetBanById(id);
			if (b != null)
			{
				try
				{
					b.AllowedGroups.Add(name);
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

		public static async Task<bool> RemoveGroup(short id, string group)
		{
			ProjectileBan b = await GetBanById(id);
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

		public static async Task<ProjectileBan?> GetBanById(short id)
		{
			return await DB.Find<ProjectileBan>().Match(x => x.Type == id)
				.ExecuteFirstAsync();
		}
	}

	public class ProjectileBan : Entity, IEquatable<ProjectileBan>
	{
		public short Type { get; set; }
		public List<string> AllowedGroups { get; set; }

		public ProjectileBan(short type)
			: this()
		{
			Type = type;
			AllowedGroups = new List<string>();
		}

		public ProjectileBan()
		{
			Type = 0;
			AllowedGroups = new List<string>();
		}

		public bool Equals(ProjectileBan other)
		{
			return Type == other.Type;
		}

		public async Task<bool> HasPermissionToCreateProjectile(TSPlayer ply)
		{
			if (ply == null)
				return false;

			if (await ply.HasPermission(Permissions.canusebannedprojectiles))
				return true;

			PermissionHookResult hookResult = PlayerHooks.OnPlayerProjbanPermission(ply, this);
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

		public override string ToString()
		{
			return Type + (AllowedGroups.Count > 0 ? " (" + String.Join(",", AllowedGroups) + ")" : "");
		}
	}
}
