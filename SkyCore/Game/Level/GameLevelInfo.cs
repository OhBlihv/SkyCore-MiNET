using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiNET.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkyCore.Games.Murder.Level;

namespace SkyCore.Game.Level
{

	[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
	public class GameLevelInfo : ICloneable
	{

		public string GameType { get; set; }

		public string LevelName { get; set; }
		
		public int WorldTime { get; set; }

		public PlayerLocation LobbyLocation { get; set; }

		public PlayerLocation LobbyNPCLocation { get; set; }

		//JSON Loading
		public GameLevelInfo()
		{
			
		}

		public GameLevelInfo(string gameType, string levelName, int worldTime, PlayerLocation lobbyLocation)
		{
			GameType = gameType;
			LevelName = levelName;
			WorldTime = worldTime;
			LobbyLocation = lobbyLocation;
		}

		public virtual object Clone()
		{
			return new GameLevelInfo(GameType, LevelName, WorldTime, (PlayerLocation) LobbyLocation.Clone());
		}

	}

	public class GameLevelInfoJsonConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return typeof(GameLevelInfo).IsAssignableFrom(objectType);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var item = JObject.Load(reader);
			
			var newItem = Activator.CreateInstance(objectType);

			serializer.Populate(item.CreateReader(), newItem);

			return newItem;
		}

		public override void WriteJson(JsonWriter writer,
			object value, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}
	}

}
