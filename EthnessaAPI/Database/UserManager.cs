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
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BCrypt.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using EthnessaAPI.Database.Models;
using MongoDB.Driver;

namespace EthnessaAPI.Database
{
	/// <summary>UserAccountManager - Methods for dealing with database user accounts and other related functionality within TShock.</summary>
	public static class UserAccountManager
	{
		internal static string _collectionName => "user_accounts";
		private static IMongoCollection<UserAccount> userAccounts => ServerBase.GlobalDatabase.GetCollection<UserAccount>(_collectionName);
		/// <summary>
		/// Adds the given user account to the database
		/// </summary>
		/// <param name="account">The user account to be added</param>
		/// <remarks>You can also use UserAccount.SaveAsync(), however you should check if it already exists first.</remarks>
		public static void AddUserAccount(UserAccount account)
		{
			if (!(GroupManager.GroupExists(account.Groups.FirstOrDefault())))
				throw new GroupNotExistsException(account.Groups.FirstOrDefault());

			try
			{
				// does account already exist?
				var alreadyExists = userAccounts.CountDocuments<UserAccount>(x => x.Name == account.Name) > 0;
				if (alreadyExists)
				{
					throw new UserAccountExistsException(account.Name);
				}

				account.AccountId = CounterManager.GetAndIncrement(_collectionName);
				userAccounts.InsertOne(account);
			}
			catch (Exception ex)
			{
				throw new UserAccountManagerException(GetString($"AddUser returned an error ({ex.Message})"), ex);
			}

			Hooks.AccountHooks.OnAccountCreate(account);
		}

		/// <summary>
		/// Removes all user accounts from the database whose usernames match the given user account
		/// </summary>
		/// <param name="account">The user account</param>
		public static void RemoveUserAccount(UserAccount account)
		{
			try
			{
				userAccounts.FindOneAndDelete<UserAccount>(x => x.Name == account.Name);
			}
			catch (Exception ex)
			{
				throw new UserAccountManagerException(GetString($"RemoveUser returned an error ({ex.Message})"), ex);
			}

			Hooks.AccountHooks.OnAccountDelete(account);
		}

		public static bool AddTag(UserAccount account, string tagName)
		{
			var tag = TagManager.GetTag(tagName);
			if (tag is null)
			{
				return false;
			}

			if(account.TagStatuses.Any(x=>x.Name==tagName))
			{
				return false;
			}

			account.TagStatuses.Add(new(tagName, true));
			userAccounts.ReplaceOne(acc => acc.Id == account.Id, account);
			return true;
		}

		public static bool SaveAccount(UserAccount account)
		{
			var result = userAccounts.ReplaceOne(x=>x.AccountId==account.AccountId, account);
			return result.ModifiedCount > 0;
		}

		public static List<Models.Tag> GetTags(UserAccount account)
		{
			List<Models.Tag> tags = new();
			foreach (var tagStatus in account.TagStatuses)
			{
				if (tagStatus.Enabled)
				{
					var tag = TagManager.GetTag(tagStatus.Name);

					if (tag is null)
					{
						continue;
					}

					tags.Add(tag);
				}
			}

			return tags;
		}

		/// <summary>
		/// Sets the Hashed Password for a given username
		/// </summary>
		/// <param name="account">The user account</param>
		/// <param name="password">The user account password to be set</param>
		public static void SetUserAccountPassword(UserAccount account, string password)
		{
			try
			{
				account.CreateBCryptHash(password);
				userAccounts.ReplaceOne(acc => acc.Id == account.Id, account);
			}
			catch (Exception ex)
			{
				throw new UserAccountManagerException(GetString("SetUserPassword returned an error"), ex);
			}
		}

		/// <summary>
		/// Sets the UUID for a given username
		/// </summary>
		/// <param name="account">The user account</param>
		/// <param name="uuid">The user account uuid to be set</param>
		public static void SetUserAccountUUID(UserAccount account, string uuid)
		{
			try
			{
				account.UUID = uuid;
				userAccounts.ReplaceOne(acc => acc.Id == account.Id, account);
			}
			catch (Exception ex)
			{
				throw new UserAccountManagerException(GetString("SetUserUUID returned an error"), ex);
			}
		}

		/// <summary>
		/// Sets the group for a given username
		/// </summary>
		/// <param name="account">The user account</param>
		/// <param name="group">The user account group to be set</param>
		public static void SetUserGroup(UserAccount account, string group)
		{
			var grp = GroupManager.GetGroupByName(group);
			if (grp is null)
				throw new GroupNotExistsException(group);

			// prepend the group to the user's group list
			account.Groups = new[] { group }.Concat(account.Groups).ToArray();
		}



		public static bool SetDesiredGroupPrefix(UserAccount account, string? groupToDisplay)
		{
			if (groupToDisplay is null)
			{
				account.DesiredGroupNamePrefix = null;
				userAccounts.ReplaceOne(acc => acc.Id == account.Id, account);
				return true;
			}

			var targetGroup = GroupManager.GetGroupByName(groupToDisplay);
			if (targetGroup == null)
			{
				return false; // Exit early if the target group does not exist.
			}

			bool IsMemberOfGroupOrAncestor(Group group)
			{
				// Check if the user is a member of the specified group or any of its ancestors.
				while (group != null)
				{
					if (account.Groups.Contains(group.Name))
					{
						return true; // User is a member of this group or an ancestor.
					}
					group = group.ParentGroupName != null ? GroupManager.GetGroupByName(group.ParentGroupName) : null;
				}
				return false;
			}

			// Check if the account is part of the target group or any of its parent groups.
			if (!IsMemberOfGroupOrAncestor(targetGroup))
			{
				return false; // User is not a member of the target group or its ancestors.
			}

			account.DesiredGroupNamePrefix = targetGroup.Name;
			// Save the changes to MongoDB.
			userAccounts.ReplaceOne(acc => acc.Id == account.Id, account);

			return true;
		}


		public static void FixGroupName(string oldName, string newName)
		{
			// Filter to find documents where the 'Groups' array contains 'oldName'.
			var filter = Builders<UserAccount>.Filter.AnyEq(x => x.Groups, oldName);

			// Update definition to set 'GroupName' to 'newName'.
			var update = Builders<UserAccount>.Update.Set(x => x.Name, newName);

			// Performing the update operation on all matching documents.
			userAccounts.UpdateMany(filter, update);
		}


		/// <summary>Updates the last accessed time for a database user account to the current time.</summary>
		/// <param name="account">The user account object to modify.</param>
		public static void UpdateLogin(UserAccount account)
		{
			try
			{
				account.LastAccessed = DateTime.UtcNow;
				userAccounts.ReplaceOne(acc => acc.Id == account.Id, account);
			}
			catch (Exception ex)
			{
				throw new UserAccountManagerException(GetString("UpdateLogin returned an error"), ex);
			}
		}

		/// <summary>Gets the database AccountId of a given user account object from the database.</summary>
		/// <param name="username">The username of the user account to query for.</param>
		/// <returns>The user account AccountId</returns>
		public static int? GetUserAccountId(string username)
		{
			try
			{
				return (userAccounts.Find<UserAccount>(x => x.Name == username).FirstOrDefault())?.AccountId;
			}
			catch (Exception ex)
			{
				ServerBase.Log.ConsoleError(GetString($"FetchHashedPasswordAndGroup returned an error: {ex}"));
			}

			return null;
		}

		/// <summary>Gets a user account object by name.</summary>
		/// <param name="name">The user's name.</param>
		/// <returns>The user account object returned from the search.</returns>
		public static UserAccount? GetUserAccountByName(string name)
		{
			try
			{
				return userAccounts.Find<UserAccount>(x => x.Name == name).FirstOrDefault();
			}
			catch (UserAccountManagerException)
			{
				return null;
			}
		}

		/// <summary>Gets a user account object by their user account AccountId.</summary>
		/// <param name="id">The user's AccountId.</param>
		/// <returns>The user account object returned from the search.</returns>
		public static UserAccount? GetUserAccountById(int? id)
		{
			if (id == null) return null;

			try
			{
				return GetUserAccount(new UserAccount { AccountId = id.Value });
			}
			catch (UserAccountManagerException)
			{
				return null;
			}
		}

		/// <summary>Gets a user account object by a user account object.</summary>
		/// <param name="account">The user account object to search by.</param>
		/// <returns>The user object that is returned from the search.</returns>
		public static UserAccount? GetUserAccount(UserAccount account)
		{
			if (account.AccountId == 0 && string.IsNullOrWhiteSpace(account.Name))
				throw new UserAccountManagerException(GetString("User account AccountId and Name are both empty"));

			return userAccounts.Find<UserAccount>(x => x.Name == account.Name || x.AccountId == account.AccountId)
				.FirstOrDefault();
		}

		/// <summary>Gets all the user accounts from the database.</summary>
		/// <returns>The user accounts from the database.</returns>
		public static List<UserAccount>? GetUserAccounts()
		{
			try
			{
				List<UserAccount> accounts = userAccounts.Find(Builders<UserAccount>.Filter.Empty).ToList();
				return accounts;
			}
			catch (Exception ex)
			{
				ServerBase.Log.Error(ex.ToString());
			}

			return null;
		}

		/// <summary>
		/// Gets all user accounts from the database with a username that starts with or contains the given username.
		/// </summary>
		/// <param name="username">Rough username search. "n" will match "n", "na", "nam", "name", etc.</param>
		/// <param name="notAtStart">If username is not the first part of the username.</param>
		/// <returns>Matching users or null if exception is thrown.</returns>
		public static List<UserAccount>? GetUserAccountsByName(string username, bool notAtStart = false)
		{
			try
			{
				var filter = notAtStart
					? Builders<UserAccount>.Filter.Regex("Username",
						new MongoDB.Bson.BsonRegularExpression(username, "i"))
					: Builders<UserAccount>.Filter.Regex("Username",
						new MongoDB.Bson.BsonRegularExpression("^" + username, "i"));

				return userAccounts.Find(filter).ToList();
			}
			catch (Exception ex)
			{
				ServerBase.Log.Error(ex.ToString());
				return null;
			}
		}

		/// <summary>UserAccountManagerException - An exception generated by the user account manager.</summary>
		[Serializable]
		public class UserAccountManagerException : Exception
		{
			/// <summary>Creates a new UserAccountManagerException object.</summary>
			/// <param name="message">The message for the object.</param>
			/// <returns>A new UserAccountManagerException object.</returns>
			public UserAccountManagerException(string message)
				: base(message)
			{
			}

			/// <summary>Creates a new UserAccountManager Object with an internal exception.</summary>
			/// <param name="message">The message for the object.</param>
			/// <param name="inner">The inner exception for the object.</param>
			/// <returns>A new UserAccountManagerException with a defined inner exception.</returns>
			public UserAccountManagerException(string message, Exception inner)
				: base(message, inner)
			{
			}
		}

		/// <summary>A UserExistsException object, used when a user account already exists when attempting to create a new one.</summary>
		[Serializable]
		public class UserAccountExistsException : UserAccountManagerException
		{
			/// <summary>Creates a new UserAccountExistsException object.</summary>
			/// <param name="name">The name of the user account that already exists.</param>
			/// <returns>A UserAccountExistsException object with the user's name passed in the message.</returns>
			public UserAccountExistsException(string name)
				: base(GetString($"User account {name} already exists"))
			{
			}
		}

		/// <summary>A UserNotExistException, used when a user does not exist and a query failed as a result of it.</summary>
		[Serializable]
		public class UserAccountNotExistException : UserAccountManagerException
		{
			/// <summary>Creates a new UserAccountNotExistException object, with the user account name in the message.</summary>
			/// <param name="name">The user account name to be pasesd in the message.</param>
			/// <returns>A new UserAccountNotExistException object with a message containing the user account name that does not exist.</returns>
			public UserAccountNotExistException(string name)
				: base(GetString($"User account {name} does not exist"))
			{
			}
		}

		/// <summary>A GroupNotExistsException, used when a group does not exist.</summary>
		[Serializable]
		public class GroupNotExistsException : UserAccountManagerException
		{
			/// <summary>Creates a new GroupNotExistsException object with the group's name in the message.</summary>
			/// <param name="group">The group name.</param>
			/// <returns>A new GroupNotExistsException with the group that does not exist's name in the message.</returns>
			public GroupNotExistsException(string group)
				: base(GetString($"Group {group} does not exist"))
			{
			}
		}

		public static UserAccount AssureAccount(string accountName, string[] permissions)
		{
			var account = GetUserAccountByName(accountName);
			if (account is null)
			{
				account = new UserAccount(accountName, "", "", "default", DateTime.UtcNow, DateTime.UtcNow, "");
				account.UserPermissions = permissions.ToList();
				AddUserAccount(account);
			}

			return account;
		}
	}
}
