using System;
using System.Threading.Tasks;

namespace TerrariaApi.Server
{
	public class PluginContainer : IDisposable
	{
		public TerrariaPlugin Plugin { get; protected set; }
		public bool Initialized { get; protected set; }
		public bool Dll { get; set; }

		public PluginContainer(TerrariaPlugin plugin) : this(plugin, true)
		{
		}

		public PluginContainer(TerrariaPlugin plugin, bool dll)
		{
			this.Plugin = plugin;
			this.Initialized = false;
			this.Dll = dll;
		}

		public Task Initialize()
		{
			this.Plugin.Initialize();
			this.Initialized = true;
			return Task.CompletedTask;
		}

		public void DeInitialize()
		{
			this.Initialized = false;
		}

		public void Dispose()
		{
			this.Plugin.Dispose();
		}
	}
}
