using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Terraria;
using static EthnessaAPI.GetDataHandlers;

namespace EthnessaAPI.Handlers.NetModules
{
	/// <summary>
	/// Handles packet 82 - Load Net Module packets
	/// </summary>
	public class NetModulePacketHandler : IPacketHandler<GetDataHandlers.ReadNetModuleEventArgs>
	{
		/// <summary>
		/// Maps net module types to handlers for the net module type. Add to or edit this dictionary to customise handling
		/// </summary>
		public static Dictionary<GetDataHandlers.NetModuleType, Type> NetModulesToHandlersMap = new Dictionary<GetDataHandlers.NetModuleType, Type>
		{
			{ GetDataHandlers.NetModuleType.CreativePowers,               typeof(CreativePowerHandler)    },
			{ GetDataHandlers.NetModuleType.CreativeUnlocksPlayerReport,  typeof(CreativeUnlocksHandler)  },
			{ GetDataHandlers.NetModuleType.TeleportPylon,                typeof(PylonHandler)            },
			{ GetDataHandlers.NetModuleType.Liquid,                       typeof(LiquidHandler)           },
			{ GetDataHandlers.NetModuleType.Bestiary,                     typeof(BestiaryHandler)         },
			{ GetDataHandlers.NetModuleType.Ambience,                     typeof(AmbienceHandler)         }
		};

		/// <summary>
		/// Invoked when a load net module packet is received. This method picks a <see cref="INetModuleHandler"/> based on the
		/// net module type being loaded, then forwards the data to the chosen handler to process
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		public void OnReceive(object sender, GetDataHandlers.ReadNetModuleEventArgs args)
		{
			INetModuleHandler handler;

			if (NetModulesToHandlersMap.ContainsKey(args.ModuleType))
			{
				handler = (INetModuleHandler)Activator.CreateInstance(NetModulesToHandlersMap[args.ModuleType]);
			}
			else
			{
				// We don't have handlers for NetModuleType.Ping and NetModuleType.Particles.
				// These net modules are fairly innocuous and can be processed normally by the game
				args.Handled = false;
				return;
			}

			handler.Deserialize(args.Data);
			var rejectPacket = handler.HandlePacket(args.Player);

			args.Handled = rejectPacket;
		}
	}
}
