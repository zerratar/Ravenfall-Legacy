using System;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace Shinobytes.IO
{
    public static class Path
    {
        public static string GameFolder;
        private static bool initFailed;
        static Path()
        {
            Init();
        }

        private static void Init()
        {
            try
            {
                var dataFolder = new System.IO.DirectoryInfo(Application.dataPath);
                GameFolder = dataFolder.Parent.FullName;
                initFailed = false;
            }
            catch
            {
                initFailed = true;
            }
        }
        public static string GameFolderAsRoot(string path)
        {
            if (initFailed) Init();
            if (System.IO.Path.IsPathFullyQualified(path)) return path;
            if (initFailed) return System.IO.Path.GetFullPath(path);
            return System.IO.Path.GetFullPath(System.IO.Path.Combine(GameFolder, path));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetFilePath(string playerStateCacheFileName)
        {
            return GameFolderAsRoot(playerStateCacheFileName);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Combine(params string[] paths)
        {
            if (paths == null || paths.Length == 0)
            {
                return GameFolderAsRoot("./");
            }

            paths[0] = GameFolderAsRoot(paths[0]);
            return System.IO.Path.Combine(paths);
        }
    }

    public static class Directory
    {

        internal static bool Exists(string path)
        {
            return System.IO.Directory.Exists(Shinobytes.IO.Path.GameFolderAsRoot(path));
        }

        internal static System.IO.DirectoryInfo CreateDirectory(string path)
        {
            return System.IO.Directory.CreateDirectory(Shinobytes.IO.Path.GameFolderAsRoot(path));
        }
    }
    public static class File
    {
        public static string[] ReadAllLines(string path)
        {
            Shinobytes.Debug.Log("Loading: " + path);
            return System.IO.File.ReadAllLines(Shinobytes.IO.Path.GameFolderAsRoot(path));
        }

        public static string[] ReadAllLines(string path, Encoding encoding)
        {
            Shinobytes.Debug.Log("Loading: " + path);
            return System.IO.File.ReadAllLines(Shinobytes.IO.Path.GameFolderAsRoot(path), encoding);
        }

        public static bool Exists(string path)
        {
            return System.IO.File.Exists(Shinobytes.IO.Path.GameFolderAsRoot(path));
        }

        public static void WriteAllLines(string path, string[] lines)
        {
            System.IO.File.WriteAllLines(Shinobytes.IO.Path.GameFolderAsRoot(path), lines);
        }

        public static void Delete(string path)
        {
            Shinobytes.Debug.Log("Deleting: " + path);
            System.IO.File.Delete(Shinobytes.IO.Path.GameFolderAsRoot(path));
        }

        public static System.IO.FileStream OpenWrite(string path)
        {
            return System.IO.File.OpenWrite(Shinobytes.IO.Path.GameFolderAsRoot(path));
        }

        internal static void WriteAllText(string path, string text)
        {
            System.IO.File.WriteAllText(Shinobytes.IO.Path.GameFolderAsRoot(path), text);
        }

        internal static string ReadAllText(string path)
        {
            return System.IO.File.ReadAllText(Shinobytes.IO.Path.GameFolderAsRoot(path));
        }

        internal static void Copy(string source, string destination, bool overwrite)
        {
            System.IO.File.Copy(Shinobytes.IO.Path.GameFolderAsRoot(source), Shinobytes.IO.Path.GameFolderAsRoot(destination), overwrite);
        }

        internal static void WriteAllBytes(string path, byte[] bytes)
        {
            System.IO.File.WriteAllBytes(Shinobytes.IO.Path.GameFolderAsRoot(path), bytes);
        }
    }
}
