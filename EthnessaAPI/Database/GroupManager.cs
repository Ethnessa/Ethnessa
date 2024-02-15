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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace EthnessaAPI.Database
{
	/// <summary>
	/// Represents the GroupManager, which is in charge of group management.
	/// </summary>
	public static class GroupManager
	{
		public static IMongoCollection<Group> groups => ServerBase.GlobalDatabase.GetCollection<Group>("groups");
		public static List<Group> GetGroups()
		{
			return groups.Find(Builders<Group>.Filter.Empty).ToList();
		}

		public static void EnsureDefaultGroups()
		{
			var serverInfo = ServerInfoManager.RetrieveServerInfo();

			if (serverInfo.DefaultGroupsCreatedOnce)
			{
				return;
			}

			// Add default groups if they don't exist
			AddDefaultGroup("guest", "",
				new List<string>(){
					Permissions.canbuild,
					Permissions.canregister,
					Permissions.canlogin,
					Permissions.canpartychat,
					Permissions.cantalkinthird,
					Permissions.canchat,
					Permissions.synclocalarea,
					Permissions.sendemoji});

			AddDefaultGroup("default", "guest",
				new List<string>(){
					Permissions.warp,
					Permissions.canchangepassword,
					Permissions.canlogout,
					Permissions.summonboss,
					Permissions.whisper,
					Permissions.wormhole,
					Permissions.canpaint,
					Permissions.pylon,
					Permissions.tppotion,
					Permissions.magicconch,
					Permissions.demonconch});

			// admin is different than in TShock, it has all permissions
			// we don't have a super-admin group because... why would we?
			// instead of filling the database with a bunch of garbage groups,
			// we are giving full control to the owner of the server.
			AddDefaultGroup("admin", "default",
				new List<string>(){"*"});

			Group.DefaultGroup = GetGroupByName(ServerBase.Config.Settings.DefaultGuestGroupName);
			AssertCoreGroupsPresent();

			serverInfo.DefaultGroupsCreatedOnce = true;
			ServerInfoManager.SaveServerInfo(serverInfo);
		}

		internal static void AssertCoreGroupsPresent()
		{
			if (!(GroupExists(ServerBase.Config.Settings.DefaultGuestGroupName)))
			{
				ServerBase.Log.ConsoleError(GetString("The guest group could not be found. This may indicate a typo in the configuration file, or that the group was renamed or deleted."));
				throw new Exception(GetString("The guest group could not be found."));
			}

			if (!(GroupExists(ServerBase.Config.Settings.DefaultRegistrationGroupName)))
			{
				ServerBase.Log.ConsoleError(GetString("The default usergroup could not be found. This may indicate a typo in the configuration file, or that the group was renamed or deleted."));
				throw new Exception(GetString("The default usergroup could not be found."));
			}
		}

		/// <summary>
		/// Asserts that the group reference can be safely assigned to the player object.
		/// <para>If this assertion fails, and <paramref name="kick"/> is true, the player is disconnected. If <paramref name="kick"/> is false, the player will receive an error message.</para>
		/// </summary>
		/// <param name="player">The player in question</param>
		/// <param name="group">The group we want to assign them</param>
		/// <param name="kick">Whether or not failing this check disconnects the player.</param>
		/// <returns></returns>
		public static bool AssertGroupValid(ServerPlayer player, Group group, bool kick)
		{
			if (group == null)
			{
				if (kick)
					player.Disconnect(GetString("Your account's group could not be loaded. Please contact server administrators about this."));
				else
					player.SendErrorMessage(GetString("Your account's group could not be loaded. Please contact server administrators about this."));
				return false;
			}

			return true;
		}

		private static void AddDefaultGroup(string name, string parent, List<string> permissions)
		{
			if (!(GroupExists(name))){
				AddGroup(name, parent, permissions, Group.DefaultChatColor);
			}
		}

		/// <summary>
		/// Determines whether the given group exists.
		/// </summary>
		/// <param name="group">The group.</param>
		/// <returns><c>true</c> if it does; otherwise, <c>false</c>.</returns>
		public static bool GroupExists(string group) => groups.CountDocuments(g => g.Name.Equals(group)) > 0;

		/// <summary>
		/// Gets the group matching the specified name.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns>The group.</returns>
		public static Group? GetGroupByName(string? name)
		{
			if (name is null)
			{
				return null;
			}

			var ret = groups.Find<Group>(x => x.Name == name).FirstOrDefault();
			return ret;
		}

		/// <summary>
		/// Adds group with name and permissions if it does not exist.
		/// </summary>
		/// <param name="name">name of group</param>
		/// <param name="parentName">parent of group</param>
		/// <param name="permissions">permissions</param>
		/// <param name="chatColor">chatcolor</param>
		public static void AddGroup(String name, string parentName, List<string> permissions, String chatColor)
		{
			if (GroupExists(name))
			{
				throw new GroupExistsException(name);
			}

			var group = new Group(name, null, chatColor)
			{
				Permissions = permissions
			};

			if (!string.IsNullOrWhiteSpace(parentName))
			{
				var parent = GetGroupByName(parentName);
				if (parent == null || name == parentName)
				{
					var error = GetString($"Invalid parent group {parentName} for group {group.Name}");
					ServerBase.Log.ConsoleError(error);
					throw new GroupManagerException(error);
				}
				group.ParentGroupName = parent.Name;
			}

			groups.InsertOne(group);
		}

		/// <summary>
		/// Updates a group including permissions
		/// </summary>
		/// <param name="name">name of the group to update</param>
		/// <param name="parentname">parent of group</param>
		/// <param name="permissions">permissions</param>
		/// <param name="chatcolor">chatcolor</param>
		/// <param name="prefix">prefix</param>
		/// <param name="suffix">suffix</param>
		public static void UpdateGroup(string name, string parentname, List<string> permissions, string chatcolor, string prefix, string suffix)
		{
			Group group = GetGroupByName(name);
			if (group == null)
				throw new GroupNotExistException(name);

			Group parent = null;
			if (!string.IsNullOrWhiteSpace(parentname))
			{
				parent = GetGroupByName(parentname);
				if (parent == null || parent == group)
					throw new GroupManagerException(GetString($"Invalid parent group {parentname} for group {name}."));

				// Check if the new parent would cause loops.
				List<Group> groupChain = new List<Group> { group, parent };
				Group? checkingGroup = GetGroupByName(parent.ParentGroupName);
				while (checkingGroup != null)
				{
					if (groupChain.Contains(checkingGroup))
						throw new GroupManagerException(
							GetString($"Parenting group {group} to {parentname} would cause loops in the parent chain."));

					groupChain.Add(checkingGroup);
					checkingGroup = GetGroupByName(parent.ParentGroupName);
				}
			}

			group.Prefix = prefix;
			group.Suffix = suffix;

			group.ChatColor = chatcolor;
			group.Permissions = permissions;
			group.ParentGroupName = parent.Name;
			group.Prefix = prefix;
			group.Suffix = suffix;

			groups.InsertOne(group);
		}

		/// <summary>
		/// Renames the specified group.
		/// </summary>
		/// <param name="name">The group's name.</param>
		/// <param name="newName">The new name.</param>
		/// <returns>The result from the operation to be sent back to the user.</returns>
		public static string RenameGroup(string name, string newName)
		{
			if (!(GroupExists(name)))
			{
				throw new GroupNotExistException(name);
			}

			if ((GroupExists(newName)))
			{
				throw new GroupExistsException(newName);
			}


			var group = GetGroupByName(name);

			if (group is null)
			{
				return GetString($"Invalid group name: {name}");
			}

			group.Name = newName;
			groups.ReplaceOne(g => g.Id == group.Id, group);

			var childGroups = groups.Find<Group>(x => x.ParentGroupName ==name)
				.ToList();

			foreach (Group grp in childGroups)
			{
				grp.ParentGroupName = newName;
				groups.ReplaceOne(g => g.Id == grp.Id, grp);
			}

			ServerBase.Config.Read(FileTools.ConfigPath, out bool writeConfig);
			if (ServerBase.Config.Settings.DefaultGuestGroupName == name)
			{
				ServerBase.Config.Settings.DefaultGuestGroupName = newName;
				Group.DefaultGroup = group;
			}
			if (ServerBase.Config.Settings.DefaultRegistrationGroupName == name)
			{
				ServerBase.Config.Settings.DefaultRegistrationGroupName = newName;
			}
			if (writeConfig)
			{
				ServerBase.Config.Write(FileTools.ConfigPath);
			}

			UserAccountManager.FixGroupName(name,newName);
			return GetString($"Group {name} has been renamed to {newName}.");

		}

		/// <summary>
		/// Deletes the specified group.
		/// </summary>
		/// <param name="name">The group's name.</param>
		/// <param name="exceptions">Whether exceptions will be thrown in case something goes wrong.</param>
		/// <returns>The result from the operation to be sent back to the user.</returns>
		public static string DeleteGroup(string name, bool exceptions = false)
		{
			if (name == Group.DefaultGroup.Name)
			{
				if (exceptions)
					throw new GroupManagerException(GetString("You can't remove the default guest group."));
				return GetString("You can't remove the default guest group.");
			}

			var group = groups.Find<Group>(x => x.Name == name)
				.FirstOrDefault();

			if (group is null)
			{
				return GetString($"Group {name} doesn't exist.");
			}

			groups.DeleteOne(x => x.Id == group.Id);

			if (exceptions)
				throw new GroupManagerException(GetString($"Failed to delete group {name}."));
			return GetString($"Failed to delete group {name}.");
		}

		/// <summary>
		/// Enumerates the given permission list and adds permissions for the specified group accordingly.
		/// </summary>
		/// <param name="name">The group name.</param>
		/// <param name="permissions">The permission list.</param>
		/// <returns>The result from the operation to be sent back to the user.</returns>
		public static string AddPermissions(string name, List<string> permissions)
		{
			var group = GetGroupByName(name);
			if (group is null)
			{
				return GetString($"Group {name} doesn't exist.");
			}

			group.Permissions.AddRange(permissions);
			groups.ReplaceOne(x => x.Id == group.Id, group);
			return "";
		}

		/// <summary>
		/// Enumerates the given permission list and removes valid permissions for the specified group accordingly.
		/// </summary>
		/// <param name="name">The group name.</param>
		/// <param name="permissions">The permission list.</param>
		/// <returns>The result from the operation to be sent back to the user.</returns>
		public static string DeletePermissions(String name, List<String> permissions)
		{
			var group = GetGroupByName(name);
			if (group is null)
			{
				return GetString($"Group {name} doesn't exist.");
			}

			permissions.ForEach(p => group.RemovePermission(p));
			groups.ReplaceOne(x => x.Id == group.Id, group);
			return "";
		}

	}

	/// <summary>
	/// Represents the base GroupManager exception.
	/// </summary>
	[Serializable]
	public class GroupManagerException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="GroupManagerException"/> with the specified message.
		/// </summary>
		/// <param name="message">The message.</param>
		public GroupManagerException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GroupManagerException"/> with the specified message and inner exception.
		/// </summary>
		/// <param name="message">The message.</param>
		/// <param name="inner">The inner exception.</param>
		public GroupManagerException(string message, Exception inner)
			: base(message, inner)
		{
		}
	}

	/// <summary>
	/// Represents the GroupExists exception.
	/// This exception is thrown whenever an attempt to add an existing group into the database is made.
	/// </summary>
	[Serializable]
	public class GroupExistsException : GroupManagerException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="GroupExistsException"/> with the specified group name.
		/// </summary>
		/// <param name="name">The group name.</param>
		public GroupExistsException(string name)
			: base(GetString($"Group {name} already exists"))
		{
		}
	}

	/// <summary>
	/// Represents the GroupNotExist exception.
	/// This exception is thrown whenever we try to access a group that does not exist.
	/// </summary>
	[Serializable]
	public class GroupNotExistException : GroupManagerException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="GroupNotExistException"/> with the specified group name.
		/// </summary>
		/// <param name="name">The group name.</param>
		public GroupNotExistException(string name)
			: base(GetString($"Group {name} does not exist"))
		{
		}
	}
}
