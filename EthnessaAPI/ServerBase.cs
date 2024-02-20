using MaxMind;
using Microsoft.Xna.Framework;
using MongoDB.Driver;
using MonoMod.Cil;
using Newtonsoft.Json;
using Rests;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using EthnessaAPI.CLI;
using EthnessaAPI.Configuration;
using EthnessaAPI.Database;
using EthnessaAPI.Hooks;
using EthnessaAPI.Localization;
using EthnessaAPI.Modules;
using EthnessaAPI.Sockets;
using HttpServer;
using Terraria;
using Terraria.Achievements;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Initializers;
using Terraria.Localization;
using Terraria.UI.Chat;
using Terraria.Utilities;
using TerrariaApi.Server;
using ServerApi = TerrariaApi.Server.ServerApi;

namespace EthnessaAPI
{
	/// <summary>
	/// This is the TShock main class. TShock is a plugin on the TerrariaServerAPI, so it extends the base TerrariaPlugin.
	/// TShock also complies with the API versioning system, and defines its required API version here.
	/// </summary>
	[ApiVersion(2, 1)]
	public class ServerBase : TerrariaPlugin
	{
		/// <summary>VersionNum - The version number the TerrariaAPI will return back to the API. We just use the Assembly info.</summary>
		public static readonly Version VersionNum = Assembly.GetExecutingAssembly().GetName().Version;

		/// <summary>SavePath - This is the path TShock saves its data in. This path is relative to the root folder (not in ServerPlugins).</summary>
		public static string SavePath = "data";

		/// <summary>LogFormatDefault - This is the default log file naming format. Actually, this is the only log format, because it never gets set again.</summary>
		private const string LogFormatDefault = "yyyy-MM-dd_HH-mm-ss";

		//TODO: Set the log path in the config file.
		/// <summary>LogFormat - This is the log format, which is never set again.</summary>
		private static string LogFormat = LogFormatDefault;

		/// <summary>LogPathDefault - The default log path.</summary>
		private const string LogPathDefault = "data/logs";

		/// <summary>This is the log path, which is initially set to the default log path, and then to the config file log path later.</summary>
		private static string LogPath = LogPathDefault;

		/// <summary>LogClear - Determines whether or not the log file should be cleared on initialization.</summary>
		private static bool LogClear;

		/// <summary>Will be set to true once Utils.StopServer() is called.</summary>
		public static bool ShuttingDown;

		/// <summary>Players - Contains all ServerPlayer objects for accessing TSPlayers currently on the server</summary>
		public static ServerPlayer[] Players = new ServerPlayer[Main.maxPlayers];

		/// <summary>Backups - Static reference to the backup manager for accessing the backup system.</summary>
		public static BackupManager Backups;

		/// <summary>Config - Static reference to the config system, for accessing values set in users' config files.</summary>
		public static TShockConfig Config { get; set; }

		/// <summary>ServerSideCharacterConfig - Static reference to the server side character config, for accessing values set by users to modify SSC.</summary>
		public static ServerSideConfig ServerSideCharacterConfig;

		/// <summary>OverridePort - Determines if TShock should override the server port.</summary>
		public static bool OverridePort;

		/// <summary>Geo - Static reference to the GeoIP system which determines the location of an IP address.</summary>
		public static GeoIPCountry? Geo;

		/// <summary>RestApi - Static reference to the Rest API authentication manager.</summary>
		public static SecureRest RestApi;

		/// <summary>RestManager - Static reference to the Rest API manager.</summary>
		public static RestManager RestManager;

		/// <summary>Utils - Static reference to the utilities class, which contains a variety of utility functions.</summary>
		public static Utils Utils = Utils.Instance;

		/// <summary>UpdateManager - Static reference to the update checker, which checks for updates and notifies server admins of updates.</summary>
		public static UpdateManager UpdateManager;

		/// <summary>Log - Static reference to the log system, which outputs to either SQL or a text file, depending on user config.</summary>
		public static ILog Log;

		/// <summary>instance - Static reference to the TerrariaPlugin instance.</summary>
		public static TerrariaPlugin instance;

		/// <summary>
		/// Static reference to a <see cref="CommandLineParser"/> used for simple command-line parsing
		/// </summary>
		public static CommandLineParser CliParser { get; } = new CommandLineParser();

		/// <summary>
		/// Used for implementing REST Tokens prior to the REST system starting up.
		/// </summary>
		public static Dictionary<string, SecureRest.TokenData> RESTStartupTokens =
			new Dictionary<string, SecureRest.TokenData>();

		/// <summary>The TShock anti-cheat/anti-exploit system.</summary>
		internal Bouncer Bouncer;

		/// <summary>The TShock item ban system.</summary>
		public static ItemBans ItemBans;

		/// <summary>
		/// TShock's Region subsystem.
		/// </summary>
		internal RegionHandler RegionSystem;

		/// <summary>
		/// Called after TShock is initialized. Useful for plugins that needs hooks before tshock but also depend on tshock being loaded.
		/// </summary>
		public static event Action Initialized;

		/// <summary>
		/// Global database connection, used for global (multi-server) data.
		/// </summary>
		public static IMongoDatabase GlobalDatabase { get; private set; }

		/// <summary>
		/// Local database connection, used for local (single-server) data.
		/// </summary>
		public static IMongoDatabase LocalDatabase { get; private set; }

		public static ModuleManager ModuleManager { get; } = new ModuleManager();

		/// <summary>Version - The version required by the TerrariaAPI to be passed back for checking &amp; loading the plugin.</summary>
		/// <value>value - The version number specified in the Assembly, based on the VersionNum variable set in this class.</value>
		public override Version Version => VersionNum;

		/// <summary>Name - The plugin name.</summary>
		/// <value>value - "TShock"</value>
		public override string Name => "EthnessaAPI";

		/// <summary>Author - The author of the plugin.</summary>
		public override string Author => "Ethnessa Developers";

		/// <summary>Description - The plugin description.</summary>
		public override string Description => "A modified version of TShock to fit the needs of TSD.";

		/// <summary>TShock - The constructor for the TShock plugin.</summary>
		/// <param name="game">game - The Terraria main game.</param>
		public ServerBase(Main game)
			: base(game)
		{
			Config = new TShockConfig();
			ServerSideCharacterConfig = new ServerSideConfig();
			ServerSideCharacterConfig.Settings.StartingInventory.Add(new NetItem(-15, 1, 0));
			ServerSideCharacterConfig.Settings.StartingInventory.Add(new NetItem(-13, 1, 0));
			ServerSideCharacterConfig.Settings.StartingInventory.Add(new NetItem(-16, 1, 0));
			Order = 0;
			instance = this;
		}


		static Dictionary<string, IntPtr> _nativeCache = new Dictionary<string, IntPtr>();

		static IntPtr ResolveNativeDep(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
		{
			if (_nativeCache.TryGetValue(libraryName, out IntPtr cached))
				return cached;

			IEnumerable<string> matches = Enumerable.Empty<string>();

			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				var osx = Path.Combine(Environment.CurrentDirectory, "runtimes", "osx-x64");
				if (Directory.Exists(osx))
					matches = Directory.GetFiles(osx, "*" + libraryName + "*", SearchOption.AllDirectories);
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				var lib64 = Path.Combine(Environment.CurrentDirectory, "runtimes", "linux-x64");
				if (Directory.Exists(lib64))
					matches = Directory.GetFiles(lib64, "*" + libraryName + "*", SearchOption.AllDirectories);
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				var x64 = Path.Combine(Environment.CurrentDirectory, "runtimes", "win-x64");
				if (Directory.Exists(x64))
					matches = Directory.GetFiles(x64, "*" + libraryName + "*", SearchOption.AllDirectories);
			}

			if (matches.Count() == 0)
			{
				matches = Directory.GetFiles(Environment.CurrentDirectory, "*" + libraryName + "*");
			}

			Debug.WriteLine($"Looking for `{libraryName}` with {matches.Count()} match(es)");

			var handle = IntPtr.Zero;

			if (matches.Count() == 1)
			{
				var match = matches.Single();
				handle = NativeLibrary.Load(match);
			}

			// cache either way. if zero, no point calling IO if we've checked this assembly before.
			_nativeCache.Add(libraryName, handle);

			return handle;
		}

		public void CrashDueToError(Exception? ex = null)
		{
			void SafeError(string message)
			{
				// Attempt to log the message if the Log is not null, else write to the console.
				if (Log is not null) Log.ConsoleError(message);
				else Console.WriteLine(message);
			}

			;

			// Display a general error message about the crash.
			SafeError(
				"TShock encountered a problem from which it cannot recover. The following message may help diagnose the problem.");
			SafeError("Until the problem is resolved, TShock will not be able to start (and will crash on startup).");

			// If an exception was provided, log its details.
			if (ex is not null)
			{
				SafeError(ex.ToString());
			}

			// Before exiting, ensure there's a pause so the user can read the error message if running outside a debugger.
			Console.WriteLine("Press enter to exit...");
			Console.ReadLine(); // Wait for user input before closing.

			Environment.Exit(1); // Exit the application.
		}


		/// <summary>Initialize - Called by the TerrariaServerAPI during initialization.</summary>
		public override void Initialize()
		{
			string logFilename;

			OTAPI.Hooks.Netplay.CreateTcpListener += (sender, args) => args.Result = new LinuxTcpSocket();
			OTAPI.Hooks.NetMessage.PlayerAnnounce += (sender, args) =>
				//TShock handles this
				args.Result = OTAPI.Hooks.NetMessage.PlayerAnnounceResult.None;

			Main.SettingsUnlock_WorldEvil = true;

			TerrariaApi.Reporting.CrashReporter.HeapshotRequesting += CrashReporter_HeapshotRequesting;

			Console.CancelKeyPress += new ConsoleCancelEventHandler(ConsoleCancelHandler);

			try
			{
				CliParser.Reset();
				HandleCommandLine(Environment.GetCommandLineArgs());

				if (!Directory.Exists(SavePath))
					Directory.CreateDirectory(SavePath);

				TShockConfig.OnConfigRead += OnConfigRead;
				FileTools.SetupConfig();

				Main.ServerSideCharacter = ServerSideCharacterConfig.Settings.Enabled;

				// If the server IP is null, set it to any (0.0.0.0)
				Netplay.ServerIP ??= IPAddress.Any;

				var now = DateTime.Now;

				// TODO: Figure out what the hell this comment means and if we need this
				// Log path was not already set by the command line parameter?
				if (LogPath == LogPathDefault)
				{
					LogPath = Config.Settings.LogPath;
				}

				Utils.EnsureAliases();

				try
				{
					logFilename = Path.Combine(LogPath, now.ToString(LogFormat) + ".log");
					if (!Directory.Exists(LogPath))
						Directory.CreateDirectory(LogPath);
				}
				catch (Exception ex)
				{
					ServerApi.LogWriter.PluginWriteLine(this,
						GetString(
							"Could not apply the given log path / log format, defaults will be used. Exception details:\n{0}",
							ex), TraceLevel.Error);

					// Problem with the log path or format use the default
					logFilename = Path.Combine(LogPathDefault, now.ToString(LogFormatDefault) + ".log");
				}

				AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			}
			catch (Exception ex)
			{
				// NOTE: What the hell is crashlog.txt? - Average
				// Will be handled by the server api and written to its crashlog.txt.
				throw new Exception("Fatal TShock initialization exception. See inner exception for details.", ex);
			}

			try
			{
				if (string.IsNullOrWhiteSpace(Config.Settings.MongoConnectionString))
				{
					ServerApi.LogWriter.PluginWriteLine(this,
						GetString("No MongoDB connection string was provided. TShock will not be able to start."),
						TraceLevel.Error);
					throw new Exception("No MongoDB connection string was found!");
				}

				MongoClient mongoClient;

				try
				{
					var mongoConnectionSettings =
						MongoClientSettings.FromConnectionString(Config.Settings.MongoConnectionString);
					mongoClient = new MongoClient(mongoConnectionSettings);
				}
				catch (MongoConfigurationException ex)
				{
					ServerApi.LogWriter.PluginWriteLine(this,
						GetString(
							"The provided MongoDB connection string is invalid. TShock will not be able to start."),
						TraceLevel.Error);
					throw new Exception("Invalid MongoDB connection string!", ex);
				}
				catch (TimeoutException ex)
				{
					ServerApi.LogWriter.PluginWriteLine(this,
						GetString(
							"Could not connect to the MongoDB server. TShock will not be able to start."),
						TraceLevel.Error);
					throw new Exception("Could not connect to the MongoDB server. Is it running?", ex);
				}

				// attempt to make connection to global database
				GlobalDatabase = mongoClient.GetDatabase(Config.Settings.DefaultGlobalDatabase);
				ServerApi.LogWriter.PluginWriteLine(this, $"Connected to global database: '{Config.Settings.DefaultGlobalDatabase}'", TraceLevel.Info);

				// attempt to make connection to local database
				LocalDatabase = mongoClient.GetDatabase(Config.Settings.LocalDatabase);
				ServerApi.LogWriter.PluginWriteLine(this, $"Connected to local database: '{Config.Settings.LocalDatabase}'", TraceLevel.Info);

				// TODO: Allow MongoDB logging to be enabled/disabled, like TShock's prev SQL logging

				Log = new TextLog(logFilename, LogClear);

				if (File.Exists(Path.Combine(SavePath, "tshock.pid")))
				{
					if (ServerBase.Config.Settings.DisplayClosedImproperlyWarning) // bcuz who gives a fuck, not me
					{
						Log.ConsoleInfo(GetString(
							"TShock was improperly shut down. Please use the exit command in the future to prevent this."));
					}

					File.Delete(Path.Combine(SavePath, "tshock.pid"));
				}

				File.WriteAllText(Path.Combine(SavePath, "tshock.pid"),
					Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture));

				CliParser.Reset();
				HandleCommandLinePostConfigLoad(Environment.GetCommandLineArgs());

				Backups = new BackupManager(Path.Combine(SavePath, "backups"))
				{
					KeepFor = Config.Settings.BackupKeepFor,
					Interval = Config.Settings.BackupInterval
				};

				// create 'guest' and 'default' user groups
				GroupManager.EnsureDefaultGroups();

				RestApi = new SecureRest(Netplay.ServerIP, Config.Settings.RestApiPort);
				RestManager = new RestManager(RestApi);
				RestManager.RegisterRestfulCommands();
				Bouncer = new Bouncer();
				RegionSystem = new RegionHandler();
				ItemBans = new ItemBans(this);

				var geoIpData = "GeoIP.dat";
				if (Config.Settings.EnableGeoIP && File.Exists(geoIpData))
					Geo = new GeoIPCountry(geoIpData);

				// NOTE FROM AVERAGE: THIS MAY BE WORTH EXPLORING
				// check if a custom tile provider is to be used
				switch (Config.Settings.WorldTileProvider?.ToLower())
				{
					case "heaptile":
						Log.ConsoleInfo(GetString($"Using {nameof(HeapTile)} for tile implementation"),
							TraceLevel.Info);
						Main.tile = new TileProvider();
						break;
					case "constileation":
						Log.ConsoleInfo(GetString($"Using {nameof(ConstileationProvider)} for tile implementation"),
							TraceLevel.Info);
						Main.tile = new ConstileationProvider();
						break;
				}

				Log.ConsoleInfo(GetString("EthnessaAPI now running!"));

				// TODO: look into better event handling :shrug:
				ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInit);
				ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
				ServerApi.Hooks.GameHardmodeTileUpdate.Register(this, OnHardUpdate);
				ServerApi.Hooks.GameStatueSpawn.Register(this, OnStatueSpawn);
				ServerApi.Hooks.ServerConnect.Register(this, OnConnect);
				ServerApi.Hooks.ServerJoin.Register(this, OnJoin);
				ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
				ServerApi.Hooks.ServerChat.Register(this, OnChat);
				ServerApi.Hooks.ServerCommand.Register(this, ServerHooks_OnCommand);
				ServerApi.Hooks.NetGetData.Register(this, OnGetData);
				ServerApi.Hooks.NetSendData.Register(this, NetHooks_SendData);
				ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreetPlayer);
				ServerApi.Hooks.NpcStrike.Register(this, NpcHooks_OnStrikeNpc);
				ServerApi.Hooks.ProjectileSetDefaults.Register(this, OnProjectileSetDefaults);
				ServerApi.Hooks.WorldStartHardMode.Register(this, OnStartHardMode);
				ServerApi.Hooks.WorldSave.Register(this, SaveManager.Instance.OnSaveWorld);
				ServerApi.Hooks.WorldChristmasCheck.Register(this, OnXmasCheck);
				ServerApi.Hooks.WorldHalloweenCheck.Register(this, OnHalloweenCheck);
				ServerApi.Hooks.NetNameCollision.Register(this, NetHooks_NameCollision);
				ServerApi.Hooks.ItemForceIntoChest.Register(this, OnItemForceIntoChest);
				ServerApi.Hooks.WorldGrassSpread.Register(this, OnWorldGrassSpread);
				Hooks.PlayerHooks.PlayerPreLogin += OnPlayerPreLogin;
				Hooks.PlayerHooks.PlayerPostLogin += OnPlayerLogin;
				Hooks.AccountHooks.AccountDelete += OnAccountDelete;
				Hooks.AccountHooks.AccountCreate += OnAccountCreate;

				GetDataHandlers.InitGetDataHandler();
				Commands.InitializeCommands();

				EnglishLanguage.Initialize();

				// The AchievementTagHandler expects Main.Achievements to be non-null, which is not normally the case on dedicated servers.
				// When trying to parse an achievement chat tag, it will instead throw.
				// The tag is parsed when calling ChatManager.ParseMessage, which is used in TShock when writing chat messages to the
				// console. Our OnChat handler uses Utils.Broadcast, which will send the message to all connected clients, write the message
				// to the console and the log. Due to the order of execution, the message ends up being sent to all connected clients, but
				// throws whilst trying to write to the console, and never gets written to the log.
				// To solve the issue, we make achievements available on the server, allowing the tag handler to work as expected, and
				// even allowing the localization of achievement names to appear in the console.

				if (Game != null)
				{
					// Initialize the AchievementManager, which is normally only done on clients.
					Game._achievements = new AchievementManager();

					IL.Terraria.Initializers.AchievementInitializer.Load += OnAchievementInitializerLoad;

					// Actually call AchievementInitializer.Load, which is also normally only done on clients.
					AchievementInitializer.Load();
				}
				else
				{
					// If we don't have a Game instance, then we'll just remove the achievement tag handler entirely. This will cause the
					// raw tag to just be used instead (and not be localized), but still avoid all the issues outlined above.
					ChatManager._handlers.Remove("a", out _);
					ChatManager._handlers.Remove("achievement", out _);
				}

				ModuleManager.Initialise(new object[] { this });

				if (Config.Settings.RestApiEnabled)
					RestApi.Start();

				Initialized?.Invoke();

				Log.ConsoleDebug(GetString("Fully initialized!"));
			}
			catch (Exception ex)
			{
				CrashDueToError(ex);
			}
		}

		private static void OnAchievementInitializerLoad(ILContext il)
		{
			// Modify AchievementInitializer.Load to remove the Main.netMode == 2 check (occupies the first 4 IL instructions)
			for (var i = 0; i < 4; i++)
				il.Body.Instructions.RemoveAt(0);
		}

		private static void CrashReporter_HeapshotRequesting(object sender, EventArgs e)
		{
			foreach (ServerPlayer player in ServerBase.Players)
			{
				player.UserAccountId = null;
			}
		}

		/// <summary>Dispose - Called when disposing.</summary>
		/// <param name="disposing">disposing - If set, disposes of all hooks and other systems.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				// NOTE: order is important here
				if (Geo != null)
				{
					Geo.Dispose();
				}

				SaveManager.Instance.Dispose();

				IL.Terraria.Initializers.AchievementInitializer.Load -= OnAchievementInitializerLoad;

				ModuleManager.Dispose();


				ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInit);
				ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
				ServerApi.Hooks.GameHardmodeTileUpdate.Deregister(this, OnHardUpdate);
				ServerApi.Hooks.GameStatueSpawn.Deregister(this, OnStatueSpawn);
				ServerApi.Hooks.ServerConnect.Deregister(this, OnConnect);
				ServerApi.Hooks.ServerJoin.Deregister(this, OnJoin);
				ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
				ServerApi.Hooks.ServerChat.Deregister(this, OnChat);
				ServerApi.Hooks.ServerCommand.Deregister(this, ServerHooks_OnCommand);
				ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
				ServerApi.Hooks.NetSendData.Deregister(this, NetHooks_SendData);
				ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreetPlayer);
				ServerApi.Hooks.NpcStrike.Deregister(this, NpcHooks_OnStrikeNpc);
				ServerApi.Hooks.ProjectileSetDefaults.Deregister(this, OnProjectileSetDefaults);
				ServerApi.Hooks.WorldStartHardMode.Deregister(this, OnStartHardMode);
				ServerApi.Hooks.WorldSave.Deregister(this, SaveManager.Instance.OnSaveWorld);
				ServerApi.Hooks.WorldChristmasCheck.Deregister(this, OnXmasCheck);
				ServerApi.Hooks.WorldHalloweenCheck.Deregister(this, OnHalloweenCheck);
				ServerApi.Hooks.NetNameCollision.Deregister(this, NetHooks_NameCollision);
				ServerApi.Hooks.ItemForceIntoChest.Deregister(this, OnItemForceIntoChest);
				ServerApi.Hooks.WorldGrassSpread.Deregister(this, OnWorldGrassSpread);
				PlayerHooks.PlayerPostLogin -= OnPlayerLogin;

				if (File.Exists(Path.Combine(SavePath, "tshock.pid")))
				{
					File.Delete(Path.Combine(SavePath, "tshock.pid"));
				}

				RestApi.Dispose();
				Log.Dispose();

				RegionSystem.Dispose();
			}

			base.Dispose(disposing);
		}

		/// <summary>OnPlayerLogin - Fires the PlayerLogin hook to listening plugins.</summary>
		/// <param name="args">args - The PlayerPostLoginEventArgs object.</param>
		private void OnPlayerLogin(PlayerPostLoginEventArgs args)
		{
			var currentIP = args.Player.IP; // Current player's IP address for readability
			var account = args.Player.Account; // Reference to the player's account for readability

			// Initialize an empty list for Known IPs
			var knownIps = string.IsNullOrWhiteSpace(account.KnownIps)
				? new List<string>()
				: JsonConvert.DeserializeObject<List<string>>(account.KnownIps);

			// Add current IP if it's not the last known IP or if the list is empty
			if (knownIps.LastOrDefault() != currentIP)
			{
				// Ensure the list does not exceed 100 entries
				if (knownIps.Count >= 100)
				{
					knownIps.RemoveAt(0); // Remove the oldest IP
				}

				knownIps.Add(currentIP); // Add the current IP to the list
			}

			// Update the account with the new or modified list of known IPs
			account.KnownIps = JsonConvert.SerializeObject(knownIps, Formatting.Indented);
			UserAccountManager.UpdateLogin(account);

			// Check if the player is banned
			BanManager.IsPlayerBanned(args.Player);
		}


		/// <summary>OnAccountDelete - Internal hook fired on account delete.</summary>
		/// <param name="args">args - The AccountDeleteEventArgs object.</param>
		private void OnAccountDelete(Hooks.AccountDeleteEventArgs args)
		{
			CharacterManager.RemovePlayer(args.Account.AccountId);
		}

		/// <summary>OnAccountCreate - Internal hook fired on account creation.</summary>
		/// <param name="args">args - The AccountCreateEventArgs object.</param>
		private void OnAccountCreate(Hooks.AccountCreateEventArgs args)
		{
			CharacterManager.SeedInitialData(UserAccountManager.GetUserAccount(args.Account));
		}

		/// <summary>OnPlayerPreLogin - Internal hook fired when on player pre login.</summary>
		/// <param name="args">args - The PlayerPreLoginEventArgs object.</param>
		private void OnPlayerPreLogin(Hooks.PlayerPreLoginEventArgs args)
		{
			if (args.Player.IsLoggedIn)
				args.Player.SaveServerCharacter();
		}

		/// <summary>NetHooks_NameCollision - Internal hook fired when a name collision happens.</summary>
		/// <param name="args">args - The NameCollisionEventArgs object.</param>
		private void NetHooks_NameCollision(NameCollisionEventArgs args)
		{
			if (args.Handled)
			{
				return;
			}

			string ip = Utils.GetRealIP(Netplay.Clients[args.Who].Socket.GetRemoteAddress().ToString());

			var player = Players.First(p => p != null && p.Name == args.Name && p.Index != args.Who);
			if (player != null)
			{
				if (player.IP == ip)
				{
					player.Kick(GetString("You logged in from the same IP."), true, true, null, true);
					args.Handled = true;
					return;
				}

				if (player.IsLoggedIn)
				{
					var ips = JsonConvert.DeserializeObject<List<string>>(player.Account.KnownIps);
					if (ips.Contains(ip))
					{
						player.Kick(GetString("You logged in from another location."), true, true, null, true);
						args.Handled = true;
					}
				}
			}

			return;
		}

		/// <summary>OnItemForceIntoChest - Internal hook fired when a player quick stacks items into a chest.</summary>
		/// <param name="args">The <see cref="ForceItemIntoChestEventArgs"/> object.</param>
		private void OnItemForceIntoChest(ForceItemIntoChestEventArgs args)
		{
			if (args.Handled)
			{
				return;
			}

			if (args.Player == null)
			{
				args.Handled = true;
				return;
			}

			ServerPlayer tsplr = Players[args.Player.whoAmI];
			if (tsplr == null)
			{
				args.Handled = true;
				return;
			}

			if (args.Chest != null)
			{
				// After checking for protected regions, no further range checking is necessarily because the client packet only specifies the
				// inventory slot to quick stack. The vanilla Terraria server itself determines what chests are close enough to the player.
				if (Config.Settings.RegionProtectChests &&
				    !RegionManager.CanBuild((int)args.WorldPosition.X, (int)args.WorldPosition.Y, tsplr))
				{
					args.Handled = true;
					return;
				}
			}
		}

		/// <summary>OnXmasCheck - Internal hook fired when the XMasCheck happens.</summary>
		/// <param name="args">args - The ChristmasCheckEventArgs object.</param>
		private void OnXmasCheck(ChristmasCheckEventArgs args)
		{
			if (args.Handled)
				return;

			if (Config.Settings.ForceXmas)
			{
				args.Xmas = true;
				args.Handled = true;
			}

			return;
		}

		/// <summary>OnHalloweenCheck - Internal hook fired when the HalloweenCheck happens.</summary>
		/// <param name="args">args - The HalloweenCheckEventArgs object.</param>
		private void OnHalloweenCheck(HalloweenCheckEventArgs args)
		{
			if (args.Handled)
				return;

			if (Config.Settings.ForceHalloween)
			{
				args.Halloween = true;
				args.Handled = true;
			}

			return;
		}

		/// <summary>
		/// Handles exceptions that we didn't catch earlier in the code, or in Terraria.
		/// </summary>
		/// <param name="sender">sender - The object that sent the exception.</param>
		/// <param name="e">e - The UnhandledExceptionEventArgs object.</param>
		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Log.Error(e.ExceptionObject.ToString());

			if (e.ExceptionObject.ToString().Contains("Terraria.Netplay.ListenForClients") ||
			    e.ExceptionObject.ToString().Contains("Terraria.Netplay.ServerLoop"))
			{
				var sb = new List<string>();
				for (int i = 0; i < Netplay.Clients.Length; i++)
				{
					if (Netplay.Clients[i] == null)
					{
						sb.Add("Client[" + i + "]");
					}
					else if (Netplay.Clients[i].Socket == null)
					{
						sb.Add("Tcp[" + i + "]");
					}
				}

				Log.Error(string.Join(", ", sb));
			}

			if (e.IsTerminating)
			{
				if (Main.worldPathName != null && Config.Settings.SaveWorldOnCrash)
				{
					Main.ActiveWorldFileData._path += ".crash";
					SaveManager.Instance.SaveWorld();
				}
			}
		}

		private bool tryingToShutdown = false;

		/// <summary> ConsoleCancelHandler - Handles when Ctrl + C is sent to the server for a safe shutdown. </summary>
		/// <param name="sender">The sender</param>
		/// <param name="args">The ConsoleCancelEventArgs associated with the event.</param>
		private void ConsoleCancelHandler(object sender, ConsoleCancelEventArgs args)
		{
			if (tryingToShutdown)
			{
				System.Environment.Exit(1);
				return;
			}

			// Cancel the default behavior
			args.Cancel = true;

			tryingToShutdown = true;

			Log.ConsoleInfo(GetString("Shutting down safely. To force shutdown, send SIGINT (CTRL + C) again."));

			// Perform a safe shutdown
			ServerBase.Utils.StopServer(true, GetString("Server console interrupted!"));
		}

		/// <summary>HandleCommandLine - Handles the command line parameters passed to the server.</summary>
		/// <param name="parms">parms - The array of arguments passed in through the command line.</param>
		private void HandleCommandLine(string[] parms)
		{
			string path = null;

			//Generic method for doing a path sanity check
			Action<string> pathChecker = (p) =>
			{
				if (!string.IsNullOrWhiteSpace(p) && p.IndexOfAny(Path.GetInvalidPathChars()) == -1)
				{
					path = p;
				}
			};

			//Prepare the parser with all the flags available
			CliParser
				.AddFlag("-configpath", pathChecker)
				//The .After Action is run after the pathChecker Action
				.After(() =>
				{
					SavePath = path ?? "tshock";
					if (path != null)
					{
						ServerApi.LogWriter.PluginWriteLine(this, GetString("Config path has been set to {0}", path),
							TraceLevel.Info);
					}
				})
				.AddFlag("-worldselectpath", pathChecker)
				.After(() =>
				{
					if (path != null)
					{
						Main.WorldPath = path;
						ServerApi.LogWriter.PluginWriteLine(this, GetString("World path has been set to {0}", path),
							TraceLevel.Info);
					}
				})
				.AddFlag("-logpath", pathChecker)
				.After(() =>
				{
					if (path != null)
					{
						LogPath = path;
						ServerApi.LogWriter.PluginWriteLine(this, GetString("Log path has been set to {0}", path),
							TraceLevel.Info);
					}
				})
				.AddFlag("-logformat", (format) =>
				{
					if (!string.IsNullOrWhiteSpace(format))
					{
						LogFormat = format;
					}
				})
				.AddFlag("-config", (cfg) =>
				{
					if (!string.IsNullOrWhiteSpace(cfg))
					{
						ServerApi.LogWriter.PluginWriteLine(this, GetString("Loading dedicated config file: {0}", cfg),
							TraceLevel.Verbose);
						Main.instance.LoadDedConfig(cfg);
					}
				})
				.AddFlag("-port", (p) =>
				{
					int port;
					if (int.TryParse(p, out port))
					{
						Netplay.ListenPort = port;
						ServerApi.LogWriter.PluginWriteLine(this, GetString("Listening on port {0}.", port),
							TraceLevel.Verbose);
					}
				})
				.AddFlag("-worldname", (world) =>
				{
					if (!string.IsNullOrWhiteSpace(world))
					{
						Main.instance.SetWorldName(world);
						ServerApi.LogWriter.PluginWriteLine(this,
							GetString("World name will be overridden by: {0}", world), TraceLevel.Verbose);
					}
				})
				.AddFlag("-ip", (ip) =>
				{
					IPAddress addr;
					if (IPAddress.TryParse(ip, out addr))
					{
						Netplay.ServerIP = addr;
						ServerApi.LogWriter.PluginWriteLine(this, GetString("Listening on IP {0}.", addr),
							TraceLevel.Verbose);
					}
					else
					{
						// The server should not start up if this argument is invalid.
						throw new InvalidOperationException("Invalid value given for command line argument \"-ip\".");
					}
				})
				.AddFlag("-autocreate", (size) =>
				{
					if (!string.IsNullOrWhiteSpace(size))
					{
						Main.instance.autoCreate(size);
					}
				})
				.AddFlag("-worldevil", (value) =>
				{
					int worldEvil;
					switch (value.ToLower())
					{
						case "random":
							worldEvil = -1;
							break;
						case "corrupt":
							worldEvil = 0;
							break;
						case "crimson":
							worldEvil = 1;
							break;
						default:
							throw new InvalidOperationException(
								"Invalid value given for command line argument \"-worldevil\".");
					}

					ServerApi.LogWriter.PluginWriteLine(this,
						GetString("New worlds will be generated with the {0} world evil type!", value),
						TraceLevel.Verbose);
					WorldGen.WorldGenParam_Evil = worldEvil;
				})

				//Flags without arguments
				.AddFlag("-logclear", () => LogClear = true)
				.AddFlag("-autoshutdown", () => Main.instance.EnableAutoShutdown())
				.AddFlag("-dump", () => Utils.Dump());

			CliParser.ParseFromSource(parms);
		}

		/// <summary>HandleCommandLinePostConfigLoad - Handles additional command line options after the config file is read.</summary>
		/// <param name="parms">parms - The array of arguments passed in through the command line.</param>
		public static void HandleCommandLinePostConfigLoad(string[] parms)
		{
			FlagSet portSet = new FlagSet("-port");
			FlagSet playerSet = new FlagSet("-maxplayers", "-players");
			FlagSet restTokenSet = new FlagSet("--rest-token", "-rest-token");
			FlagSet restEnableSet = new FlagSet("--rest-enabled", "-rest-enabled");
			FlagSet restPortSet = new FlagSet("--rest-port", "-rest-port");

			CliParser
				.AddFlags(portSet, (p) =>
				{
					int port;
					if (int.TryParse(p, out port))
					{
						Netplay.ListenPort = port;
						Config.Settings.ServerPort = port;
						OverridePort = true;
						Log.ConsoleInfo(GetString("Port overridden by startup argument. Set to {0}", port));
					}
				})
				.AddFlags(restTokenSet, (token) =>
				{
					RESTStartupTokens.Add(token,
						new SecureRest.TokenData { Username = "null", UserGroupName = "superadmin" });
					Console.WriteLine(GetString("Startup parameter overrode REST token."));
				})
				.AddFlags(restEnableSet, (e) =>
				{
					bool enabled;
					if (bool.TryParse(e, out enabled))
					{
						Config.Settings.RestApiEnabled = enabled;
						Console.WriteLine(GetString("Startup parameter overrode REST enable."));
					}
				})
				.AddFlags(restPortSet, (p) =>
				{
					int restPort;
					if (int.TryParse(p, out restPort))
					{
						Config.Settings.RestApiPort = restPort;
						Console.WriteLine(GetString("Startup parameter overrode REST port."));
					}
				})
				.AddFlags(playerSet, (p) =>
				{
					int slots;
					if (int.TryParse(p, out slots))
					{
						Config.Settings.MaxSlots = slots;
						Console.WriteLine(
							GetString("Startup parameter overrode maximum player slot configuration value."));
					}
				});

			CliParser.ParseFromSource(parms);
		}

		/// <summary>SetupToken - The auth token used by the setup system to grant temporary superadmin access to new admins.</summary>
		public static int SetupToken = -1;

		private string _cliPassword = null;

		/// <summary>OnPostInit - Fired when the server loads a map, to perform world specific operations.</summary>
		/// <param name="args">args - The EventArgs object.</param>
		private void OnPostInit(EventArgs args)
		{
			Utils.SetConsoleTitle(false);

			//This is to prevent a bug where a CLI-defined password causes packets to be
			//sent in an unexpected order, resulting in clients being unable to connect
			if (!string.IsNullOrEmpty(Netplay.ServerPassword))
			{
				//CLI defined password overrides a config password
				if (!string.IsNullOrEmpty(Config.Settings.ServerPassword))
				{
					Log.ConsoleError(GetString(
						"!!! The server password in config.json was overridden by the interactive prompt and will be ignored."));
				}

				if (!Config.Settings.DisableUUIDLogin)
				{
					Log.ConsoleError(GetString(
						"!!! UUID login is enabled. If a user's UUID matches an account, the server password will be bypassed."));
					Log.ConsoleError(GetString(
						"!!! > Set DisableUUIDLogin to true in the config file and /reload if this is a problem."));
				}

				if (!Config.Settings.DisableLoginBeforeJoin)
				{
					Log.ConsoleError(GetString(
						"!!! Login before join is enabled. Existing accounts can login & the server password will be bypassed."));
					Log.ConsoleError(GetString(
						"!!! > Set DisableLoginBeforeJoin to true in the config file and /reload if this is a problem."));
				}

				_cliPassword = Netplay.ServerPassword;
				Netplay.ServerPassword = "";
				Config.Settings.ServerPassword = _cliPassword;
			}
			else
			{
				if (!string.IsNullOrEmpty(Config.Settings.ServerPassword))
				{
					Log.ConsoleInfo(GetString("A password for this server was set in config.json and is being used."));
				}
			}

			if (!Config.Settings.DisableLoginBeforeJoin)
			{
				Log.ConsoleInfo(GetString(
					"Login before join enabled. Users may be prompted for an account specific password instead of a server password on connect."));
			}

			if (!Config.Settings.DisableUUIDLogin)
			{
				Log.ConsoleInfo(GetString("Login using UUID enabled. Users automatically login via UUID."));
				Log.ConsoleInfo(GetString(
					"A malicious server can easily steal a user's UUID. You may consider turning this option off if you run a public server."));
			}

			// Disable the auth system if "setup.lock" is present or a user account already exists
			if (File.Exists(Path.Combine(SavePath, "setup.lock")) ||
			    (UserAccountManager.GetUserAccounts()?.Count() > 0))
			{
				SetupToken = 0;

				if (File.Exists(Path.Combine(SavePath, "setup-code.txt")))
				{
					Log.ConsoleInfo(GetString(
						"An account has been detected in the user database, but setup-code.txt is still present."));
					Log.ConsoleInfo(GetString(
						"TShock will now disable the initial setup system and remove setup-code.txt as it is no longer needed."));
					File.Delete(Path.Combine(SavePath, "setup-code.txt"));
				}

				if (!File.Exists(Path.Combine(SavePath, "setup.lock")))
				{
					// This avoids unnecessary database work, which can get ridiculously high on old servers as all users need to be fetched
					File.Create(Path.Combine(SavePath, "setup.lock"));
				}
			}
			else if (!File.Exists(Path.Combine(SavePath, "setup-code.txt")))
			{
				var r = new Random((int)DateTime.Now.ToBinary());
				SetupToken = r.Next(100000, 10000000);
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine(GetString("To setup the server, join the game and type {0}setup {1}",
					Commands.Specifier, SetupToken));
				Console.WriteLine(GetString("This token will display until disabled by verification. ({0}setup)",
					Commands.Specifier));
				Console.ResetColor();
				File.WriteAllText(Path.Combine(SavePath, "setup-code.txt"), SetupToken.ToString());
			}
			else
			{
				SetupToken = Convert.ToInt32(File.ReadAllText(Path.Combine(SavePath, "setup-code.txt")));
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine(GetString(
					"TShock Notice: setup-code.txt is still present, and the code located in that file will be used."));
				Console.WriteLine(GetString("To setup the server, join the game and type {0}setup {1}",
					Commands.Specifier, SetupToken));
				Console.WriteLine(GetString("This token will display until disabled by verification. ({0}setup)",
					Commands.Specifier));
				Console.ResetColor();
			}

			Utils.ComputeMaxStyles();
			Utils.FixChestStacks();

			if (Config.Settings.UseServerName)
			{
				Main.worldName = Config.Settings.ServerName;
			}

			UpdateManager = new UpdateManager();
		}

		/// <summary>LastCheck - Used to keep track of the last check for basically all time based checks.</summary>
		private DateTime LastCheck = DateTime.UtcNow;

		/// <summary>LastSave - Used to keep track of SSC save intervals.</summary>
		private DateTime LastSave = DateTime.UtcNow;

		/// <summary>OnUpdate - Called when ever the server ticks.</summary>
		/// <param name="args">args - EventArgs args</param>
		private void OnUpdate(EventArgs args)
		{
			// This forces Terraria to actually continue to update
			// even if there are no clients connected
			if (ServerApi.ForceUpdate)
			{
				Netplay.HasClients = true;
			}

			if (Backups.IsBackupTime)
			{
				Backups.Backup();
			}

			//call these every second, not every update
			if ((DateTime.UtcNow - LastCheck).TotalSeconds >= 1)
			{
				OnSecondUpdate();
				LastCheck = DateTime.UtcNow;
			}

			if (Main.ServerSideCharacter && (DateTime.UtcNow - LastSave).TotalMinutes >=
			    ServerSideCharacterConfig.Settings.ServerSideCharacterSave)
			{
				foreach (var player in Players)
				{
					// prevent null point exceptions
					if (player != null && player is { IsLoggedIn: true, IsDisabledPendingTrashRemoval: false })
					{
						CharacterManager.InsertPlayerData(player);
					}
				}

				LastSave = DateTime.UtcNow;
			}
		}

		/// <summary>OnSecondUpdate - Called effectively every second for all time based checks.</summary>
		private void OnSecondUpdate()
		{
			DisableFlags flags = Config.Settings.DisableSecondUpdateLogs
				? DisableFlags.WriteToConsole
				: DisableFlags.WriteToLogAndConsole;

			if (Config.Settings.ForceTime != "normal")
			{
				switch (Config.Settings.ForceTime)
				{
					case "day":
						ServerPlayer.ServerConsole.SetTime(true, 27000.0);
						break;
					case "night":
						ServerPlayer.ServerConsole.SetTime(false, 16200.0);
						break;
				}
			}

			foreach (ServerPlayer player in Players)
			{
				if (player != null && player.Active)
				{
					if (player.TilesDestroyed != null)
					{
						if (player.TileKillThreshold >= Config.Settings.TileKillThreshold)
						{
							player.Disable(GetString("Reached TileKill threshold."), flags);
							ServerPlayer.ServerConsole.RevertTiles(player.TilesDestroyed);
							player.TilesDestroyed.Clear();
						}
					}

					if (player.TileKillThreshold > 0)
					{
						player.TileKillThreshold = 0;
						//We don't want to revert the entire map in case of a disable.
						lock (player.TilesDestroyed)
						{
							player.TilesDestroyed.Clear();
						}
					}

					if (player.TilesCreated != null)
					{
						if (player.TilePlaceThreshold >= Config.Settings.TilePlaceThreshold)
						{
							player.Disable(GetString("Reached TilePlace threshold"), flags);
							lock (player.TilesCreated)
							{
								ServerPlayer.ServerConsole.RevertTiles(player.TilesCreated);
								player.TilesCreated.Clear();
							}
						}
					}

					if (player.TilePlaceThreshold > 0)
					{
						player.TilePlaceThreshold = 0;
					}

					if (player.RecentFuse > 0)
						player.RecentFuse--;

					if (Main.ServerSideCharacter && (player.TPlayer.SpawnX > 0) &&
					    (player.sX != player.TPlayer.SpawnX))
					{
						player.sX = player.TPlayer.SpawnX;
						player.sY = player.TPlayer.SpawnY;
					}

					if (Main.ServerSideCharacter && (player.sX > 0) && (player.sY > 0) && (player.TPlayer.SpawnX < 0))
					{
						player.TPlayer.SpawnX = player.sX;
						player.TPlayer.SpawnY = player.sY;
					}

					if (player.RememberedPositionPending > 0)
					{
						if (player.RememberedPositionPending == 1)
						{
							var pos = RememberedPosManager.GetLeavePos(player.Account.AccountId);
							if (pos is null)
							{
								return;
							}

							player.Teleport(pos.Value.X * 16, pos.Value.Y * 16);
							player.RememberedPositionPending = 0;
						}
						else
						{
							player.RememberedPositionPending--;
						}
					}

					if (player.TileLiquidThreshold >= Config.Settings.TileLiquidThreshold)
					{
						player.Disable(GetString("Reached TileLiquid threshold"), flags);
					}

					if (player.TileLiquidThreshold > 0)
					{
						player.TileLiquidThreshold = 0;
					}

					if (player.ProjectileThreshold >= Config.Settings.ProjectileThreshold)
					{
						player.Disable(GetString("Reached projectile threshold"), flags);
					}

					if (player.ProjectileThreshold > 0)
					{
						player.ProjectileThreshold = 0;
					}

					if (player.PaintThreshold >= Config.Settings.TilePaintThreshold)
					{
						player.Disable(GetString("Reached paint threshold"), flags);
					}

					if (player.PaintThreshold > 0)
					{
						player.PaintThreshold = 0;
					}

					if (player.HealOtherThreshold >= ServerBase.Config.Settings.HealOtherThreshold)
					{
						player.Disable(GetString("Reached HealOtherPlayer threshold"), flags);
					}

					if (player.HealOtherThreshold > 0)
					{
						player.HealOtherThreshold = 0;
					}

					if (player.RespawnTimer > 0 && --player.RespawnTimer == 0 && player.Difficulty != 2)
					{
						player.Spawn(PlayerSpawnContext.ReviveFromDeath);
					}

					if (!Main.ServerSideCharacter || (Main.ServerSideCharacter && player.IsLoggedIn))
					{
						if (!player.HasPermission(Permissions.ignorestackhackdetection))
						{
							player.IsDisabledForStackDetection = player.HasHackedItemStacks(shouldWarnPlayer: true);
						}

						if (player.IsBeingDisabled())
						{
							player.Disable(flags: flags);
						}
					}
				}
			}

			Bouncer.OnSecondUpdate();
			Utils.SetConsoleTitle(false);
		}

		/// <summary>OnHardUpdate - Fired when a hardmode tile update event happens.</summary>
		/// <param name="args">args - The HardmodeTileUpdateEventArgs object.</param>
		private void OnHardUpdate(HardmodeTileUpdateEventArgs args)
		{
			if (args.Handled)
				return;

			if (!OnCreep(args.Type))
			{
				args.Handled = true;
			}

			return;
		}

		/// <summary>OnWorldGrassSpread - Fired when grass is attempting to spread.</summary>
		/// <param name="args">args - The GrassSpreadEventArgs object.</param>
		private void OnWorldGrassSpread(GrassSpreadEventArgs args)
		{
			if (args.Handled)
				return;

			if (!OnCreep(args.Grass))
			{
				args.Handled = true;
			}

			return;
		}

		/// <summary>
		/// Checks if the tile type is allowed to creep
		/// </summary>
		/// <param name="tileType">Tile id</param>
		/// <returns>True if allowed, otherwise false</returns>
		private bool OnCreep(int tileType)
		{
			if (!Config.Settings.AllowCrimsonCreep && (tileType == TileID.Dirt || tileType == TileID.CrimsonGrass
			                                                                   || TileID.Sets.Crimson[tileType]))
			{
				return false;
			}

			if (!Config.Settings.AllowCorruptionCreep && (tileType == TileID.Dirt || tileType == TileID.CorruptThorns
				    || TileID.Sets.Corrupt[tileType]))
			{
				return false;
			}

			if (!Config.Settings.AllowHallowCreep && TileID.Sets.Hallow[tileType])
			{
				return false;
			}

			return true;
		}

		/// <summary>OnStatueSpawn - Fired when a statue spawns.</summary>
		/// <param name="args">args - The StatueSpawnEventArgs object.</param>
		private void OnStatueSpawn(StatueSpawnEventArgs args)
		{
			if (args.Within200 < Config.Settings.StatueSpawn200 && args.Within600 < Config.Settings.StatueSpawn600 &&
			    args.WorldWide < Config.Settings.StatueSpawnWorld)
			{
				args.Handled = true;
			}
			else
			{
				args.Handled = false;
			}

			return;
		}

		/// <summary>OnConnect - Fired when a player connects to the server.</summary>
		/// <param name="args">args - The ConnectEventArgs object.</param>
		private void OnConnect(ConnectEventArgs args)
		{
			if (ShuttingDown)
			{
				NetMessage.SendData((int)PacketTypes.Disconnect, args.Who, -1,
					NetworkText.FromLiteral(GetString("Server is shutting down...")));
				args.Handled = true;
				return;
			}

			var player = new ServerPlayer(args.Who);

			if (Utils.GetActivePlayerCount() + 1 > Config.Settings.MaxSlots + Config.Settings.ReservedSlots)
			{
				player.Kick(Config.Settings.ServerFullNoReservedReason, true, true, null, false);
				args.Handled = true;
				return;
			}

			if (!FileTools.OnWhitelist(player.IP))
			{
				player.Kick(Config.Settings.WhitelistKickReason, true, true, null, false);
				args.Handled = true;
				return;
			}

			if (Geo != null)
			{
				var code = Geo.TryGetCountryCode(IPAddress.Parse(player.IP));
				player.Country = code == null ? "N/A" : GeoIPCountry.GetCountryNameByCode(code);
				if (code == "A1")
				{
					if (Config.Settings.KickProxyUsers)
					{
						player.Kick(GetString("Connecting via a proxy is not allowed."), true, true, null, false);
						args.Handled = true;
						return;
					}
				}
			}

			Players[args.Who] = player;
			return;
		}

		/// <summary>OnJoin - Internal hook called when a player joins. This is called after OnConnect.</summary>
		/// <param name="args">args - The JoinEventArgs object.</param>
		private void OnJoin(JoinEventArgs args)
		{
			var player = Players[args.Who];
			if (player == null)
			{
				args.Handled = true;
				return;
			}

			if (Config.Settings.KickEmptyUUID && String.IsNullOrWhiteSpace(player.UUID))
			{
				player.Kick(
					GetString("Your client sent a blank UUID. Configure it to send one or use a different client."),
					true, true, null, false);
				args.Handled = true;
				return;
			}

			BanManager.IsPlayerBanned(player);
		}

		/// <summary>OnLeave - Called when a player leaves the server.</summary>
		/// <param name="args">args - The LeaveEventArgs object.</param>
		private void OnLeave(LeaveEventArgs args)
		{
			if (args.Who >= Players.Length || args.Who < 0)
			{
				//Something not right has happened
				return;
			}

			var tsplr = Players[args.Who];
			if (tsplr == null)
			{
				return;
			}

			Players[args.Who] = null;

			//Reset toggle creative powers to default, preventing potential power transfer & desync on another user occupying this slot later.

			foreach (var kv in CreativePowerManager.Instance._powersById)
			{
				var power = kv.Value;

				//No need to reset sliders - those are reset manually by the game, most likely an oversight that toggles don't receive this treatment.

				if (power is CreativePowers.APerPlayerTogglePower toggle)
				{
					if (toggle._perPlayerIsEnabled[args.Who] == toggle._defaultToggleState)
						continue;

					toggle.SetEnabledState(args.Who, toggle._defaultToggleState);
				}
			}

			if (tsplr.ReceivedInfo)
			{
				if (!tsplr.SilentKickInProgress && tsplr.State >= 3)
					Utils.Broadcast(GetString("{0} has left.", tsplr.Name), Color.Yellow);
				Log.Info(GetString("{0} disconnected.", tsplr.Name));

				if (tsplr.IsLoggedIn && !tsplr.IsDisabledPendingTrashRemoval && Main.ServerSideCharacter &&
				    (!tsplr.Dead || tsplr.TPlayer.difficulty != 2))
				{
					tsplr.PlayerData.CopyCharacter(tsplr);
					CharacterManager.InsertPlayerData(tsplr);
				}

				if (Config.Settings.RememberLeavePos && !tsplr.LoginHarassed)
				{
					RememberedPosManager.InsertLeavePos(tsplr.Account.AccountId, (int)(tsplr.X / 16),
						(int)(tsplr.Y / 16));
				}

				if (tsplr.tempGroupTimer is not null)
				{
					tsplr.tempGroupTimer.Stop();
				}
			}

			// Fire the OnPlayerLogout hook too, if the player was logged in and they have a ServerPlayer object.
			if (tsplr.IsLoggedIn)
			{
				Hooks.PlayerHooks.OnPlayerLogout(tsplr);
			}

			// The last player will leave after this hook is executed.
			if (Utils.GetActivePlayerCount() == 1)
			{
				if (Config.Settings.SaveWorldOnLastPlayerExit)
					SaveManager.Instance.SaveWorld();
				Utils.SetConsoleTitle(true);
			}
		}

		/// <summary>OnChat - Fired when a player chats. Used for handling chat and commands.</summary>
		/// <param name="args">args - The ServerChatEventArgs object.</param>
		private void OnChat(ServerChatEventArgs args)
		{
			var player = Players.ElementAtOrDefault(args.Who);

			if (args.Handled || player == default)
			{
				return;
			}

			if (args.Text.Length > 500)
			{
				player.Kick(GetString("Crash attempt via long chat packet."), true);
				args.Handled = true;
				return;
			}

			string text = args.Text;

			/* OLD TSHOCK NOTE FOR THIS SECTION */
			// Terraria now has chat commands on the client side.
			// These commands remove the commands prefix (e.g. /me /playing) and send the command id instead
			// In order for us to keep legacy code we must reverse this and get the prefix using the command id
			// TODO: Figure out how we can retrieve command prefix this without any of this code

			// Use LINQ to find the first matching command, if any
			var matchingCommand = Terraria.UI.Chat.ChatManager.Commands._localizedCommands
				.FirstOrDefault(item => item.Value._name == args.CommandId._name);

			// Check if a matching command was found
			if (matchingCommand.Key != null)
			{
				// Prepend the command key to 'text', adding a space if 'text' is not empty
				text = matchingCommand.Key.Value + (string.IsNullOrEmpty(text) ? "" : $" {text}");
			}

			// Check if the text starts with the command specifier or the silent command specifier
			if (Utils.IsCommand(text))
			{
				try
				{
					args.Handled = true;

					// Handle the command
					if (!Commands.HandleCommand(player, text))
					{
						// This is required in case anyone makes HandleCommand return false again
						player.SendErrorMessage(
							GetString("Unable to parse command. Please contact an administrator for assistance."));
						Log.ConsoleError(GetString("Unable to parse command '{0}' from player {1}."), text,
							player.Name);
					}
				}
				catch (Exception ex)
				{
					Log.ConsoleError(GetString("An exception occurred executing a command."));
					Log.Error(ex.ToString());
				}
			}
			else // player is sending a chat message
			{
				var canPlayerChat = player.HasPermission(Permissions.canchat);
				if (!canPlayerChat) // do they even have perms?
				{
					args.Handled = true;
					player.SendMessage("You do not have permission to chat.", Color.IndianRed);
					return;
				}

				if (player.IsMuted) // are they muted ?
				{
					args.Handled = true;
					player.SendErrorMessage(GetString("You are muted!"));
					return;
				}

				if (ServerBase.Config.Settings.EnableChatAboveHeads) // if chat above heads is enabled
				{
					var terrariaPlayer = player.TPlayer;
					string playerName = terrariaPlayer.name;

					// IMPLEMENT THIS LATER
					// NOT SURE HOW TO DO THIS YET
					// ALSO, NOT SURE IF WE EVEN NEED THIS
					args.Handled = true;
					return;
				}

				// user has perms, isn't muted, and chat above heads is disabled
				// so we can just send the message

				// format the chat message
				/*  {0} = group name
					{1} = group prefix
					{2} = player name
					{3} = group suffix
					{4} = chat message
					{5} = tags
				*/

				text = string.Format(Config.Settings.ChatFormat, player.Group?.Name, player.GetPrefix(), player.Name,
					player.Group.Suffix,
					args.Text, player.GetTagsText());

				// Invoke the PlayerChat hook. If this hook event handled then we need to prevent sending the chat message
				args.Handled = true;
				bool cancelChat = PlayerHooks.OnPlayerChat(player, args.Text, ref text);

				if (cancelChat)
				{
					return;
				}

				if (ServerBase.Utils.ContainsFilteredWord(text) && player.HasPermission(EthnessaAPI.Permissions.canbypassfilter) is false)
				{
					// censor filtered words with ***
					text = ServerBase.Utils.CensorFilteredWords(text);
					ServerBase.Log.Info($"Filtered word detected in chat message from user: {player.Name}.");
				}

				Utils.Broadcast(text, player.Group.R, player.Group.G, player.Group.B);
			}
		}

		/// <summary>
		/// Called when a command is issued from the server console.
		/// </summary>
		/// <param name="args">The CommandEventArgs object</param>
		private void ServerHooks_OnCommand(CommandEventArgs args)
		{
			if (args.Handled)
				return;

			if (string.IsNullOrWhiteSpace(args.Command))
			{
				args.Handled = true;
				return;
			}

			// Damn you ThreadStatic and Redigit
			if (Main.rand == null)
			{
				Main.rand = new UnifiedRandom();
			}

			if (args.Command == "autosave")
			{
				Main.autoSave = Config.Settings.AutoSave = !Config.Settings.AutoSave;
				if (Config.Settings.AutoSave)
					Log.ConsoleInfo(GetString("AutoSave Enabled"));
				else
					Log.ConsoleInfo(GetString("AutoSave Disabled"));
			}
			else if (args.Command.StartsWith(Commands.Specifier) || args.Command.StartsWith(Commands.SilentSpecifier))
			{
				Commands.HandleCommand(ServerPlayer.ServerConsole, args.Command);
			}
			else
			{
				Commands.HandleCommand(ServerPlayer.ServerConsole, "/" + args.Command);
			}

			args.Handled = true;
		}

		/// <summary>OnGetData - Called when the server gets raw data packets.</summary>
		/// <param name="e">e - The GetDataEventArgs object.</param>
		private void OnGetData(GetDataEventArgs e)
		{
			if (e.Handled)
				return;

			PacketTypes type = e.MsgID;

			var player = Players[e.Msg.whoAmI];
			if (player == null || !player.ConnectionAlive)
			{
				e.Handled = true;
				return;
			}

			if (player.RequiresPassword && type != PacketTypes.PasswordSend)
			{
				e.Handled = true;
				return;
			}

			if ((player.State < 10 || player.Dead) && (int)type > 12 && (int)type != 16 && (int)type != 42 &&
			    (int)type != 50 &&
			    (int)type != 38 && (int)type != 21 && (int)type != 22 && type != PacketTypes.SyncLoadout)
			{
				e.Handled = true;
				return;
			}

			int length = e.Length - 1;
			if (length < 0)
			{
				length = 0;
			}

			using (var data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length - 1))
			{
				// Exceptions are already handled
				e.Handled = GetDataHandlers.HandlerGetData(type, player, data);
			}
		}

		/// <summary>OnGreetPlayer - Fired when a player is greeted by the server. Handles things like the MOTD, join messages, etc.</summary>
		/// <param name="args">args - The GreetPlayerEventArgs object.</param>
		private void OnGreetPlayer(GreetPlayerEventArgs args)
		{
			var player = Players[args.Who];
			if (player == null)
			{
				args.Handled = true;
				return;
			}

			player.LoginMS = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

			if (Config.Settings.EnableGeoIP && ServerBase.Geo != null)
			{
				Log.Info(GetString("{0} ({1}) from '{2}' group from '{3}' joined. ({4}/{5})", player.Name, player.IP,
					player.Group.Name, player.Country, ServerBase.Utils.GetActivePlayerCount(),
					ServerBase.Config.Settings.MaxSlots));
				if (!player.SilentJoinInProgress)
					Utils.Broadcast(GetString("{0} ({1}) has joined.", player.Name, player.Country), Color.Yellow);
			}
			else
			{
				Log.Info(GetString("{0} ({1}) from '{2}' group joined. ({3}/{4})", player.Name, player.IP,
					player.Group.Name, ServerBase.Utils.GetActivePlayerCount(), ServerBase.Config.Settings.MaxSlots));
				if (!player.SilentJoinInProgress)
					Utils.Broadcast(GetString("{0} has joined.", player.Name), Color.Yellow);
			}

			if (Config.Settings.DisplayIPToAdmins)
				Utils.SendLogs(GetString("{0} has joined. IP: {1}", player.Name, player.IP), Color.Blue);

			player.SendFileTextAsMessage(FileTools.MotdPath);

			string pvpMode = Config.Settings.PvPMode.ToLowerInvariant();
			if (pvpMode == "always" || pvpMode == "pvpwithnoteam")
			{
				player.TPlayer.hostile = true;
				player.SendData(PacketTypes.TogglePvp, "", player.Index);
				ServerPlayer.All.SendData(PacketTypes.TogglePvp, "", player.Index);
			}

			if (!player.IsLoggedIn)
			{
				if (Main.ServerSideCharacter)
				{
					player.IsDisabledForSSC = true;
					player.SendErrorMessage(GetString(
						"Server side characters are enabled! Please {0}register or {0}login to play!",
						Commands.Specifier));
					player.LoginHarassed = true;
				}
				else if (Config.Settings.RequireLogin)
				{
					player.SendErrorMessage(GetString("Please {0}register or {0}login to play!", Commands.Specifier));
					player.LoginHarassed = true;
				}
			}

			player.LastNetPosition = new Vector2(Main.spawnTileX * 16f, Main.spawnTileY * 16f);

			if (Config.Settings.RememberLeavePos &&
			    (RememberedPosManager.GetLeavePos(player.Account.AccountId) != Vector2.Zero) &&
			    !player.LoginHarassed)
			{
				player.RememberedPositionPending = 3;
				player.SendInfoMessage(GetString("You will be teleported to your last known location..."));
			}

			args.Handled = true;
		}

		/// <summary>NpcHooks_OnStrikeNpc - Fired when an NPC strike packet happens.</summary>
		/// <param name="e">e - The NpcStrikeEventArgs object.</param>
		private void NpcHooks_OnStrikeNpc(NpcStrikeEventArgs e)
		{
			if (Config.Settings.InfiniteInvasion)
			{
				if (Main.invasionSize < 10)
				{
					Main.invasionSize = 20000000;
				}
			}

			return;
		}

		/// <summary>OnProjectileSetDefaults - Called when a projectile sets the default attributes for itself.</summary>
		/// <param name="e">e - The SetDefaultsEventArgs object parameterized with Projectile and int.</param>
		private void OnProjectileSetDefaults(SetDefaultsEventArgs<Projectile, int> e)
		{
			//tombstone fix.
			if (e.Info == ProjectileID.Tombstone ||
			    (e.Info >= ProjectileID.GraveMarker && e.Info <= ProjectileID.Obelisk) ||
			    (e.Info >= ProjectileID.RichGravestone1 && e.Info <= ProjectileID.RichGravestone5))
				if (Config.Settings.DisableTombstones)
					e.Object.SetDefaults(0);
			if (e.Info == ProjectileID.HappyBomb)
				if (Config.Settings.DisableClownBombs)
					e.Object.SetDefaults(0);
			if (e.Info == ProjectileID.SnowBallHostile)
				if (Config.Settings.DisableSnowBalls)
					e.Object.SetDefaults(0);
			if (e.Info == ProjectileID.BombSkeletronPrime)
				if (Config.Settings.DisablePrimeBombs)
					e.Object.SetDefaults(0);

			return;
		}

		/// <summary>NetHooks_SendData - Fired when the server sends data.</summary>
		/// <param name="e">e - The SendDataEventArgs object.</param>
		private void NetHooks_SendData(SendDataEventArgs e)
		{
			if (e.MsgId == PacketTypes.PlayerHp)
			{
				if (Main.player[(byte)e.number].statLife <= 0)
				{
					e.Handled = true;
					return;
				}
			}
			else if (e.MsgId == PacketTypes.ProjectileNew)
			{
				if (e.number >= 0 && e.number < Main.projectile.Length)
				{
					var projectile = Main.projectile[e.number];
					if (projectile.active && projectile.owner >= 0 &&
					    (GetDataHandlers.projectileCreatesLiquid.ContainsKey(projectile.type) ||
					     GetDataHandlers.projectileCreatesTile.ContainsKey(projectile.type)))
					{
						var player = Players[projectile.owner];
						if (player != null)
						{
							if (player.RecentlyCreatedProjectiles.Any(p => p.Index == e.number && p.Killed))
							{
								player.RecentlyCreatedProjectiles.RemoveAll(p => p.Index == e.number && p.Killed);
							}

							if (player.RecentlyCreatedProjectiles.All(p => p.Index != e.number))
							{
								player.RecentlyCreatedProjectiles.Add(new GetDataHandlers.ProjectileStruct()
								{
									Index = e.number,
									Type = (short)projectile.type,
									CreatedAt = DateTime.Now
								});
							}
						}
					}
				}
			}

			return;
		}

		/// <summary>OnStartHardMode - Fired when hard mode is started.</summary>
		/// <param name="e">e - The HandledEventArgs object.</param>
		private void OnStartHardMode(HandledEventArgs e)
		{
			if (Config.Settings.DisableHardmode)
				e.Handled = true;

			return;
		}

		/// <summary>OnConfigRead - Fired when the config file has been read.</summary>
		/// <param name="file">file - The config file object.</param>
		public void OnConfigRead(ConfigFile<EthnessaSettings> file)
		{
			NPC.defaultMaxSpawns = file.Settings.DefaultMaximumSpawns;
			NPC.defaultSpawnRate = file.Settings.DefaultSpawnRate;

			Main.autoSave = file.Settings.AutoSave;
			if (Backups != null)
			{
				Backups.KeepFor = file.Settings.BackupKeepFor;
				Backups.Interval = file.Settings.BackupInterval;
			}

			if (!OverridePort)
			{
				Netplay.ListenPort = file.Settings.ServerPort;
			}

			if (file.Settings.MaxSlots > Main.maxPlayers - file.Settings.ReservedSlots)
				file.Settings.MaxSlots = Main.maxPlayers - file.Settings.ReservedSlots;
			Main.maxNetPlayers = file.Settings.MaxSlots + file.Settings.ReservedSlots;

			Netplay.ServerPassword = "";
			if (!string.IsNullOrEmpty(_cliPassword))
			{
				//This prevents a config reload from removing/updating a CLI-defined password
				file.Settings.ServerPassword = _cliPassword;
			}

			Netplay.SpamCheck = false;
		}
	}
}
