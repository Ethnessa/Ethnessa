using System.Threading.Tasks;
using static EthnessaAPI.GetDataHandlers;

namespace EthnessaAPI.Handlers.IllegalPerSe
{
	/// <summary>
	/// Rejects emoji packets with mismatched identifiers
	/// </summary>
	public class EmojiPlayerMismatch : IPacketHandler<GetDataHandlers.EmojiEventArgs>
	{
		/// <summary>
		/// Invoked on emoji send. Rejects packets that are impossible.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		public void OnReceive(object sender, GetDataHandlers.EmojiEventArgs args)
		{
			if (args.PlayerIndex != args.Player.Index)
			{
				ServerBase.Log.ConsoleError(GetString($"IllegalPerSe: Emoji packet rejected for AccountId spoofing. Expected {args.Player.Index}, received {args.PlayerIndex} from {args.Player.Name}."));
				args.Handled = true;
				return;
			}
		}
	}
}
