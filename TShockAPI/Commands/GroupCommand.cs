using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TShockAPI.Database;

namespace TShockAPI.ServerCommands;

public class GroupCommand : Command
{
	public override List<string> Names { get; protected set; } = new() { "group" };
	public override CommandDelegate CommandDelegate { get; set; } = Execute;
	public override List<string> Permissions { get; protected set; } = new(){TShockAPI.Permissions.managegroup};
	public static async Task Execute(CommandArgs args)
	{
		string subCmd = args.Parameters.Count == 0 ? "help" : args.Parameters[0].ToLower();

		switch (subCmd)
		{
			case "add":

				#region Add group

			{
				if (args.Parameters.Count < 2)
				{
					args.Player.SendErrorMessage(GetString(
						"Invalid syntax. Proper syntax: {0}group add <group name> [permissions].", Commands.Specifier));
					return;
				}

				string groupName = args.Parameters[1];
				args.Parameters.RemoveRange(0, 2);
				List<string> permissions = args.Parameters;

				try
				{
					await GroupManager.AddGroup(groupName, null, permissions, TShockAPI.Group.DefaultChatColor);
					args.Player.SendSuccessMessage(GetString($"Group {groupName} was added successfully."));
				}
				catch (GroupExistsException)
				{
					args.Player.SendErrorMessage(GetString("A group with the same name already exists."));
				}
				catch (GroupManagerException ex)
				{
					args.Player.SendErrorMessage(ex.ToString());
				}
			}

				#endregion

				return;
			case "addperm":

				#region Add permissions

			{
				if (args.Parameters.Count < 3)
				{
					args.Player.SendErrorMessage(GetString(
						"Invalid syntax. Proper syntax: {0}group addperm <group name> <permissions...>.", Commands.Specifier));
					return;
				}

				string groupName = args.Parameters[1];
				args.Parameters.RemoveRange(0, 2);
				if (groupName == "*")
				{
					foreach (Group g in await GroupManager.GetGroupsAsync())
					{
						await GroupManager.AddPermissions(g.Name, args.Parameters);
					}

					args.Player.SendSuccessMessage(
						GetString("The permissions have been added to all of the groups in the system."));
					return;
				}

				try
				{
					string response = await GroupManager.AddPermissions(groupName, args.Parameters);
					if (response.Length > 0)
					{
						args.Player.SendSuccessMessage(response);
					}

					return;
				}
				catch (GroupManagerException ex)
				{
					args.Player.SendErrorMessage(ex.ToString());
				}
			}

				#endregion

				return;
			case "help":

				#region Help

			{
				int pageNumber;
				if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
					return;

				var lines = new List<string>
				{
					GetString("add <name> <permissions...> - Adds a new group."),
					GetString("addperm <group> <permissions...> - Adds permissions to a group."),
					GetString("color <group> <rrr,ggg,bbb> - Changes a group's chat color."),
					GetString("rename <group> <new name> - Changes a group's name."),
					GetString("del <group> - Deletes a group."),
					GetString("delperm <group> <permissions...> - Removes permissions from a group."),
					GetString("list [page] - Lists groups."),
					GetString("listperm <group> [page] - Lists a group's permissions."),
					GetString("parent <group> <parent group> - Changes a group's parent group."),
					GetString("prefix <group> <prefix> - Changes a group's prefix."),
					GetString("suffix <group> <suffix> - Changes a group's suffix.")
				};

				PaginationTools.SendPage(args.Player, pageNumber, lines,
					new PaginationTools.Settings
					{
						HeaderFormat = GetString("Group Sub-Commands ({{0}}/{{1}}):"),
						FooterFormat = GetString("Type {0}group help {{0}} for more sub-commands.", Commands.Specifier)
					}
				);
			}

				#endregion

				return;
			case "parent":

				#region Parent

			{
				if (args.Parameters.Count < 2)
				{
					args.Player.SendErrorMessage(GetString(
						"Invalid syntax. Proper syntax: {0}group parent <group name> [new parent group name].",
						Commands.Specifier));
					return;
				}

				string groupName = args.Parameters[1];
				var group = await GroupManager.GetGroupByName(groupName);
				if (group == null)
				{
					args.Player.SendErrorMessage(GetString("No such group \"{0}\".", groupName));
					return;
				}

				if (args.Parameters.Count > 2)
				{
					string newParentGroupName = string.Join(" ", args.Parameters.Skip(2));
					if (!string.IsNullOrWhiteSpace(newParentGroupName) &&
					    !await GroupManager.GroupExists(newParentGroupName))
					{
						args.Player.SendErrorMessage(GetString("No such group \"{0}\".", newParentGroupName));
						return;
					}

					try
					{
						await GroupManager.UpdateGroup(groupName, newParentGroupName, group.Permissions, group.ChatColor,
							group.Suffix, group.Prefix);

						if (!string.IsNullOrWhiteSpace(newParentGroupName))
							args.Player.SendSuccessMessage(GetString("Parent of group \"{0}\" set to \"{1}\".",
								groupName, newParentGroupName));
						else
							args.Player.SendSuccessMessage(GetString("Removed parent of group \"{0}\".", groupName));
					}
					catch (GroupManagerException ex)
					{
						args.Player.SendErrorMessage(ex.Message);
					}
				}
				else
				{
					var parent = await group.GetParentGroup();
					if (parent is not null)
						args.Player.SendSuccessMessage(GetString("Parent of \"{0}\" is \"{1}\".", group.Name,
							parent));
					else
						args.Player.SendSuccessMessage(GetString("Group \"{0}\" has no parent.", group.Name));
				}
			}

				#endregion

				return;
			case "suffix":

				#region Suffix

			{
				if (args.Parameters.Count < 2)
				{
					args.Player.SendErrorMessage(GetString(
						"Invalid syntax. Proper syntax: {0}group suffix <group name> [new suffix].", Commands.Specifier));
					return;
				}

				string groupName = args.Parameters[1];
				var group = await GroupManager.GetGroupByName(groupName);
				if (group == null)
				{
					args.Player.SendErrorMessage(GetString("No such group \"{0}\".", groupName));
					return;
				}

				if (args.Parameters.Count > 2)
				{
					string newSuffix = string.Join(" ", args.Parameters.Skip(2));

					try
					{
						await GroupManager.UpdateGroup(groupName, group.ParentGroupName ?? "", group.Permissions, group.ChatColor,
							newSuffix, group.Prefix);

						if (!string.IsNullOrWhiteSpace(newSuffix))
							args.Player.SendSuccessMessage(GetString("Suffix of group \"{0}\" set to \"{1}\".",
								groupName, newSuffix));
						else
							args.Player.SendSuccessMessage(GetString("Removed suffix of group \"{0}\".", groupName));
					}
					catch (GroupManagerException ex)
					{
						args.Player.SendErrorMessage(ex.Message);
					}
				}
				else
				{
					if (!string.IsNullOrWhiteSpace(group.Suffix))
						args.Player.SendSuccessMessage(GetString("Suffix of \"{0}\" is \"{1}\".", group.Name,
							group.Suffix));
					else
						args.Player.SendSuccessMessage(GetString("Group \"{0}\" has no suffix.", group.Name));
				}
			}

				#endregion

				return;
			case "prefix":

				#region Prefix

			{
				if (args.Parameters.Count < 2)
				{
					args.Player.SendErrorMessage(GetString(
						"Invalid syntax. Proper syntax: {0}group prefix <group name> [new prefix].", Commands.Specifier));
					return;
				}

				string groupName = args.Parameters[1];
				var group = await GroupManager.GetGroupByName(groupName);
				if (group == null)
				{
					args.Player.SendErrorMessage(GetString("No such group \"{0}\".", groupName));
					return;
				}

				if (args.Parameters.Count > 2)
				{
					string newPrefix = string.Join(" ", args.Parameters.Skip(2));

					try
					{
						await GroupManager.UpdateGroup(groupName, group.ParentGroupName, group.Permissions, group.ChatColor,
							group.Suffix, newPrefix);

						if (!string.IsNullOrWhiteSpace(newPrefix))
							args.Player.SendSuccessMessage(GetString("Prefix of group \"{0}\" set to \"{1}\".",
								groupName, newPrefix));
						else
							args.Player.SendSuccessMessage(GetString("Removed prefix of group \"{0}\".", groupName));
					}
					catch (GroupManagerException ex)
					{
						args.Player.SendErrorMessage(ex.Message);
					}
				}
				else
				{
					if (!string.IsNullOrWhiteSpace(group.Prefix))
						args.Player.SendSuccessMessage(GetString("Prefix of \"{0}\" is \"{1}\".", group.Name,
							group.Prefix));
					else
						args.Player.SendSuccessMessage(GetString("Group \"{0}\" has no prefix.", group.Name));
				}
			}

				#endregion

				return;
			case "color":

				#region Color

			{
				if (args.Parameters.Count < 2 || args.Parameters.Count > 3)
				{
					args.Player.SendErrorMessage(GetString(
						"Invalid syntax. Proper syntax: {0}group color <group name> [new color(000,000,000)].",
						Commands.Specifier));
					return;
				}

				string groupName = args.Parameters[1];
				var group = await GroupManager.GetGroupByName(groupName);
				if (group == null)
				{
					args.Player.SendErrorMessage(GetString("No such group \"{0}\".", groupName));
					return;
				}

				if (args.Parameters.Count == 3)
				{
					string newColor = args.Parameters[2];

					String[] parts = newColor.Split(',');
					byte r;
					byte g;
					byte b;
					if (parts.Length == 3 && byte.TryParse(parts[0], out r) && byte.TryParse(parts[1], out g) &&
					    byte.TryParse(parts[2], out b))
					{
						try
						{
							await GroupManager.UpdateGroup(groupName, group.ParentGroupName, group.Permissions, newColor,
								group.Suffix, group.Prefix);

							args.Player.SendSuccessMessage(GetString("Chat color for group \"{0}\" set to \"{1}\".",
								groupName, newColor));
						}
						catch (GroupManagerException ex)
						{
							args.Player.SendErrorMessage(ex.Message);
						}
					}
					else
					{
						args.Player.SendErrorMessage(GetString("Invalid syntax for color, expected \"rrr,ggg,bbb\"."));
					}
				}
				else
				{
					args.Player.SendSuccessMessage(GetString("Chat color for \"{0}\" is \"{1}\".", group.Name,
						group.ChatColor));
				}
			}

				#endregion

				return;
			case "rename":

				#region Rename group

			{
				if (args.Parameters.Count != 3)
				{
					args.Player.SendErrorMessage(
						GetString("Invalid syntax. Proper syntax: {0}group rename <group> <new name>.", Commands.Specifier));
					return;
				}

				string group = args.Parameters[1];
				string newName = args.Parameters[2];
				try
				{
					string response = await GroupManager.RenameGroup(group, newName);
					args.Player.SendSuccessMessage(response);
				}
				catch (GroupManagerException ex)
				{
					args.Player.SendErrorMessage(ex.Message);
				}
			}

				#endregion

				return;
			case "del":

				#region Delete group

			{
				if (args.Parameters.Count != 2)
				{
					args.Player.SendErrorMessage(GetString("Invalid syntax. Proper syntax: {0}group del <group name>.",
						Commands.Specifier));
					return;
				}

				try
				{
					string response = await GroupManager.DeleteGroup(args.Parameters[1], true);
					if (response.Length > 0)
					{
						args.Player.SendSuccessMessage(response);
					}
				}
				catch (GroupManagerException ex)
				{
					args.Player.SendErrorMessage(ex.Message);
				}
			}

				#endregion

				return;
			case "delperm":

				#region Delete permissions

			{
				if (args.Parameters.Count < 3)
				{
					args.Player.SendErrorMessage(GetString(
						"Invalid syntax. Proper syntax: {0}group delperm <group name> <permissions...>.", Commands.Specifier));
					return;
				}

				string groupName = args.Parameters[1];
				args.Parameters.RemoveRange(0, 2);
				if (groupName == "*")
				{
					foreach (Group g in await GroupManager.GetGroupsAsync())
					{
						await GroupManager.DeletePermissions(g.Name, args.Parameters);
					}

					args.Player.SendSuccessMessage(
						GetString("The permissions have been removed from all of the groups in the system."));
					return;
				}

				try
				{
					string response = await GroupManager.DeletePermissions(groupName, args.Parameters);
					if (response.Length > 0)
					{
						args.Player.SendSuccessMessage(response);
					}

					return;
				}
				catch (GroupManagerException ex)
				{
					args.Player.SendErrorMessage(ex.Message);
				}
			}

				#endregion

				return;
			case "list":

				#region List groups

			{
				int pageNumber;
				if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
					return;
				var groupNames = from grp in (await GroupManager.GetGroupsAsync())
					select grp.Name;
				PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(groupNames),
					new PaginationTools.Settings
					{
						HeaderFormat = GetString("Groups ({{0}}/{{1}}):"),
						FooterFormat = GetString("Type {0}group list {{0}} for more.", Commands.Specifier)
					});
			}

				#endregion

				return;
			case "listperm":

				#region List permissions

			{
				if (args.Parameters.Count == 1)
				{
					args.Player.SendErrorMessage(
						GetString("Invalid syntax. Proper syntax: {0}group listperm <group name> [page].", Commands.Specifier));
					return;
				}

				int pageNumber;
				if (!PaginationTools.TryParsePageNumber(args.Parameters, 2, args.Player, out pageNumber))
					return;

				if (!await GroupManager.GroupExists(args.Parameters[1]))
				{
					args.Player.SendErrorMessage(GetString("Invalid group."));
					return;
				}

				var grp = await GroupManager.GetGroupByName(args.Parameters[1]);
				List<string> permissions = await grp.GetPermissions();

				PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(permissions),
					new PaginationTools.Settings
					{
						HeaderFormat = GetString("Permissions for {0} ({{0}}/{{1}}):", grp.Name),
						FooterFormat = GetString("Type {0}group listperm {1} {{0}} for more.", Commands.Specifier, grp.Name),
						NothingToDisplayString = GetString($"There are currently no permissions for {grp.Name}.")
					});
			}

				#endregion

				return;
			default:
				args.Player.SendErrorMessage(GetString(
					"Invalid subcommand! Type {0}group help for more information on valid commands.", Commands.Specifier));
				return;
		}
	}
}
