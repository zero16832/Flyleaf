﻿using System;
using System.IO;
using System.Text;

namespace FlyleafLib
{
    public static class Logger
    {
        public static bool      CanError        => Engine.Config.LogLevel >= LogLevel.Error;
        public static bool      CanWarn         => Engine.Config.LogLevel >= LogLevel.Warn;
        public static bool      CanInfo         => Engine.Config.LogLevel >= LogLevel.Info;
        public static bool      CanDebug        => Engine.Config.LogLevel >= LogLevel.Debug;
        public static bool      CanTrace        => Engine.Config.LogLevel >= LogLevel.Trace;

        internal static Action<string>
                                Output          = DevNullPtr;
        static string           lastOutput      = "";

        static FileStream       fileStream;
        static object           lockFileStream  = new object();

        internal static void SetOutput()
        {
            var output = Engine.Config.LogOutput;

            if (string.IsNullOrEmpty(output))
            {
                if (lastOutput != "")
                {
                    Output = DevNullPtr;
                    lastOutput = "";
                }
            }
            else if (output.StartsWith(":"))
            {
                if (output == ":console")
                {
                    if (lastOutput != ":console")
                    {
                        Output = Console.WriteLine;
                        lastOutput = ":console";
                    }
                }
                else if (output == ":debug")
                {
                    if (lastOutput != ":debug")
                    {
                        Output = DebugPtr;
                        lastOutput = ":debug";
                    }
                }
                else
                    throw new Exception("Invalid log output");
            }
            else
            {
                lock (lockFileStream)
                {
                    fileStream?.Dispose();
                    string dir = Path.GetDirectoryName(output);
                    if (string.IsNullOrEmpty(dir))
                        throw new Exception("Invalid path");

                    Directory.CreateDirectory(dir);
                    fileStream = new FileStream(output, FileMode.Append, FileAccess.Write);
                    if (lastOutput != ":file")
                    {
                        Output = FilePtr;
                        lastOutput = ":file";
                    }
                }
            }
        }
        static void DebugPtr(string msg) => System.Diagnostics.Debug.WriteLine(msg);
        static void DevNullPtr(string msg) { }
        static void FilePtr(string msg)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes($"{msg}\r\n");

                lock (lockFileStream)
                {
                    fileStream.Write(data, 0, data.Length);
                    fileStream.Flush();
                }
            } catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("[File Log Error] " + e.Message);
                Output = Console.WriteLine;
            }
        }

        
        internal static void Log(string msg, LogLevel logLevel)
        { 
            if (logLevel <= Engine.Config.LogLevel)
                Output($"{DateTime.Now.ToString("hh.mm.ss.fff")} | {logLevel.ToString().PadRight(5, ' ')} | {msg}");
        }
    }

    public class LogHandler
    {
        string prefix;
        public LogHandler(string prefix = "")
        {
            this.prefix = prefix;
        }
        public void Error(string msg) => Logger.Log($"{prefix}{msg}", LogLevel.Error);
        public void Info (string msg) => Logger.Log($"{prefix}{msg}", LogLevel.Info);
        public void Warn (string msg) => Logger.Log($"{prefix}{msg}", LogLevel.Warn);
        public void Debug(string msg) => Logger.Log($"{prefix}{msg}", LogLevel.Debug);
        public void Trace(string msg) => Logger.Log($"{prefix}{msg}", LogLevel.Trace);
    }

    public enum LogLevel
    {
        Quiet = 0x00,
        Error = 0x10,
        Warn  = 0x20,
        Info  = 0x30,
        Debug = 0x40,
        Trace = 0x50
    }
}