using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Assimp;
using BooBoo.Util;
using Raylib_cs;

namespace BooBoo.Asset
{
    internal class SkinnedMesh
    {
        Vertex[] vertices;
        ushort[] indices;

        uint vao;
        uint vbo;
        uint ebo;

        public SkinnedMesh(Vertex[] vertices, ushort[] indices)
        {
            this.vertices = vertices;
            this.indices = indices;

            unsafe
            {
                vao = Rlgl.LoadVertexArray();
                Rlgl.EnableVertexArray(vao);
                fixed(void* verts = &this.vertices[0]) vbo = Rlgl.LoadVertexBuffer(verts, vertices.Length * Vertex.VertexSize, false);
                fixed (void* edges = &this.indices[0]) ebo = Rlgl.LoadVertexBufferElement(edges, indices.Length * sizeof(ushort), false);

                Rlgl.SetVertexAttribute(0, 3, Rlgl.FLOAT, false, Vertex.VertexSize, (void*)0);
                Rlgl.EnableVertexAttribute(0);
                Rlgl.SetVertexAttribute(1, 2, Rlgl.FLOAT, false, Vertex.VertexSize, (void*)(sizeof(float) * 3));
                Rlgl.EnableVertexAttribute(1);
                Rlgl.SetVertexAttribute(2, 3, Rlgl.FLOAT, false, Vertex.VertexSize, (void*)(sizeof(float) * 5));
                Rlgl.EnableVertexAttribute(2);
                Rlgl.SetVertexAttribute(3, 4, Rlgl.FLOAT, false, Vertex.VertexSize, (void*)(sizeof(float) * 8));
                Rlgl.EnableVertexAttribute(3);
                Rlgl.SetVertexAttribute(4, 4, Rlgl.FLOAT, false, Vertex.VertexSize, (void*)(sizeof(float) * 12));
                Rlgl.EnableVertexAttribute(4);
                Rlgl.SetVertexAttribute(5, 4, Rlgl.FLOAT, false, Vertex.VertexSize, (void*)(sizeof(float) * 16));
                Rlgl.EnableVertexAttribute(5);

                Rlgl.DisableVertexArray();
            }
        }

        ~SkinnedMesh()
        {
            Rlgl.UnloadVertexArray(vao);
            Rlgl.UnloadVertexBuffer(vbo);
            Rlgl.UnloadVertexBuffer(ebo);
        }

        public static SkinnedMesh LoadMesh(Assimp.Mesh mesh)
        {
            Vertex[] vertices = new Vertex[mesh.VertexCount];
            int[] indicesInt = mesh.GetIndices();
            ushort[] indices = Array.ConvertAll(indicesInt, input => (ushort)input);
            for (int i = 0; i < mesh.VertexCount; i++)
            {
                vertices[i] = new Vertex();
                vertices[i].position = mesh.Vertices[i].ToVector3();
                vertices[i].uv = mesh.TextureCoordinateChannels[0][i].ToVector2();
                vertices[i].normal = mesh.Normals[i].ToVector3();
                vertices[i].color = mesh.VertexColorChannels[0][i].ToVector4();
                int bonesAdded = 0;
                vertices[i].bones = new Vector4(-1.0f);
                vertices[i].weights = new Vector4(0.0f);
            }
            List<Bone> bones = mesh.Bones;
            //ugly :skull:
            for (int i = 0; i < bones.Count; i++)
                for (int j = 0; j < bones[i].VertexWeightCount; j++)
                    for (int l = 0; l < 4; l++)
                        switch(l)
                        {
                            case 0:
                                if (vertices[bones[i].VertexWeights[j].VertexID].bones.X == -1.0f)
                                {
                                    vertices[bones[i].VertexWeights[j].VertexID].bones.X = i;
                                    vertices[bones[i].VertexWeights[j].VertexID].weights.X = bones[i].VertexWeights[j].Weight;
                                    continue;
                                }
                                break;
                            case 1:
                                if (vertices[bones[i].VertexWeights[j].VertexID].bones.Y == -1.0f)
                                {
                                    vertices[bones[i].VertexWeights[j].VertexID].bones.Y = i;
                                    vertices[bones[i].VertexWeights[j].VertexID].weights.Y = bones[i].VertexWeights[j].Weight;
                                    continue;
                                }
                                break;
                            case 2:
                                if (vertices[bones[i].VertexWeights[j].VertexID].bones.Z == -1.0f)
                                {
                                    vertices[bones[i].VertexWeights[j].VertexID].bones.Z = i;
                                    vertices[bones[i].VertexWeights[j].VertexID].weights.Z = bones[i].VertexWeights[j].Weight;
                                    continue;
                                }
                                break;
                            case 3:
                                if (vertices[bones[i].VertexWeights[j].VertexID].bones.W == -1.0f)
                                {
                                    vertices[bones[i].VertexWeights[j].VertexID].bones.W = i;
                                    vertices[bones[i].VertexWeights[j].VertexID].weights.W = bones[i].VertexWeights[j].Weight;
                                    continue;
                                }
                                break;
                        }

            return new SkinnedMesh(vertices, indices);
        }

        public unsafe void Draw()
        {
            Rlgl.EnableVertexArray(vao);
            Rlgl.EnableVertexBufferElement(ebo);
            Rlgl.DrawVertexArrayElements(0, indices.Length, null);
            Rlgl.DisableVertexBufferElement();
            Rlgl.DisableVertexArray();
        }

        public struct Vertex
        {
            public Vector3 position;
            public Vector2 uv;
            public Vector3 normal;
            public Vector4 color;

            public Vector4 bones;
            public Vector4 weights;
            public const int VertexMaxBoneInfluence = 4;

            public const int VertexSize = sizeof(float) * 20;
        }
    }
}
