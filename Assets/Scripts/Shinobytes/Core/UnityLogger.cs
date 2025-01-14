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
        private static bool logToFile;
        private static bool isBatchMode;
        private static string TargetLogFilePath;
        private static DateTime lastLogMessage;
        private static DateTime lastWriteToFile;
        private const string CustomLogFile = "ravenfall.log";
        private const string CustomPrevLogFile = "ravenfall-prev.log";
        private static readonly object mutex = new object();

        public static bool KeepPlayerLog = true;

        static Debug()
        {
            PatchIfNecessary();
        }

        private static void PatchIfNecessary()
        {
            if (patched) return;

            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var appDataFolder = System.IO.Path.Combine(userProfile, @"AppData\LocalLow\", Application.companyName, Application.productName);

            //logToFile = Application.unityVersion.Contains("6000.0.16f1");
            //var prevLog = Path.Combine(appDataFolder, "player-prev.log");
            //var prevLogExists = System.IO.File.Exists(prevLog);
            //if (prevLogExists && new System.IO.FileInfo(prevLog).Length == 0)
            //{
            //    logToFile = true;
            //}

            // always log to file for now

            PlayerLogFilePath = Path.Combine(appDataFolder, "player.log");

            logToFile = true;
            isBatchMode = Application.isBatchMode;
            if (logToFile)
            {
                Application.logMessageReceived += Application_logMessageReceived;
                lock (mutex)
                {
                    //Application.logMessageReceivedThreaded += Application_logMessageReceivedThreaded;
                    TargetLogFilePath = Path.Combine(appDataFolder, CustomLogFile);
                    if (System.IO.File.Exists(TargetLogFilePath))
                    {
                        // copy to a backup file
                        var backupFile = Path.Combine(appDataFolder, CustomPrevLogFile);
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
            }
            patched = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Application_logMessageReceived(string condition, string stackTrace, LogType type)
        {
            LogToFile(condition, stackTrace, type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool LogToFile(string condition, string stackTrace, LogType type)
        {
            if (!logToFile || string.IsNullOrEmpty(TargetLogFilePath) || string.IsNullOrEmpty(condition)) return false;
            if (!Filter(condition, type)) return false;

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
            PatchIfNecessary();
            var msg = GetMessage(message);
            UnityEngine.Debug.Log(msg);
            if (isBatchMode) Console.WriteLine(Prefix(LogType.Log) + msg);
            lastLogMessage = DateTime.UtcNow;
        }

        public static void Log(object message)
        {
            PatchIfNecessary();
            var msg = GetMessage(message?.ToString());
            UnityEngine.Debug.Log(msg);
            if (isBatchMode) Console.WriteLine(Prefix(LogType.Log) + msg);
            lastLogMessage = DateTime.UtcNow;
        }

        public static void LogWarning(string message)
        {
            PatchIfNecessary();
            var msg = GetMessage(message);
            UnityEngine.Debug.LogWarning(msg);
            if (isBatchMode) Console.WriteLine(Prefix(LogType.Warning) + msg);
            lastLogMessage = DateTime.UtcNow;
        }

        public static void LogError(string message)
        {
            PatchIfNecessary();
            var msg = GetMessage(message);
            UnityEngine.Debug.LogError(msg);
            if (isBatchMode) Console.WriteLine(Prefix(LogType.Error) + msg);
            lastLogMessage = DateTime.UtcNow;
        }

        private static string Prefix(LogType logType)
        {
            return "[" + logType.ToString().PadLeft(9) + "] ";
        }

        private static string GetMessage(string message)
        {
            return "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + message;
        }
    }
}
