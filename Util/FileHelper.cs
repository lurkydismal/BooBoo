using System;
using System.IO;
using BlakieLibSharp;
using Raylib_cs;

namespace BooBoo.Util
{
    internal static class FileHelper
    {
        //dont wanna copy data folder a lot so lets get it from project directory. this is built asuming we're in {project}/bin/debug
        private static readonly string ProjectPath = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName + "/Data/";
        private static string GetFullPath(string path)
        {
#if DEBUG
            return ProjectPath + path;
#else
            return Environment.CurrentDirectory + '/' + path;
#endif
        }

        public static DPArc LoadArchive(string path)
        {
            path = GetFullPath(path);
            if (!File.Exists(path))
                return null;
            DPArc rtrn;
            using (BinaryReader file = new BinaryReader(File.OpenRead(path)))
                rtrn = new DPArc(file);
            return rtrn;
        }

        public static ArchiveFile LoadFileFromArchive(string fullPath)
        {
            int dparcIndex = fullPath.IndexOf(".dparc");
            if (dparcIndex <= 0)
                return null;
            string archivePath = fullPath.Substring(0, dparcIndex + 7);
            string fileName = fullPath.Substring(dparcIndex + 6);
            return LoadFileFromArchive(archivePath, fileName);
        }

        public static ArchiveFile LoadFileFromArchive(string archivePath, string fileName)
        {
            archivePath = GetFullPath(archivePath);
            if (!File.Exists(archivePath))
                return null;
            ArchiveFile rtrn;
            using (BinaryReader file = new BinaryReader(File.OpenRead(archivePath)))
                using (DPArc arc = new DPArc(file))
                {
                    if (arc.FileExists(fileName))
                        rtrn = arc.GetFile(fileName);
                    else
                        rtrn = null;
                }
            return rtrn;
        }

        public static PrmAn LoadPrmAn(string path)
        {
            path = GetFullPath(path);
            if (!File.Exists(path))
                return null;
            PrmAn rtrn;
            using (BinaryReader file = new BinaryReader(File.OpenRead(path)))
                rtrn = new PrmAn(file);
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
