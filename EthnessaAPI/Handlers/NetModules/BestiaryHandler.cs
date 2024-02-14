using System.IO;
using System.Threading.Tasks;

namespace EthnessaAPI.Handlers.NetModules
{
	/// <summary>
	/// Rejects client->server bestiary net modules as the client should never send this to the server
	/// </summary>
	public class BestiaryHandler : INetModuleHandler
	{
		/// <summary>
		/// No deserialization needed. This should never be received by the server
		/// </summary>
		/// <param name="data"></param>
		public void Deserialize(MemoryStream data)
		{
		}

		/// <summary>
		/// This should never be received by the server
		/// </summary>
		/// <param name="player"></param>
		/// <param name="rejectPacket"></param>
		public bool HandlePacket(ServerPlayer player)
		{
			return true;
		}
	}
}
