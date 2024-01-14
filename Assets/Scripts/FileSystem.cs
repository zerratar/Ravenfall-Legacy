using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace Shinobytes.IO
{
    public static class Path
    {
        private static string gameFolder;

        private static bool initFailed;
        
        public static string Executable;

        public static string GameFolder
        {
            get
            {
                if (string.IsNullOrEmpty(gameFolder))
                {
                    Init();
                }

                return gameFolder;
            }
        }

        static Path()
        {
            Init();
        }

        private static void Init()
        {
            try
            {

                var startupArgs = Environment.GetCommandLineArgs().Select(x => x.ToLower()).ToArray();
                Path.Executable = startupArgs.FirstOrDefault(x => x.ToLower().EndsWith(".exe"));

                var dataFolder = new System.IO.DirectoryInfo(Application.dataPath);
                gameFolder = dataFolder.Parent.FullName;

                Shinobytes.Debug.Log("Game Folder: " + gameFolder);

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
            var fullPath = Path.GameFolderAsRoot(path);
            Debug.Log("Deleting: " + fullPath);
            return System.IO.Directory.CreateDirectory(fullPath);
        }
    }

    public static class File
    {
        public static string[] ReadAllLines(string path)
        {
            var fullPath = Path.GameFolderAsRoot(path);
            Debug.Log("Loading: " + fullPath);
            return System.IO.File.ReadAllLines(fullPath);
        }

        public static string[] ReadAllLines(string path, Encoding encoding)
        {
            var fullPath = Path.GameFolderAsRoot(path);
            Debug.Log("Loading: " + fullPath);
            return System.IO.File.ReadAllLines(fullPath, encoding);
        }

        public static bool Exists(string path)
        {
            return System.IO.File.Exists(Path.GameFolderAsRoot(path));
        }

        public static void WriteAllLines(string path, string[] lines)
        {
            System.IO.File.WriteAllLines(Path.GameFolderAsRoot(path), lines);
        }

        public static void Delete(string path)
        {
            Debug.Log("Deleting: " + path);
            System.IO.File.Delete(Path.GameFolderAsRoot(path));
        }

        public static System.IO.FileStream OpenWrite(string path)
        {
            return System.IO.File.OpenWrite(Path.GameFolderAsRoot(path));
        }

        internal static void WriteAllText(string path, string text)
        {
            System.IO.File.WriteAllText(Path.GameFolderAsRoot(path), text);
        }

        internal static string ReadAllText(string path)
        {
            return System.IO.File.ReadAllText(Path.GameFolderAsRoot(path));
        }

        internal static void Copy(string source, string destination, bool overwrite)
        {
            System.IO.File.Copy(Path.GameFolderAsRoot(source), Path.GameFolderAsRoot(destination), overwrite);
        }

        internal static void WriteAllBytes(string path, byte[] bytes)
        {
            System.IO.File.WriteAllBytes(Path.GameFolderAsRoot(path), bytes);
        }
    }
}
