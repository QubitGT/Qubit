using System;

namespace Qubit.Libraries.DiscordRPC.Logging
{



    public class ConsoleLogger : ILogger
    {



        public LogLevel Level { get; set; }




        public bool Coloured { get; set; }




        public bool Colored
        {
            get => Coloured;
            set => Coloured = value;
        }




        public ConsoleLogger()
        {
            Level = LogLevel.Info;
            Coloured = false;
        }





        public ConsoleLogger(LogLevel level)
            : this()
        {
            Level = level;
        }






        public ConsoleLogger(LogLevel level, bool coloured)
        {
            Level = level;
            Coloured = coloured;
        }






        public void Trace(string message, params object[] args)
        {
            if (Level > LogLevel.Trace) return;

            if (Coloured) Console.ForegroundColor = ConsoleColor.Gray;

            string prefixedMessage = "TRACE: " + message;

            if (args.Length > 0)
            {
                Console.WriteLine(prefixedMessage, args);
            }
            else
            {
                Console.WriteLine(prefixedMessage);
            }
        }






        public void Info(string message, params object[] args)
        {
            if (Level > LogLevel.Info) return;

            if (Coloured) Console.ForegroundColor = ConsoleColor.White;

            string prefixedMessage = "INFO: " + message;

            if (args.Length > 0)
            {
                Console.WriteLine(prefixedMessage, args);
            }
            else
            {
                Console.WriteLine(prefixedMessage);
            }
        }






        public void Warning(string message, params object[] args)
        {
            if (Level > LogLevel.Warning) return;

            if (Coloured) Console.ForegroundColor = ConsoleColor.Yellow;

            string prefixedMessage = "WARN: " + message;

            if (args.Length > 0)
            {
                Console.WriteLine(prefixedMessage, args);
            }
            else
            {
                Console.WriteLine(prefixedMessage);
            }
        }






        public void Error(string message, params object[] args)
        {
            if (Level > LogLevel.Error) return;

            if (Coloured) Console.ForegroundColor = ConsoleColor.Red;

            string prefixedMessage = "ERR : " + message;

            if (args.Length > 0)
            {
                Console.WriteLine(prefixedMessage, args);
            }
            else
            {
                Console.WriteLine(prefixedMessage);
            }
        }

    }
}
