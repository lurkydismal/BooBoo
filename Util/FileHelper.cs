using System;
using System.IO;
using BlakieLibSharp;
using Raylib_cs;

namespace BooBoo.Util
{
    internal static class FileHelper
    {
        private static readonly string ProjectPath = "E:/Repos/BooBoo/Data/";
        private static string GetFullPath(string path)
        {
#if DEBUG
            return ProjectPath + path;
#else
            return Directory.GetCurrentDirectory() + '/' + path;
#endif
        }

        public static DPArc LoadArchive(string path)
        {
            path = GetFullPath(path);
            if (!File.Exists(path))
                return null;
            BinaryReader file = new BinaryReader(File.OpenRead(path));
            return new DPArc(file);
        }

        public static PrmAn LoadPrmAn(string path)
        {
            path = GetFullPath(path);
            if (!File.Exists(path))
                return null;
            BinaryReader file = new BinaryReader(File.OpenRead(path));
            PrmAn rtrn = new PrmAn(file);
            file.Close();
            rtrn.LoadTexturesToGPU();
            return rtrn;
        }

        public static Shader? LoadShader(string vertexPath, string fragmentPath)
        {
            vertexPath = GetFullPath(vertexPath);
            fragmentPath = GetFullPath(fragmentPath);
            if (!File.Exists(vertexPath) || !File.Exists(fragmentPath))
                return null;
            return Raylib.LoadShader(vertexPath, fragmentPath);
        }

        public static Font? LoadFont(string path)
        {
            path = GetFullPath(path);
            if (!File.Exists(path))
                return null;
            return Raylib.LoadFont(path);
        }

        public static bool FileExists(string path)
        {
            path = GetFullPath(path);
            return File.Exists(path);
        }
    }
}
