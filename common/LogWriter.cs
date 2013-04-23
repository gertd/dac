//------------------------------------------------------------------------------
// <copyright file="Helpers.cs" company="SQLProj.com">
//	 Copyright (c) 2013 SQLProj.com.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
namespace SqlProj.Common
{   
    using System;
    using System.Diagnostics;

    internal sealed class LogWriter
    {
        private static volatile LogWriter _logWriter;
        private static object _lockObject = new object();
        private static LogLevel _logLevel = LogLevel.Normal;

        public enum LogLevel : int
        {
            None = 0,
            Normal = 1,
            Verbose = 2
        };

        public static LogLevel LoggingLevel 
        {
            get { return _logLevel; }
            set {_logLevel = value; }
        }

        private LogWriter()
        {
        }

        public static LogWriter Instance()
        {
            if (_logWriter == null)
            {
                lock (_lockObject)
                {
                    if (_logWriter == null)
                        _logWriter = new LogWriter();
                }
            }
            return _logWriter;
        }

        public static void WriteMessage(string message)
        {
            if (LoggingLevel >= LogLevel.Normal)
            {
                Console.Out.WriteLine("Info: [{0:yyyy-MM-dd HH:mm:ss}] [{1}]",
                    DateTime.Now,
                    message);
            }
        }

        public static void WriteWarning(string message)
        {
            if (LoggingLevel >= LogLevel.Normal)
            {
                Console.Out.WriteLine("Warn: [{0:yyyy-MM-dd HH:mm:ss}] [{1}]",
                    DateTime.Now,
                    message);
            }
        }

        public static void WriteError(string message)
        {
            Console.Error.WriteLine("Err:  [{0:yyyy-MM-dd HH:mm:ss}] [{1}]",
                DateTime.Now,
                message);
        }

        public static void WriteError(Exception exception)
        {
            do
            {
                System.Console.Error.WriteLine("Err:  [{0:yyyy-MM-dd HH:mm:ss}] [{1}]",
                    DateTime.Now,
                    exception.Message);

                exception = exception.InnerException;
            }
            while (null != exception);
        }
    }
}