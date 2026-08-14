using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using UnityEngine;

namespace RavenNest.SDK
{
    public class UnityLogger : ILogger
    {
        public void WriteDebug(string message)
        {
            Shinobytes.Debug.Log(message);
        }

        public void WriteError(string message)
        {
            Shinobytes.Debug.LogError(message);
        }

        public void WriteWarning(string message)
        {
            Shinobytes.Debug.LogWarning(message);
        }
        public void Write(string message)
        {
            Shinobytes.Debug.Log(message);
        }

        public void WriteMessage(string message)
        {
            Shinobytes.Debug.Log(message);
        }
    }
}

namespace Shinobytes
{

    public static class Debug
    {
        private static volatile bool patched;
        private static long logCounter = 0;
        private static string PlayerLogFilePath;
        private static string LogFolder;
        private static bool logToFile;
        private static bool isBatchMode;
        private static string TargetLogFilePath;
        private static DateTime lastLogMessage;
        private static DateTime lastWriteToFile;
        private const string CustomLogFile = "ravenfall.log";
        private const string CustomPrevLogFile = "ravenfall-prev.log";
        private static readonly object mutex = new object();

        public static bool KeepPlayerLog = true;
        private static bool patchFailed = false;

        static Debug()
        {
            PatchIfNecessary();
        }

        private static void PatchIfNecessary()
        {
            if (patched) return;
            try
            {


                //var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                //var appDataFolder = System.IO.Path.Combine(userProfile, @"AppData\LocalLow\", Application.companyName, Application.productName);
                var appDataFolder = UnityEngine.Application.persistentDataPath;
                appDataFolder = appDataFolder.Replace("\\/", "/");
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    appDataFolder = appDataFolder.Replace("\\", "/");
                }
                if (!Directory.Exists(appDataFolder))
                {
                    Directory.CreateDirectory(appDataFolder);
                }
                LogFolder = appDataFolder;
                PlayerLogFilePath = Path.Combine(appDataFolder, "player.log");

                logToFile = true;
                //isBatchMode = Application.isBatchMode;
                isBatchMode = false;
                if (logToFile)
                {
                    Application.logMessageReceived += Application_logMessageReceived;
                    lock (mutex)
                    {
                        //Application.logMessageReceivedThreaded += Application_logMessageReceivedThreaded;
                        TargetLogFilePath = Path.Combine(appDataFolder, CustomLogFile);
                        if (Environment.OSVersion.Platform == PlatformID.Unix)
                        {
                            TargetLogFilePath = TargetLogFilePath.Replace("\\", "/");
                        }

                        if (System.IO.File.Exists(TargetLogFilePath))
                        {
                            // copy to a backup file
                            var backupFile = Path.Combine(appDataFolder, CustomPrevLogFile);
                            if (Environment.OSVersion.Platform == PlatformID.Unix)
                            {
                                backupFile = backupFile.Replace("\\", "/");
                            }
                            if (System.IO.File.Exists(backupFile))
                            {
                                System.IO.File.Delete(backupFile);
                            }
                            System.IO.File.Move(TargetLogFilePath, backupFile);
                        }
                        AppendSystemInfo(TargetLogFilePath);
                    }
                }
                else
                {
                    TargetLogFilePath = Path.Combine(appDataFolder, "player.log");
                    if (Environment.OSVersion.Platform == PlatformID.Unix)
                    {
                        TargetLogFilePath = TargetLogFilePath.Replace("\\", "/");
                    }
                }
                patched = true;
            }
            catch
            {
                logToFile = false;
                patchFailed = true;
            }
        }

        public static byte[] GetLogFileContentAsBytes(string logFile)
        {
            try
            {
                var path = Path.Combine(LogFolder, logFile);
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    path = path.Replace("\\", "/");
                }

                if (!System.IO.File.Exists(path))
                    return Array.Empty<byte>();

                lock (mutex)
                {
                    File.Copy(path, path + ".tmp", true);
                }
                var bytes = System.IO.File.ReadAllBytes(path + ".tmp");
                try
                {
                    System.IO.File.Delete(path + ".tmp");
                }
                catch { }
                return bytes;
            }
            catch
            {
                return Array.Empty<byte>();
            }
        }

        public static byte[] GetCurrentLogContentAsBytes()
        {
            try
            {
                lock (mutex)
                {
                    File.Copy(TargetLogFilePath, TargetLogFilePath + ".tmp", true);
                }

                var bytes = System.IO.File.ReadAllBytes(TargetLogFilePath + ".tmp");
                try
                {
                    System.IO.File.Delete(TargetLogFilePath + ".tmp");
                }
                catch { }
                return bytes;
            }
            catch
            {
                return Array.Empty<byte>();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Application_logMessageReceived(string condition, string stackTrace, LogType type)
        {
            LogToFile(condition, stackTrace, type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool LogToFile(string condition, string stackTrace, LogType type)
        {
            if (patchFailed || !logToFile || string.IsNullOrEmpty(TargetLogFilePath) || string.IsNullOrEmpty(condition)) return false;
            if (!Filter(condition, type)) return false;
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                TargetLogFilePath = TargetLogFilePath.Replace("\\", "/");
            }
            lock (mutex)
            {
                var timeSinceLastLog = DateTime.UtcNow - lastLogMessage;
                var timeSinceWrite = DateTime.UtcNow - lastWriteToFile;
                var count = Interlocked.Increment(ref logCounter);
                if (count < 30 || count > 200 || timeSinceLastLog > TimeSpan.FromSeconds(1) || timeSinceWrite > TimeSpan.FromSeconds(1))
                {
                    lastWriteToFile = DateTime.UtcNow;
                    var logFile = new FileInfo(TargetLogFilePath);
                    if (logFile.Length > 1024 * 1024 * 10)
                    {
                        var backupFile = Path.Combine(logFile.DirectoryName, CustomPrevLogFile);
                        if (Environment.OSVersion.Platform == PlatformID.Unix)
                        {
                            backupFile = backupFile.Replace("\\", "/");
                        }
                        if (System.IO.File.Exists(backupFile))
                        {
                            System.IO.File.Delete(backupFile);
                        }
                        System.IO.File.Move(TargetLogFilePath, backupFile);
                        AppendSystemInfo(TargetLogFilePath);
                        File.AppendAllText(TargetLogFilePath, "Log file exceeded 10MB, backed up to " + backupFile + Environment.NewLine);
                    }

                    Interlocked.Exchange(ref logCounter, 0);

                    // delete the player.log file, it should not be used.
                    // if the game crashes, then the player.log will only contain the crash log.
                    // while the ravenfall.log will contain game logs.
                    if (!KeepPlayerLog)
                    {
                        try
                        {
                            if (System.IO.File.Exists(PlayerLogFilePath))
                            {
                                System.IO.File.Delete(PlayerLogFilePath);
                            }
                        }
                        catch
                        {
                            // ignore this as it could be Unity trying to write to the file.
                        }
                    }
                }

                File.AppendAllText(TargetLogFilePath, "[" + type.ToString().PadLeft(9) + "] " + condition + Environment.NewLine + stackTrace);
                return true;
            }
        }

        private static bool Filter(string message, LogType logType)
        {
            if (string.IsNullOrEmpty(message)) return false;
            if (logType == LogType.Warning &&
                ContainsAny(message,
                    "Failed to create agent because it is not close enough to the NavMesh",
                    "Failed to create agent because there is no valid NavMesh"))
                return false;
            return true;
        }

        private static bool ContainsAny(this string input, params string[] values)
        {
            if (string.IsNullOrEmpty(input)) return false;
            if (values == null || values.Length == 0) return false;
            foreach (var value in values)
            {
                if (input.Contains(value, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static void AppendSystemInfo(string logFilePath)
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                logFilePath = logFilePath.Replace("\\", "/");
            }

            lock (mutex)
            {
                var sb = new StringBuilder();

                sb.AppendLine("Unity Version: " + Application.unityVersion);
                sb.AppendLine("Game Version: " + Application.version);

                sb.AppendLine();
                sb.AppendLine("[System]");
                sb.AppendLine("OS: " + SystemInfo.operatingSystem);
                sb.AppendLine("System Memory Size: " + SystemInfo.systemMemorySize);

                sb.AppendLine();
                sb.AppendLine("[Processor]");
                sb.AppendLine("Type: " + SystemInfo.processorType);
                sb.AppendLine("Model: " + SystemInfo.processorModel);
                sb.AppendLine("Count: " + SystemInfo.processorCount);
                sb.AppendLine("Frequency: " + SystemInfo.processorFrequency);

                sb.AppendLine();
                sb.AppendLine("[Graphics Device]");
                sb.AppendLine("Name: " + SystemInfo.graphicsDeviceName);
                sb.AppendLine("Vendor: " + SystemInfo.graphicsDeviceVendor);
                sb.AppendLine("Vendor ID: " + SystemInfo.graphicsDeviceVendorID);
                sb.AppendLine("ID: " + SystemInfo.graphicsDeviceID);
                sb.AppendLine("Type: " + SystemInfo.graphicsDeviceType);
                sb.AppendLine("Version: " + SystemInfo.graphicsDeviceVersion);
                sb.AppendLine("Memory Size: " + SystemInfo.graphicsMemorySize);
                sb.AppendLine("Multi Threaded: " + SystemInfo.graphicsMultiThreaded);
                sb.AppendLine("Shader Level: " + SystemInfo.graphicsShaderLevel);
                sb.AppendLine("UV Starts At Top: " + SystemInfo.graphicsUVStartsAtTop);

                sb.AppendLine();
                sb.AppendLine("[Device]");
                sb.AppendLine("Model: " + SystemInfo.deviceModel);
                sb.AppendLine("Name: " + SystemInfo.deviceName);
                sb.AppendLine("Type: " + SystemInfo.deviceType);
                sb.AppendLine("Unique Identifier: " + SystemInfo.deviceUniqueIdentifier);
                sb.AppendLine();
                File.AppendAllText(logFilePath, sb.ToString());
            }
        }


        public static void Log(string message)
        {
            try
            {
                PatchIfNecessary();
                var msg = GetMessage(message);
                UnityEngine.Debug.Log(msg);
                if (isBatchMode) Console.WriteLine(Prefix(LogType.Log) + msg);
                lastLogMessage = DateTime.UtcNow;
            }
            catch { }
        }

        public static void Log(object message)
        {
            try
            {
                PatchIfNecessary();
                var msg = GetMessage(message?.ToString());
                UnityEngine.Debug.Log(msg);
                if (isBatchMode) Console.WriteLine(Prefix(LogType.Log) + msg);
                lastLogMessage = DateTime.UtcNow;
            }
            catch { }
        }

        public static void LogWarning(string message)
        {
            try
            {
                PatchIfNecessary();
                var msg = GetMessage(message);
                UnityEngine.Debug.LogWarning(msg);
                if (isBatchMode) Console.WriteLine(Prefix(LogType.Warning) + msg);
                lastLogMessage = DateTime.UtcNow;
            }
            catch { }
        }

        public static void LogError(string message)
        {
            try
            {
                PatchIfNecessary();
                var msg = GetMessage(message);
                UnityEngine.Debug.LogError(msg);
                if (isBatchMode) Console.WriteLine(Prefix(LogType.Error) + msg);
                lastLogMessage = DateTime.UtcNow;
            }
            catch { }
        }

        private static string Prefix(LogType logType)
        {
            return "[" + logType.ToString().PadLeft(9) + "] ";
        }

        private static string GetMessage(string message)
        {
            var msg = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + message;
            // try get callstack if debug

#if DEBUG
            var stackTrace = Environment.StackTrace;
            if (!string.IsNullOrEmpty(stackTrace))
            {
                // first line will have: at System.Environment.get_StackTrace () [0x00000] 
                // second message is this method (GetMessage)
                stackTrace = string.Join(Environment.NewLine, stackTrace.Split(Environment.NewLine)[2..]);
                return msg + Environment.NewLine + stackTrace;
            }
#endif

            return msg;
        }
    }
}
