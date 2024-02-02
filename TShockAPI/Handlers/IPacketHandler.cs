using System.Threading.Tasks;

namespace TShockAPI.Handlers
{
	/// <summary>
	/// Describes a packet handler that receives a packet from a GetDataHandler
	/// </summary>
	/// <typeparam name="TEventArgs"></typeparam>
	public interface IPacketHandler<TEventArgs> where TEventArgs : GetDataHandledEventArgs
	{
		/// <summary>
		/// Invoked when the packet is received
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		public Task OnReceive(TEventArgs args);
	}
}
