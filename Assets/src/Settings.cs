using System.IO;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace src
{
    public class Settings
    {
        public string editoastUrl;
        public int infraId;
        public int timetableId;
        public float startLatitude;
        public float startLongitude;
        public int startZoomLevel;
        public string mapboxApiKey;

        private static Settings _instance = null;

        public static Settings GetInstance()
        {
            if (_instance == null)
            {
                var configFilePath = Path.Combine(Application.streamingAssetsPath, "config.yaml");
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                string yaml = File.ReadAllText(configFilePath);
                return deserializer.Deserialize<Settings>(yaml);
            }
            return _instance;
        }
    }
}
