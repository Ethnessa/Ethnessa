using System.Collections.Generic;
using System.IO;
using System.IO.Streams;
using System.Threading.Tasks;
using static EthnessaAPI.GetDataHandlers;

namespace EthnessaAPI.Handlers.NetModules
{
	/// <summary>
	/// Provides handling for the Creative Power net module. Checks permissions on all creative powers
	/// </summary>
	public class CreativePowerHandler : INetModuleHandler
	{
		/// <summary>
		/// The power type being activated
		/// </summary>
		public GetDataHandlers.CreativePowerTypes PowerType { get; set; }

		/// <summary>
		/// Reads the power type from the stream
		/// </summary>
		/// <param name="data"></param>
		public void Deserialize(MemoryStream data)
		{
			PowerType = (GetDataHandlers.CreativePowerTypes)data.ReadInt16();
		}

		/// <summary>
		/// Determines if the player has permission to use the power type
		/// </summary>
		/// <param name="player"></param>
		/// <param name="rejectPacket"></param>
		public bool HandlePacket(ServerPlayer player)
		{
			if (!HasPermission(PowerType, player))
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Determines if a player has permission to use a specific creative power
		/// </summary>
		/// <param name="powerType"></param>
		/// <param name="player"></param>
		/// <returns></returns>
		public static bool HasPermission(GetDataHandlers.CreativePowerTypes powerType, ServerPlayer player)
		{
			if (!PowerToPermissionMap.ContainsKey(powerType))
			{
				ServerBase.Log.ConsoleDebug(GetString("CreativePowerHandler received permission check request for unknown creative power"));
				return false;
			}

			string permission = PowerToPermissionMap[powerType];

			//prevent being told about the spawnrate permission on join until relogic fixes
			if (!player.HasReceivedNpcPermissionError && powerType == GetDataHandlers.CreativePowerTypes.SetSpawnRate)
			{
				player.HasReceivedNpcPermissionError = true;
				return false;
			}

			if (!player.HasPermission(permission))
			{
				player.SendErrorMessage(PermissionToDescriptionMap[permission]);
				return false;
			}

			return true;
		}


		/// <summary>
		/// Maps creative powers to permission nodes
		/// </summary>
		public static Dictionary<GetDataHandlers.CreativePowerTypes, string> PowerToPermissionMap = new Dictionary<GetDataHandlers.CreativePowerTypes, string>
		{
			{ GetDataHandlers.CreativePowerTypes.FreezeTime,              Permissions.journey_timefreeze		},
			{ GetDataHandlers.CreativePowerTypes.SetDawn,                 Permissions.journey_timeset			},
			{ GetDataHandlers.CreativePowerTypes.SetNoon,                 Permissions.journey_timeset			},
			{ GetDataHandlers.CreativePowerTypes.SetDusk,                 Permissions.journey_timeset			},
			{ GetDataHandlers.CreativePowerTypes.SetMidnight,             Permissions.journey_timeset			},
			{ GetDataHandlers.CreativePowerTypes.Godmode,                 Permissions.journey_godmode			},
			{ GetDataHandlers.CreativePowerTypes.WindStrength,            Permissions.journey_windstrength		},
			{ GetDataHandlers.CreativePowerTypes.RainStrength,            Permissions.journey_rainstrength		},
			{ GetDataHandlers.CreativePowerTypes.TimeSpeed,               Permissions.journey_timespeed			},
			{ GetDataHandlers.CreativePowerTypes.RainFreeze,              Permissions.journey_rainfreeze		},
			{ GetDataHandlers.CreativePowerTypes.WindFreeze,              Permissions.journey_windfreeze		},
			{ GetDataHandlers.CreativePowerTypes.IncreasePlacementRange,  Permissions.journey_placementrange	},
			{ GetDataHandlers.CreativePowerTypes.WorldDifficulty,         Permissions.journey_setdifficulty		},
			{ GetDataHandlers.CreativePowerTypes.BiomeSpreadFreeze,       Permissions.journey_biomespreadfreeze },
			{ GetDataHandlers.CreativePowerTypes.SetSpawnRate,            Permissions.journey_setspawnrate		},
		};

		/// <summary>
		/// Maps journey mode permission nodes to descriptions of what the permission allows
		/// </summary>
		public static Dictionary<string, string> PermissionToDescriptionMap = new Dictionary<string, string>
		{
			{ Permissions.journey_timefreeze,			GetString("You do not have permission to freeze the time of the server.")						},
			{ Permissions.journey_timeset,				GetString("You do not have permission to modify the time of the server.")						},
			{ Permissions.journey_godmode,				GetString("You do not have permission to toggle godmode.")										},
			{ Permissions.journey_windstrength,			GetString("You do not have permission to modify the wind strength of the server.")				},
			{ Permissions.journey_rainstrength,			GetString("You do not have permission to modify the rain strength of the server.")				},
			{ Permissions.journey_timespeed,			GetString("You do not have permission to modify the time speed of the server.")					},
			{ Permissions.journey_rainfreeze,			GetString("You do not have permission to freeze the rain strength of the server.")				},
			{ Permissions.journey_windfreeze,			GetString("You do not have permission to freeze the wind strength of the server.")				},
			{ Permissions.journey_placementrange,		GetString("You do not have permission to modify the tile placement range of your character.") 	},
			{ Permissions.journey_setdifficulty,		GetString("You do not have permission to modify the world difficulty of the server.")			},
			{ Permissions.journey_biomespreadfreeze,	GetString("You do not have permission to freeze the biome spread of the server.")				},
			{ Permissions.journey_setspawnrate,			GetString("You do not have permission to modify the NPC spawn rate of the server.")				},
		};
	}
}
