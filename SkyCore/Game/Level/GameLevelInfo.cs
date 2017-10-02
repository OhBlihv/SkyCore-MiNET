using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiNET.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SkyCore.Game.Level
{
	public class GameLevelInfo
	{

		public string GameType { get; }

		public string LevelName { get; }

		public PlayerLocation LobbyLocation { get; set; }

		public GameLevelInfo(string gameType, string levelName, PlayerLocation lobbyLocation)
		{
			GameType = gameType;
			LevelName = levelName;
			LobbyLocation = lobbyLocation;
		}

	}

	public class GameLevelInfoConverter : JsonConverter
	{
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			//TODO?
			throw new NotImplementedException();
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			JObject item = JObject.Load(reader);

			Type newObjectType = null;
			if (item.TryGetValue("GameType", out var serialisedGameType))
			{
				if (SkyCoreAPI.Instance.GameModes.ContainsKey(serialisedGameType.Value<string>()))
				{
					newObjectType = SkyCoreAPI.Instance.GameModes[serialisedGameType.Value<string>()].GetGameLevelInfoType();
				}
				else
				{
					SkyUtil.log($"GameType {serialisedGameType.Value<string>()} not found/loaded.");
					return null;
				}
			}

			if (newObjectType == null)
			{
				SkyUtil.log("Could not find gametype in json object.");
				return null;
			}

			//existingValue = Convert.ChangeType(existingValue, objectType);
			existingValue = Activator.CreateInstance(objectType);

			serializer.Populate(item.CreateReader(), existingValue);

			return existingValue;
		}

		public override bool CanConvert(Type objectType)
		{
			return typeof(GameLevelInfo).IsAssignableFrom(objectType);
		}

	}

}
