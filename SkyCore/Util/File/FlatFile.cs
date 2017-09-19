using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiNET.Utils;
using SharpAvi;
using YamlDotNet.RepresentationModel;

namespace SkyCore.Util.File
{
	public class FlatFile
	{

		public static readonly Dictionary<string, FlatFile> LoadedConfigs = new Dictionary<string, FlatFile>();

		public static FlatFile ForFile(string fileName)
		{
			if (LoadedConfigs.ContainsKey(fileName))
			{
				return LoadedConfigs[fileName];
			}

			FlatFile flatFile = new FlatFile(fileName);

			LoadedConfigs.Add(fileName, flatFile);
			return flatFile;
		}

		//

		private readonly Dictionary<string, object> _contentMap = new Dictionary<string, object>();

		private readonly string _filename;
		private readonly YamlStream _yaml = new YamlStream();

		private FlatFile(String fileName)
		{
			_filename = fileName;

			SkyUtil.log($"Loading FlatFile from {fileName}");
			string content = System.IO.File.ReadAllText(fileName);

			var input = new StringReader(content);

			_yaml.Load(input);

			var mapping = (YamlMappingNode)_yaml.Documents[0].RootNode;

			foreach (var entry in mapping.Children)
			{
				LoadNode(((YamlScalarNode)entry.Key).Value, entry.Value);
			}

			SkyUtil.log("Initialized");
		}

		private void LoadNode(string key, YamlNode node)
		{
			switch (node.NodeType)
			{
				case YamlNodeType.Sequence:
				{
					SkyUtil.log($"Reached Sequence at {key}");

					object valueList = null;

					foreach (YamlNode yamlNode in (YamlSequenceNode) node)
					{
						if (valueList == null)
						{
							_contentMap.TryGetValue(((YamlScalarNode)yamlNode).Value, out valueList);

							if (valueList == null)
							{
								valueList = new List<object>();
								_contentMap.Add(key, valueList);
							}
						}

						((List<object>) valueList).Add(((YamlScalarNode)yamlNode).Value);
						SkyUtil.log($"Added item to sequence: {((YamlScalarNode)yamlNode).Value} for key {key}");
					}
					break;
				}
				case YamlNodeType.Alias:
				{
					SkyUtil.log($"Reached Alias at {key}");
					break;
				}
				case YamlNodeType.Mapping:
				{
					SkyUtil.log($"Reached Mapping at {key}");

					IDictionary<YamlNode, YamlNode> children = ((YamlMappingNode) node).Children;
					foreach (YamlNode yamlNode in children.Keys)
					{
						YamlNode value = children[yamlNode];
						LoadNode(key + "." + ((YamlScalarNode) yamlNode).Value, value);
					}
					break;
				}
				case YamlNodeType.Scalar:
				{
					try
					{
						_contentMap.Add(key, ((YamlScalarNode)node).Value);
						SkyUtil.log($"Loaded {key}: {((YamlScalarNode)node).Value}");
					}
					catch (ArgumentException e)
					{
						Console.WriteLine(e);
					}
					break;
				}
			}
		}

		public void Set(string key, object value)
		{
			YamlNode yamlNode;
			if (value is string)
			{
				yamlNode = new YamlScalarNode((string) value);
			}
			else if(double.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var number))
			{
				yamlNode = new YamlScalarNode("" + number);
			}
			else if (value is IEnumerable)
			{
				List<YamlNode> listChildren = new List<YamlNode>();
				foreach (object listValue in (IEnumerable) value)
				{
					var location = listValue as PlayerLocation;
					if (location != null)
					{
						listChildren.Add(new YamlScalarNode(SerializeLocation(location)));
					}
					else
					{
						listChildren.Add(new YamlScalarNode(listValue.ToString()));
					}
				}

				yamlNode = new YamlSequenceNode(listChildren);
			}
			else
			{
				SkyUtil.log($"Invalid object type for saving at '{key}'=>'{value}'");
				return;
			}

			((YamlMappingNode)_yaml.Documents[0].RootNode).Add(key, yamlNode);

			_yaml.Save(new StreamWriter(_filename, true), true);
		}

		//

		public bool Contains(string key)
		{
			foreach (string keyLoop in _contentMap.Keys)
			{
				SkyUtil.log($"Key: '{keyLoop}'");
			}
			return (string) Get(key, "") != "";
		}

		public object Get(string key, object defaultValue)
		{
			_contentMap.TryGetValue(key, out var value);

			SkyUtil.log($"Attempted to retrieve {key}, Found: {value != null} -> Returning {value ?? defaultValue}");

			return value ?? defaultValue;
		}

		public string GetString(string key, string defaultValue)
		{
			return Get(key, defaultValue) as string;
		}

		public int GetInt(string key, string defaultValue)
		{
			var value = Get(key, defaultValue) as int?;

			return value ?? 0;
		}

		public List<string> GetStringList(string key, List<string> defaultValue)
		{
			object value = Get(key, defaultValue);
			if (value is IList)
			{
				List<string> stringList = new List<string>();
				foreach (object objectValue in (IEnumerable) value)
				{
					stringList.Add(objectValue.ToString());
				}

				return stringList;
			}

			return defaultValue;
		}

		public PlayerLocation GetLocation(string key, PlayerLocation defaultValue)
		{
			string value = (string) Get(key, null);
			if (value == null)
			{
				return defaultValue;
			}

			return ParseLocation(value) ?? defaultValue;
		}

		public List<PlayerLocation> GetLocationList(string key, List<PlayerLocation> defaultValue)
		{
			List<string> locationStrings = GetStringList(key, null);
			if (locationStrings == null)
			{
				SkyUtil.log($"List not found at {key}");
				return defaultValue;
			}

			List<PlayerLocation> locations = new List<PlayerLocation>();
			foreach (string locationString in locationStrings)
			{
				PlayerLocation parsedLocation = ParseLocation(locationString);
				if (parsedLocation != null)
				{
					locations.Add(parsedLocation);
				}
				else
				{
					SkyUtil.log($"Parsed invalid location with {locationString}");
				}
			}

			SkyUtil.log($"Returning {locations.Count} locations");

			return locations;
		}

		//

		public static string SerializeLocation(PlayerLocation playerLocation)
		{
			return playerLocation.X + ":" + playerLocation.Y + ":" + playerLocation.Z + ":" + playerLocation.Yaw + ":" + playerLocation.Pitch;
		}

		public static PlayerLocation ParseLocation(string locationString)
		{
			string[] split = locationString.Split(':');

			try
			{
				if (split.Length == 3) //No Yaw/Pitch
				{
					return new PlayerLocation(double.Parse(split[0]), double.Parse(split[0]), double.Parse(split[0]));
				}
				else if (split.Length == 5) //Yaw/Pitch included
				{
					return new PlayerLocation(double.Parse(split[0]), double.Parse(split[1]), double.Parse(split[2]), float.Parse(split[3]), float.Parse(split[4]));
				}
				else //Invalid format
				{
					SkyUtil.log($"Invalid format provided for location='{locationString}'");
					return null;
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				return null;
			}
		}

	}
}
