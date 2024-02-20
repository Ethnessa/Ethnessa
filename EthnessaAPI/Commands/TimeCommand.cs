using System;
using System.Collections.Generic;
using System.Linq;
using EthnessaAPI.Models;
using Terraria;

namespace EthnessaAPI.ServerCommands;

public class TimeCommand : Command
{
	public override List<string> Names { get; protected set; } = new List<string> { "time" };
	public override List<string> Permissions { get; protected set; } = new() { EthnessaAPI.Permissions.time };
	public override CommandDelegate CommandDelegate { get; set; } = Time;

	private static void Time(CommandArgs args)
	{
		var input = args.Parameters.ElementAtOrDefault(0)?.ToLower();

		// Check if no input is given and show the current time.
		if (input == null)
		{
			ShowCurrentTime(args);
			return;
		}

		// Predefined time settings for day, night, noon, and midnight.
		var predefinedTimes = new Dictionary<string, (bool isDayTime, double time)>
		{
			["day"] = (true, 0.0),
			["night"] = (false, 0.0),
			["noon"] = (true, 27000.0),
			["midnight"] = (false, 16200.0)
		};

		if (predefinedTimes.TryGetValue(input, out var timeSetting))
		{
			SetPredefinedTime(args, timeSetting.isDayTime, timeSetting.time);
			return;
		}

		// Custom time setting based on input.
		SetCustomTime(args, input);
	}

	private static void ShowCurrentTime(CommandArgs args)
	{
		double time = Main.time / 3600.0 + 4.5;
		if (!Main.dayTime)
			time += 15.0;
		time %= 24.0;

		int hours = (int)Math.Floor(time);
		int minutes = (int)Math.Floor((time % 1.0) * 60.0);
		args.Player.SendInfoMessage($"The current time is {hours}:{minutes:D2}.");
	}

	private static void SetPredefinedTime(CommandArgs args, bool isDayTime, double time)
	{
		ServerPlayer.ServerConsole.SetTime(isDayTime, time);
		string timeString = isDayTime ? "04:30" : "19:30";
		if (time == 27000.0) timeString = "12:00";
		else if (time == 16200.0) timeString = "00:00";
		ServerPlayer.All.SendInfoMessage($"{args.Player.Name} set the time to {timeString}.");
	}

	private static void SetCustomTime(CommandArgs args, string input)
	{
		string[] parts = input.Split(':');
		if (parts.Length != 2 || !int.TryParse(parts[0], out int hours) || !int.TryParse(parts[1], out int minutes) ||
		    hours < 0 || hours > 23 || minutes < 0 || minutes > 59)
		{
			args.Player.SendErrorMessage("Invalid time string. Proper format: hh:mm, in 24-hour time.");
			return;
		}

		decimal customTime = hours + (minutes / 60.0m) - 4.5m;
		if (customTime < 0) customTime += 24;

		bool isDayTime = customTime < 15.0m;
		double gameTime = (double)((customTime - (isDayTime ? 0 : 15.0m)) * 3600.0m);

		ServerPlayer.ServerConsole.SetTime(isDayTime, gameTime);
		ServerPlayer.All.SendInfoMessage($"{args.Player.Name} set the time to {hours}:{minutes:D2}.");
	}
}
