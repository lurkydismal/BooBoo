using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Raylib_cs;

namespace BooBoo.Battle
{
    internal class BattleStage
    {
        #region stage table stuff
        public static Dictionary<string, StageDef> stageTable = new Dictionary<string, StageDef>();

        public static void InitStageTable(StageDef[] stages)
        {
            foreach(StageDef stage in stages)
                stageTable.TryAdd(stage.Id, stage);
        }

        public static StageDef[] GetAllStages()
        {
            return stageTable.Values.ToArray();
        }
        #endregion

        public static BattleStage stage;

        public StageDef stageDef { get; private set; }
        public float stageWidth { get { return stageDef.StageWidth; } }
        public float maxPlayerDistance { get { return stageDef.MaxPlayerDistance; } }

        public BattleStage(string loadId)
        {
            stageDef = stageTable[loadId];
            stage = this;
        }

        public void Update()
        {

        }

        public void Draw()
        {
            Color clearColor = new Color(stageDef.ClearColor[0], stageDef.ClearColor[1], stageDef.ClearColor[2], stageDef.ClearColor[3]);
            Raylib.ClearBackground(clearColor);
            Raylib.BeginMode3D(BattleCamera.activeCamera);

            //Draw stage mesh stuff

            if (stageDef.DrawGrid)
                Raylib.DrawGrid((int)stageDef.StageWidth * 2, 1.0f);

            Raylib.EndMode3D();
        }

        public struct StageDef
        {
            [JsonProperty("Name")]
            public string Name;
            [JsonProperty("Id")]
            public string Id;
            [JsonProperty("Num")]
            public int Num;
            [JsonProperty("FilePath")]
            public string FilePath;
            [JsonProperty("HasLuaScript")]
            public bool HasLuaScript;
            [JsonProperty("DrawGrid")]
            public bool DrawGrid;
            [JsonProperty("ClearColor")]
            public byte[] ClearColor;
            [JsonProperty("Shaders")]
            public string[] Shaders;
            [JsonProperty("ShaderMatAssign")]
            public int[] ShaderMatAssign; //this is quirky. array length the size of mats in model, int is index of what shader it should apply to the mat
            [JsonProperty("LightDir")]
            public float[] LightDir;
            [JsonProperty("StageWidth")]
            public float StageWidth;
            [JsonProperty("MaxPlayerDistance")]
            public float MaxPlayerDistance;
            [JsonProperty("GroundTypes")]
            public GroundType[] GroundTypes;

            public struct GroundType
            {
                [JsonProperty("FloorType")]
                public FloorType FloorType;
                [JsonProperty("ShadowType")]
                public ShadowType ShadowType;
                [JsonProperty("CustomShadowShader")]
                public string CustomShadowShader;
                [JsonProperty("FloorSize")]
                public float FloorSize;
            }
        }
    }
}
