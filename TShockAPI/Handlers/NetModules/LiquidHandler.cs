using System.IO;
using System.Threading.Tasks;

namespace TShockAPI.Handlers.NetModules
{
	/// <summary>
	/// Handles the NetLiquidModule. Rejects all incoming net liquid requests, as clients should never send them
	/// </summary>
	public class LiquidHandler : INetModuleHandler
	{
		/// <summary>
		/// Does nothing. We should not deserialize this data
		/// </summary>
		/// <param name="data"></param>
		public void Deserialize(MemoryStream data)
		{
			// No need to deserialize
		}

		/// <summary>
		/// Rejects the packet. Clients should not send this to us
		/// </summary>
		/// <param name="player"></param>
		/// <param name="rejectPacket"></param>
		public async Task<bool> HandlePacket(ServerPlayer player)
		{
			return true;
		}
	}
}
