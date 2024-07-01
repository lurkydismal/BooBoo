using System;
using System.Numerics;
using BooBoo.Util;
using BooBoo.Asset;
using BlakieLibSharp;
using Raylib_cs;
using BooBoo.Engine;

namespace BooBoo.GameState
{
    internal class ModelTestGameState : GameStateBase
    {
        DPArc characterArchive;
        CharacterModel characterModel;
        Camera3D camera;
        public Shader shader;
        public int mvpLoc;

        public override void Init()
        {
            gameState = this;
            shader = FileHelper.LoadShader("Shader/SkinnedMesh.vert", "Shader/StriveChar.frag").Value;
            mvpLoc = Raylib.GetShaderLocation(shader, "mvp");
            camera = new Camera3D();
            camera.Position = new Vector3(0.0f, 0.0f, 8.0f);
            camera.Target = new Vector3(0.0f, 0.0f, 0.0f);
            camera.FovY = 45.0f;
            camera.Projection = CameraProjection.Perspective;
            camera.Up = Vector3.UnitY;
            characterArchive = FileHelper.LoadArchive("Char/Sol.dparc");
            characterModel = new CharacterModel(characterArchive.GetFile("Models/Sol.dae").data);
        }

        public override void Update()
        {
            if(Raylib.IsKeyDown(KeyboardKey.W))
                camera.Position.Z += 2.0f / 60.0f;

            if (Raylib.IsKeyDown(KeyboardKey.S))
                camera.Position.Z -= 2.0f / 60.0f;

            if (Raylib.IsKeyDown(KeyboardKey.D))
                camera.Position.X += 2.0f / 60.0f;

            if (Raylib.IsKeyDown(KeyboardKey.A))
                camera.Position.X -= 2.0f / 60.0f;

            //Console.WriteLine(camera.Position);
        }

        public override void Draw()
        {
            Window.BeginDrawing();
            Raylib.BeginShaderMode(shader);
            Raylib.BeginMode3D(camera);
            characterModel.Draw();
            Raylib.EndMode3D();
            Raylib.EndShaderMode();
            Window.FinalizeDrawing();
        }

        public override void End()
        {
            
        }
    }
}
