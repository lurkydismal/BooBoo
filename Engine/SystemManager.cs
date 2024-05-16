using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using BooBoo.GameState;
using BooBoo.Util;
using BlakieLibSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace BooBoo.Engine
{
    internal static class SystemManager
    {
        static Dictionary<string, CharTableEntry> charTable = new Dictionary<string, CharTableEntry>();

        public static void InitSystem()
        {
            DPArc archive = FileHelper.LoadArchive("System.dparc");
            CharTableEntry[] chars = JObject.Parse(archive.GetFile("CharTable.json").DataAsString())["Chars"].ToObject<CharTableEntry[]>();
            foreach (CharTableEntry chara in chars)
                charTable.Add(chara.Id, chara);
            MusicManager.InitSongTable(JObject.Parse(archive.GetFile("SongTable.json").DataAsString())["Songs"].ToObject<MusicManager.SongTableEntry[]>());
            FontsManager.LoadFonts(JObject.Parse(archive.GetFile("Fonts.json").DataAsString())["FontPaths"].ToObject<FontsManager.FontPath[]>());
            CSelGameState.InitCselTable(JsonConvert.DeserializeObject<CSelGameState.CselTable>(archive.GetFile("Csel.json").DataAsString()));
            Battle.BattleStage.InitStageTable(JObject.Parse(archive.GetFile("StageTable.json").DataAsString())["Stages"].ToObject<Battle.BattleStage.StageDef[]>());
            archive.Dispose();
        }

        public static bool CharExists(string id)
        {
            return charTable.ContainsKey(id);
        }

        public static CharTableEntry GetChar(string id)
        {
            return charTable.ContainsKey(id) ? charTable[id] : null;
        }

        public static CharTableEntry[] GetAllChars()
        {
            return charTable.Values.ToArray();
        }

        public static string[] GetAllIds()
        {
            return charTable.Keys.ToArray();
        }

        public class CharTableEntry
        {
            [JsonProperty("Name_Long")]
            public string Name_Short;
            [JsonProperty("Name_Short")]
            public string Name_Long;
            [JsonProperty("Id")]
            public string Id;
            [JsonProperty("Num")]
            public int Num;
            [JsonProperty("DLCSettings")]
            public DLCSettings DLCSettings;
            [JsonProperty("MajorArchetype")]
            public CharArchetype MajorArchetype;
            [JsonProperty("MinorArchetype")]
            public CharArchetype MinorArchetype;
            [JsonProperty("CanBeRandom")]
            public bool CanBeRandom;
            [JsonProperty("Stage")]
            public string Stage;
            [JsonProperty("Song")]
            public string Song;
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum DLCSettings
        {
            None,
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum CharArchetype
        {
            Power,
            Zoning,
            Rushdown,
            Balance,
            Uniuqe,
            Grappler,
            Mixup,
            Puppet,
            GlassCannon,
            HitAndRun
        }
    }
}
