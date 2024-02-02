using System.IO;
using System.Threading.Tasks;

namespace TShockAPI.Handlers.NetModules
{
	/// <summary>
	/// Describes a handler for a net module
	/// </summary>
	public interface INetModuleHandler
	{
		/// <summary>
		/// Reads the net module's data from the given stream
		/// </summary>
		/// <param name="data"></param>
		void Deserialize(MemoryStream data);

		/// <summary>
		/// Provides handling for the packet and determines if it should be accepted or rejected
		/// </summary>
		/// <param name="player"></param>
		/// <param name="rejectPacket"></param>
		Task<bool> HandlePacket(TSPlayer player);
	}
}
