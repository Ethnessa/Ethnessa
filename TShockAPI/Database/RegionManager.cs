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
using Terraria;
using Microsoft.Xna.Framework;
using MongoDB.Driver;
using MongoDB.Entities;
using Entity = MongoDB.Entities.Entity;

namespace TShockAPI.Database
{
	/// <summary>
	/// Represents the Region database manager.
	/// </summary>
	public static class RegionManager
	{
		public static async Task<int> CountRegions()
		{
			return Convert.ToInt32(await DB.CountAsync<Region>());
		}

		/// <summary>
		/// Adds a region to the database.
		/// </summary>
		/// <param name="tx">TileX of the top left corner.</param>
		/// <param name="ty">TileY of the top left corner.</param>
		/// <param name="width">Width of the region in tiles.</param>
		/// <param name="height">Height of the region in tiles.</param>
		/// <param name="regionname">The name of the region.</param>
		/// <param name="owner">The User Account Name of the person who created this region.</param>
		/// <param name="worldid">The world id that this region is in.</param>
		/// <param name="z">The Z index of the region.</param>
		/// <returns>Whether the region was created and added successfully.</returns>
		public static async Task<bool> AddRegion(int tx, int ty, int width, int height, string regionname, string owner, string worldid, int z = 0)
		{
			if (await GetRegionByName(regionname) != null)
			{
				return false;
			}
			try
			{
				int nextId = await CountRegions();
				Region region = new Region(nextId, new Rectangle(tx, ty, width, height), regionname, owner, true, worldid, z);
				Hooks.RegionHooks.OnRegionCreated(region);
				return true;
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}
			return false;
		}

		/// <summary>
		/// Deletes the region from this world with a given ID.
		/// </summary>
		/// <param name="id">The ID of the region to delete.</param>
		/// <returns>Whether the region was successfully deleted.</returns>
		public static async Task<bool> DeleteRegion(int id)
		{
			try
			{
				var worldid = Main.worldID.ToString();

				var region = await DB.Find<Region>().Match(x=>x.ID == id && x.WorldID == worldid)
					.ExecuteFirstAsync();

				if (region is not null)
				{
					Hooks.RegionHooks.OnRegionDeleted(region);
					await region.DeleteAsync();
				}

				return true;
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}
			return false;
		}

		/// <summary>
		/// Deletes the region from this world with a given name.
		/// </summary>
		/// <param name="name">The name of the region to delete.</param>
		/// <returns>Whether the region was successfully deleted.</returns>
		public static async Task<bool> DeleteRegion(string name)
		{
			try
			{
				var worldid = Main.worldID.ToString();

				var region = await DB.Find<Region>().Match(x=>x.Name == name && x.WorldID == worldid)
					.ExecuteFirstAsync();

				if (region is not null)
				{
					Hooks.RegionHooks.OnRegionDeleted(region);
					await region.DeleteAsync();
				}

				return true;
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}
			return false;
		}

		/// <summary>
		/// Sets the protected state of the region with a given ID.
		/// </summary>
		/// <param name="id">The ID of the region to change.</param>
		/// <param name="state">New protected state of the region.</param>
		/// <returns>Whether the region's state was successfully changed.</returns>
		public static async Task<bool> SetRegionState(int id, bool state)
		{
			try
			{
				var worldId = Main.worldID.ToString();

				var region = await DB.Find<Region>().Match(x=>x.ID == id && x.WorldID == worldId)
					.ExecuteFirstAsync();

				if (region is not null)
				{
					region.DisableBuild = state;
					await region.SaveAsync();
				}
				return true;
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
				return false;
			}
		}

		/// <summary>
		/// Sets the protected state of the region with a given name.
		/// </summary>
		/// <param name="name">The name of the region to change.</param>
		/// <param name="state">New protected state of the region.</param>
		/// <returns>Whether the region's state was successfully changed.</returns>
		public static async Task<bool> SetRegionState(string name, bool state)
		{
			try
			{
				var region = await GetRegionByName(name);
				if (region is not null)
				{
					region.DisableBuild = state;
					await region.SaveAsync();
				}

				return true;
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
				return false;
			}
		}

		/// <summary>
		/// Checks if a given player can build in a region at the given (x, y) coordinate
		/// </summary>
		/// <param name="x">X coordinate</param>
		/// <param name="y">Y coordinate</param>
		/// <param name="ply">Player to check permissions with</param>
		/// <returns>Whether the player can build at the given (x, y) coordinate</returns>
		public static async Task<bool> CanBuild(int x, int y, TSPlayer ply)
		{
			if (!(await ply.HasPermission(Permissions.canbuild)))
			{
				return false;
			}
			Region top = null;

			var regions = await DB.Find<Region>().Match(r => r.InArea(x, y) && r.WorldID == Main.worldID.ToString())
				.ExecuteAsync();

			foreach (Region region in regions)
			{
				if (region.InArea(x, y))
				{
					if (top == null || region.Z > top.Z)
						top = region;
				}
			}
			return top == null || await top.HasPermissionToBuildInRegion(ply);
		}

		/// <summary>
		/// Checks if any regions exist at the given (x, y) coordinate
		/// </summary>
		/// <param name="x">X coordinate</param>
		/// <param name="y">Y coordinate</param>
		/// <returns>Whether any regions exist at the given (x, y) coordinate</returns>
		public static async Task<bool> InArea(int x, int y)
		{
			return await DB.CountAsync<Region>(r => r.InArea(x, y)) > 0;
		}

		/// <summary>
		/// Checks if any regions exist at the given (x, y) coordinate
		/// and returns an IEnumerable containing their <see cref="Region"/> objects
		/// </summary>
		/// <param name="x">X coordinate</param>
		/// <param name="y">Y coordinate</param>
		/// <returns>The <see cref="Region"/> objects of any regions that exist at the given (x, y) coordinate</returns>
		public static async Task<IEnumerable<Region>> InAreaRegion(int x, int y)
		{
			return await DB.Find<Region>().Match(r => r.InArea(x, y)).ExecuteAsync();
		}

		/// <summary>
		/// Changes the size of a given region
		/// </summary>
		/// <param name="regionName">Name of the region to resize</param>
		/// <param name="addAmount">Amount to resize</param>
		/// <param name="direction">Direction to resize in:
		/// 0 = resize height and Y.
		/// 1 = resize width.
		/// 2 = resize height.
		/// 3 = resize width and X.</param>
		/// <returns></returns>
		public static async Task<bool> ResizeRegion(string regionName, int addAmount, int direction)
		{
			//0 = up
			//1 = right
			//2 = down
			//3 = left
			int X = 0;
			int Y = 0;
			int height = 0;
			int width = 0;

			try
			{
				Region region = await GetRegionByName(regionName);
				if (region is null)
				{
					return false;
				}

				switch (direction)
				{
					case 0:
						Y -= addAmount;
						height += addAmount;
						break;
					case 1:
						width += addAmount;
						break;
					case 2:
						height += addAmount;
						break;
					case 3:
						X -= addAmount;
						width += addAmount;
						break;
					default:
						return false;
				}

				region.Area = new Rectangle(X, Y, width, height);
				await region.SaveAsync();
				return true;
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}
			return false;
		}

		/// <summary>
		/// Renames a region
		/// </summary>
		/// <param name="oldName">Name of the region to rename</param>
		/// <param name="newName">New name of the region</param>
		/// <returns>true if renamed successfully, false otherwise</returns>
		public static async Task<bool> RenameRegion(string oldName, string newName)
		{
			try
			{
				Region region = await GetRegionByName(oldName);
				if (region is null)
				{
					return false;
				}


				region.Name = newName;
				await region.SaveAsync();
				Hooks.RegionHooks.OnRegionRenamed(region, oldName, newName);
				return true;

			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}

			return false;
		}

		/// <summary>
		/// Removes an allowed user from a region
		/// </summary>
		/// <param name="regionName">Name of the region to modify</param>
		/// <param name="userName">Username to remove</param>
		/// <returns>true if removed successfully</returns>
		public static async Task<bool> RemoveUser(string regionName, string userName)
		{
			Region r = await GetRegionByName(regionName);
			if (r == null) return false;

			if (!r.RemoveID((await UserAccountManager.GetUserAccountByName(userName))?.ID ?? 0))
			{
				return false;
			}

			await r.SaveAsync();
			return true;
		}

		/// <summary>
		/// Adds a user to a region's allowed user list
		/// </summary>
		/// <param name="regionName">Name of the region to modify</param>
		/// <param name="userName">Username to add</param>
		/// <returns>true if added successfully</returns>
		public static async Task<bool> AddNewUser(string regionName, string userName)
		{
			try
			{
				Region region = await GetRegionByName(regionName);
				var userId = (await UserAccountManager.GetUserAccountByName(userName))?.ID;

				if (userId is null)
					return false;

				// Is the user already allowed to the region?
				if (region.AllowedIDs.Contains(userId.GetValueOrDefault()))
					return true;

				region.AllowedIDs.Add(userId.GetValueOrDefault());
				await region.SaveAsync();
				return true;
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}
			return false;
		}

		/// <summary>
		/// Sets the position of a region.
		/// </summary>
		/// <param name="regionName">The region name.</param>
		/// <param name="x">The X position.</param>
		/// <param name="y">The Y position.</param>
		/// <param name="height">The height.</param>
		/// <param name="width">The width.</param>
		/// <returns>Whether the operation succeeded.</returns>
		public static async Task<bool> PositionRegion(string regionName, int x, int y, int width, int height)
		{
			try
			{
				Region region = await GetRegionByName(regionName);
				if (region is null)
				{
					throw new Exception("Region not found");
				}

				region.Area = new Rectangle(x, y, width, height);

				await region.SaveAsync();
				return true;
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}
			return false;
		}

		/// <summary>
		/// Gets all the regions names from world
		/// </summary>
		/// <param name="worldId">World name to get regions from</param>
		/// <returns>List of regions with only their names</returns>
		public static async Task<List<Region>> ListAllRegions(string worldId)
		{
			var regions = new List<Region>();
			try
			{
				regions = await DB.Find<Region>().Match(x => x.WorldID == worldId).ExecuteAsync();
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}
			return regions;
		}

		/// <summary>
		/// Returns a region with the given name
		/// </summary>
		/// <param name="name">Region name</param>
		/// <returns>The region with the given name, or null if not found</returns>
		public static async Task<Region?> GetRegionByName(String name)
		{
			return await DB.Find<Region>().Match(x => x.Name == name && x.WorldID == Main.worldID.ToString())
				.ExecuteFirstAsync();
		}

		/// <summary>
		/// Returns a region with the given ID
		/// </summary>
		/// <param name="id">Region ID</param>
		/// <returns>The region with the given ID, or null if not found</returns>
		public static async Task<Region?> GetRegionByID(int id)
		{
			return await DB.Find<Region>().Match(x => x.ID == id && x.WorldID == Main.worldID.ToString())
				.ExecuteFirstAsync();
		}

		/// <summary>
		/// Changes the owner of the region with the given name
		/// </summary>
		/// <param name="regionName">Region name</param>
		/// <param name="newOwner">New owner's username</param>
		/// <returns>Whether the change was successful</returns>
		public static async Task<bool> ChangeOwner(string regionName, string newOwner)
		{
			var region = await GetRegionByName(regionName);
			if (region == null) return false;

			region.Owner = newOwner;
			await region.SaveAsync();
			return true;
		}

		/// <summary>
		/// Allows a group to use a region
		/// </summary>
		/// <param name="regionName">Region name</param>
		/// <param name="groupName">Group's name</param>
		/// <returns>Whether the change was successful</returns>
		public static async Task<bool> AllowGroup(string regionName, string groupName)
		{
			var region = await GetRegionByName(regionName);
			if (region is null)
			{
				return false;
			}

			// Is the group already allowed to the region?
			if (region.AllowedGroups.Contains(groupName))
				return true;

			region.AllowedGroups.Add(groupName);
			await region.SaveAsync();
			return true;
		}

		/// <summary>
		/// Removes a group's access to a region
		/// </summary>
		/// <param name="regionName">Region name</param>
		/// <param name="group">Group name</param>
		/// <returns>Whether the change was successful</returns>
		public static async Task<bool> RemoveGroup(string regionName, string group)
		{
			var region = await GetRegionByName(regionName);
			if (region is null)
			{
				return false;
			}

			if (!region.RemoveGroup(group))
			{
				return false;
			}

			await region.SaveAsync();
			return true;
		}

		/// <summary>
		/// Returns the <see cref="Region"/> with the highest Z index of the given list
		/// </summary>
		/// <param name="regions">List of Regions to compare</param>
		/// <returns></returns>
		public static async Task<Region> GetTopRegion(IEnumerable<Region> regions)
		{
			Region ret = null;
			foreach (Region r in regions)
			{
				if (ret == null)
					ret = r;
				else
				{
					if (r.Z > ret.Z)
						ret = r;
				}
			}
			return ret;
		}

		/// <summary>
		/// Sets the Z index of a given region
		/// </summary>
		/// <param name="name">Region name</param>
		/// <param name="z">New Z index</param>
		/// <returns>Whether the change was successful</returns>
		public static async Task<bool> SetZ(string name, int z)
		{
			try
			{
				var region = await GetRegionByName(name);
				if (region is null) return false;

				region.Z = z;
				await region.SaveAsync();
				return true;
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
				return false;
			}
		}
	}

	public class Region : Entity
	{
		public int ID { get; set; }
		public Rectangle Area { get; set; }
		public string Name { get; set; }
		public string Owner { get; set; }
		public bool DisableBuild { get; set; }
		public string WorldID { get; set; }
		public List<int> AllowedIDs { get; set; }
		public List<string> AllowedGroups { get; set; }
		public int Z { get; set; }

		public Region(int id, Rectangle region, string name, string owner, bool disablebuild, string RegionWorldIDz, int z)
			: this()
		{
			ID = id;
			Area = region;
			Name = name;
			Owner = owner;
			DisableBuild = disablebuild;
			WorldID = RegionWorldIDz;
			Z = z;
		}

		public Region()
		{
			Area = Rectangle.Empty;
			Name = string.Empty;
			DisableBuild = true;
			WorldID = string.Empty;
			AllowedIDs = new List<int>();
			AllowedGroups = new List<string>();
			Z = 0;
		}

		/// <summary>
		/// Checks if a given point is in the region's area
		/// </summary>
		/// <param name="point">Point to check</param>
		/// <returns>Whether the point exists in the region's area</returns>
		public bool InArea(Rectangle point)
		{
			return InArea(point.X, point.Y);
		}

		/// <summary>
		/// Checks if a given (x, y) coordinate is in the region's area
		/// </summary>
		/// <param name="x">X coordinate to check</param>
		/// <param name="y">Y coordinate to check</param>
		/// <returns>Whether the coordinate exists in the region's area</returns>
		public bool InArea(int x, int y) //overloaded with x,y
		{
			/*
			DO NOT CHANGE TO Area.Contains(x, y)!
			Area.Contains does not account for the right and bottom 'border' of the rectangle,
			which results in regions being trimmed.
			*/
			return x >= Area.X && x <= Area.X + Area.Width && y >= Area.Y && y <= Area.Y + Area.Height;
		}

		/// <summary>
		/// Checks if a given player has permission to build in the region
		/// </summary>
		/// <param name="ply">Player to check permissions with</param>
		/// <returns>Whether the player has permission</returns>
		public async Task<bool> HasPermissionToBuildInRegion(TSPlayer ply)
		{
			if (!DisableBuild)
			{
				return true;
			}
			if (!ply.IsLoggedIn)
			{
				if (!ply.HasBeenNaggedAboutLoggingIn)
				{
					ply.SendMessage(GetString("You must be logged in to take advantage of protected regions."), Color.Red);
					ply.HasBeenNaggedAboutLoggingIn = true;
				}
				return false;
			}

			return await ply.HasPermission(Permissions.editregion) || AllowedIDs.Contains(ply.Account.ID) || AllowedGroups.Contains(ply.Group.Name) || Owner == ply.Account.Name;
		}

		/// <summary>
		/// Sets the user IDs which are allowed to use the region
		/// </summary>
		/// <param name="ids">String of IDs to set</param>
		public void SetAllowedIDs(String ids)
		{
			String[] idArr = ids.Split(',');
			List<int> idList = new List<int>();

			foreach (String id in idArr)
			{
				int i = 0;
				if (int.TryParse(id, out i) && i != 0)
				{
					idList.Add(i);
				}
			}
			AllowedIDs = idList;
		}

		/// <summary>
		/// Sets the group names which are allowed to use the region
		/// </summary>
		/// <param name="groups">String of group names to set</param>
		public void SetAllowedGroups(String groups)
		{
			// prevent null pointer exceptions
			if (!string.IsNullOrEmpty(groups))
			{
				List<String> groupList = groups.Split(',').ToList();

				for (int i = 0; i < groupList.Count; i++)
				{
					groupList[i] = groupList[i].Trim();
				}

				AllowedGroups = groupList;
			}
		}

		/// <summary>
		/// Removes a user's access to the region
		/// </summary>
		/// <param name="id">User ID to remove</param>
		/// <returns>true if the user was found and removed from the region's allowed users</returns>
		public bool RemoveID(int id)
		{
			return AllowedIDs.Remove(id);
		}

		/// <summary>
		/// Removes a group's access to the region
		/// </summary>
		/// <param name="groupName">Group name to remove</param>
		/// <returns></returns>
		public bool RemoveGroup(string groupName)
		{
			return AllowedGroups.Remove(groupName);
		}
	}
}
