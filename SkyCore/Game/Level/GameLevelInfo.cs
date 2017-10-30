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
	public class GameLevelInfo : ICloneable
	{

		public string GameType { get; }

		public string LevelName { get; }
		
		public int WorldTime { get; }

		public PlayerLocation LobbyLocation { get; set; }

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
					SkyUtil.log($"Attempting to load GameInfo of type {serialisedGameType.Value<string>()}");
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

			//existingValue = Convert.ChangeType(existingValue, newObjectType);
			existingValue = Activator.CreateInstance(newObjectType);

			serializer.Populate(item.CreateReader(), existingValue);

			SkyUtil.log($"Loaded as {newObjectType} ({existingValue.GetType()}");

			return existingValue;
		}

		public override bool CanRead => true;

		public override bool CanConvert(Type objectType)
		{
			SkyUtil.log($"Is {typeof(GameLevelInfo)} assignable from {objectType} == {typeof(GameLevelInfo).IsAssignableFrom(objectType)}");
			return typeof(GameLevelInfo).IsAssignableFrom(objectType);
		}

	}

}
