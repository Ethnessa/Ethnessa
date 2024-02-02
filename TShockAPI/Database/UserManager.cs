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
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Entities;
using TShockAPI.Database.Models;

namespace TShockAPI.Database
{
	/// <summary>UserAccountManager - Methods for dealing with database user accounts and other related functionality within TShock.</summary>
	public static class UserAccountManager
	{
		/// <summary>
		/// Adds the given user account to the database
		/// </summary>
		/// <param name="account">The user account to be added</param>
		/// <remarks>You can also use UserAccount.SaveAsync(), however you should check if it already exists first.</remarks>
		public static async Task AddUserAccount(UserAccount account)
		{
			if (!(await GroupManager.GroupExists(account.Group)))
				throw new GroupNotExistsException(account.Group);

			try
			{
				// does account already exist?
				var alreadyExists = await DB.CountAsync<UserAccount>(x => x.Name == account.Name) > 0;
				if (alreadyExists)
				{
					throw new UserAccountExistsException(account.Name);
				}

				account.AccountId = await GetNextUserId();
				await account.SaveAsync();
			}
			catch (Exception ex)
			{
				throw new UserAccountManagerException(GetString($"AddUser returned an error ({ex.Message})"), ex);
			}

			Hooks.AccountHooks.OnAccountCreate(account);
		}

		public static async Task<int> GetNextUserId()
		{
			if (await DB.Queryable<UserAccount>().AnyAsync() is false)
				return 0;
			return (int)await DB.Queryable<UserAccount>().MaxAsync(x => x.AccountId) + 1;
		}

		/// <summary>
		/// Removes all user accounts from the database whose usernames match the given user account
		/// </summary>
		/// <param name="account">The user account</param>
		public static async Task RemoveUserAccount(UserAccount account)
		{
			try
			{
				await DB.DeleteAsync<UserAccount>(x => x.Name == account.Name);
			}
			catch (Exception ex)
			{
				throw new UserAccountManagerException(GetString($"RemoveUser returned an error ({ex.Message})"), ex);
			}

			Hooks.AccountHooks.OnAccountDelete(account);
		}

		/// <summary>
		/// Sets the Hashed Password for a given username
		/// </summary>
		/// <param name="account">The user account</param>
		/// <param name="password">The user account password to be set</param>
		public static async Task SetUserAccountPassword(UserAccount account, string password)
		{
			try
			{
				account.CreateBCryptHash(password);
				await account.SaveAsync();
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
		public static async Task SetUserAccountUUID(UserAccount account, string uuid)
		{
			try
			{
				account.UUID = uuid;
				await account.SaveAsync();
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
		public static async Task SetUserGroup(UserAccount account, string group)
		{
			var grp = await GroupManager.GetGroupByName(group);
			if (grp == null)
				throw new GroupNotExistsException(group);

			account.Group = group;

			try
			{
				// Update player group reference for any logged in player
				foreach (var player in TShock.Players.Where(p =>
					         p != null && p.Account != null && p.Account.Name == account.Name))
				{
					player.Group = grp;
				}
			}
			catch (Exception ex)
			{
				throw new UserAccountManagerException(GetString("SetUserGroup returned an error"), ex);
			}
		}

		/// <summary>Updates the last accessed time for a database user account to the current time.</summary>
		/// <param name="account">The user account object to modify.</param>
		public static async Task UpdateLogin(UserAccount account)
		{
			try
			{
				account.LastAccessed = DateTime.UtcNow;
				await account.SaveAsync();
			}
			catch (Exception ex)
			{
				throw new UserAccountManagerException(GetString("UpdateLogin returned an error"), ex);
			}
		}

		/// <summary>Gets the database AccountId of a given user account object from the database.</summary>
		/// <param name="username">The username of the user account to query for.</param>
		/// <returns>The user account AccountId</returns>
		public static async Task<int?> GetUserAccountId(string username)
		{
			try
			{
				return (await DB.Find<UserAccount>().Match(x => x.Name == username).ExecuteFirstAsync())?.AccountId;
			}
			catch (Exception ex)
			{
				TShock.Log.ConsoleError(GetString($"FetchHashedPasswordAndGroup returned an error: {ex}"));
			}

			return -1;
		}

		/// <summary>Gets a user account object by name.</summary>
		/// <param name="name">The user's name.</param>
		/// <returns>The user account object returned from the search.</returns>
		public static async Task<UserAccount?> GetUserAccountByName(string name)
		{
			try
			{
				return await GetUserAccount(new UserAccount { Name = name });
			}
			catch (UserAccountManagerException)
			{
				return null;
			}
		}

		/// <summary>Gets a user account object by their user account AccountId.</summary>
		/// <param name="id">The user's AccountId.</param>
		/// <returns>The user account object returned from the search.</returns>
		public static async Task<UserAccount?> GetUserAccountById(int id)
		{
			try
			{
				return await GetUserAccount(new UserAccount { AccountId = id });
			}
			catch (UserAccountManagerException)
			{
				return null;
			}
		}

		/// <summary>Gets a user account object by a user account object.</summary>
		/// <param name="account">The user account object to search by.</param>
		/// <returns>The user object that is returned from the search.</returns>
		public static async Task<UserAccount?> GetUserAccount(UserAccount account)
		{
			if (account.AccountId == 0 && string.IsNullOrWhiteSpace(account.Name))
				throw new UserAccountManagerException(GetString("User account AccountId and Name are both empty"));

			return await DB.Find<UserAccount>()
				.Match(x => x.Name == account.Name || x.AccountId == account.AccountId)
				.ExecuteFirstAsync();
		}

		/// <summary>Gets all the user accounts from the database.</summary>
		/// <returns>The user accounts from the database.</returns>
		public static async Task<List<UserAccount>?> GetUserAccounts()
		{
			try
			{
				List<UserAccount> accounts = await DB.Queryable<UserAccount>().ToListAsync();
				return accounts;
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}

			return null;
		}

		/// <summary>
		/// Gets all user accounts from the database with a username that starts with or contains the given username.
		/// </summary>
		/// <param name="username">Rough username search. "n" will match "n", "na", "nam", "name", etc.</param>
		/// <param name="notAtStart">If username is not the first part of the username.</param>
		/// <returns>Matching users or null if exception is thrown.</returns>
		public static async Task<List<UserAccount>?> GetUserAccountsByName(string username, bool notAtStart = false)
		{
			try
			{
				var filter = notAtStart
					? Builders<UserAccount>.Filter.Regex("Username",
						new MongoDB.Bson.BsonRegularExpression(username, "i"))
					: Builders<UserAccount>.Filter.Regex("Username",
						new MongoDB.Bson.BsonRegularExpression("^" + username, "i"));

				return await DB.Find<UserAccount>().Match(filter).ExecuteAsync();
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
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
	}
}
