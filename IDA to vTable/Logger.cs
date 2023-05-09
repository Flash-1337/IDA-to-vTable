using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IDA_to_vTable
{
    internal class Logger
    {
        public enum LogType
        {
            Info,
            Warning,
            Error
        }

        public static void Log(string message, LogType logType = LogType.Info, bool debug = false)
        {

            // if debug and it is a debug message then log else don't log

#if DEBUG
            // Continue through code and log debug messages
#else
            if (debug) return;
#endif

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"[{DateTime.Now}] : ");

            switch (logType)
            {
                case LogType.Info:
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    break;
                case LogType.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogType.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
            }
            Console.WriteLine(message);

        }
    }
}
