using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Assimp;
using Raylib_cs;

namespace BooBoo.Asset
{
    //Character Model is what defines world space along with animation frames to use and whatnot
    //Skinned Mesh is just the mesh and skeleton data
    internal class CharacterModel
    {
        public Vector3 position = Vector3.Zero;
        public Vector3 rotation = Vector3.Zero;
        public Vector3 scale = Vector3.One;

        public SkinnedMesh[] meshes;
        
        public CharacterModel(Scene scene)
        {
            LoadMeshes(scene);
        }

        public CharacterModel(byte[] data)
        {
            Scene scene;
            using (MemoryStream stream = new MemoryStream(data))
                scene = new AssimpContext().ImportFileFromStream(stream, PostProcessSteps.JoinIdenticalVertices | PostProcessSteps.Triangulate 
                    | PostProcessSteps.GenerateNormals | PostProcessSteps.SortByPrimitiveType);
            LoadMeshes(scene);
        }

        private void LoadMeshes(Scene scene)
        {
            meshes = new SkinnedMesh[scene.MeshCount];
            for(int i = 0; i < meshes.Length; i++)
                meshes[i] = SkinnedMesh.LoadMesh(scene.Meshes[i]);
        }

        public void Draw()
        {
            System.Numerics.Matrix4x4 mvp = System.Numerics.Matrix4x4.Transpose(Rlgl.GetMatrixTransform());// = System.Numerics.Matrix4x4.Identity;
            mvp *= System.Numerics.Matrix4x4.Transpose(Rlgl.GetMatrixModelview());
            mvp *= System.Numerics.Matrix4x4.Transpose(Rlgl.GetMatrixProjection());
            //Console.WriteLine(mvp);
            Raylib.SetShaderValueMatrix((GameState.GameStateBase.gameState as GameState.ModelTestGameState).shader,
                (GameState.GameStateBase.gameState as GameState.ModelTestGameState).mvpLoc, System.Numerics.Matrix4x4.Transpose(mvp));
            foreach (SkinnedMesh mesh in meshes)
                mesh.Draw();
        }
    }
}
