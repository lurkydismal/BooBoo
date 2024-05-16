using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace BooBoo.Engine
{
    internal static class MusicManager
    {
        static Dictionary<string, SongTableEntry> songTable = new Dictionary<string, SongTableEntry>();

        public static void InitSongTable(SongTableEntry[] songs)
        {
            foreach(SongTableEntry song in songs)
                songTable.Add(song.Id, song);
        }

        public static SongTableEntry GetSongEntry(string id)
        {
            return songTable.ContainsKey(id) ? songTable[id] : null;
        }

        public static SongTableEntry[] GetAllEntries()
        {
            return songTable.Values.ToArray();
        }

        public class SongTableEntry
        {
            [JsonProperty("Name")]
            public string Name;
            [JsonProperty("Id")]
            public string Id;
            [JsonProperty("Num")]
            public int num;
            [JsonProperty("File")]
            public string File;
            [JsonProperty("LoopPoint")]
            public float LoopPoint;
        }
    }
}
