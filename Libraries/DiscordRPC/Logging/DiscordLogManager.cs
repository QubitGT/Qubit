using Qubit.Libraries.Core;
using System;

namespace Qubit.Libraries.DiscordRPC.Logging
{
    public class DiscordLogManager : ILogger
    {
        public LogLevel Level { get; set; }

        public bool Coloured { get; set; }

        [Obsolete("Use Coloured")]
        public bool Colored
        {
            get
            {
                return Coloured;
            }
            set
            {
                Coloured = value;
            }
        }

        public DiscordLogManager()
        {
            Level = LogLevel.Info;
            Coloured = false;
        }

        public DiscordLogManager(LogLevel level) : this()
        {
            Level = level;
        }

        public DiscordLogManager(LogLevel level, bool coloured)
        {
            Level = level;
            Coloured = coloured;
        }

        public void Trace(string message, params object[] args)
        {
            if (Level > LogLevel.Trace)
            {
                return;
            }
            if (Coloured)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            string text = "TRACE: " + message;
            if (args.Length != 0)
            {
                Logs.Log(text, args);
                return;
            }
            Logs.Log(text);
        }

        public void Info(string message, params object[] args)
        {
            if (Level > LogLevel.Info)
            {
                return;
            }
            string text = "INFO: " + message;
            if (args.Length != 0)
            {
                Logs.Log(text, args);
                return;
            }
            Logs.Log(text);
        }

        public void Warning(string message, params object[] args)
        {
            if (Level > LogLevel.Warning)
            {
                return;
            }
            string text = "WARN: " + message;
            if (args.Length != 0)
            {
                Logs.LogWarning(text, args);
                return;
            }
            Logs.LogWarning(text);
        }

        public void Error(string message, params object[] args)
        {
            if (Level > LogLevel.Error)
            {
                return;
            }
            string text = "ERR : " + message;
            if (args.Length != 0)
            {
                Logs.LogError(text, args);
                return;
            }
            Logs.LogError(text);
        }
    }
}
