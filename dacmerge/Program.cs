//------------------------------------------------------------------------------
// <copyright file="Program.cs" company="SQLProj.com">
//         Copyright © 2013 SQLProj.com - All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
namespace SqlProj.Utils.Dac.Merge
{
    using System;
    using System.Diagnostics;
    using CommandLine;
    using SqlProj.Common;

    /// <summary>
    /// Entrypoint class
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Entrypoint method
        /// </summary>
        /// <param name="args">command line arguments</param>
        internal static void Main(string[] args)
        {
            Environment.ExitCode = ExitCode.NothingSucceeded.AsInt();

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler
                ((sender, o) =>
                    {
                        LogWriter.WriteError("Unhandled exception occured");
                        LogWriter.WriteError(o.ExceptionObject as Exception);
                    }
                );
                
            try
            {
                var parsedArgs = new AppArgs();
                if (Parser.ParseArguments(args, parsedArgs))
                {
                    if (parsedArgs.VersionInfo || parsedArgs.Help)
                    {
                        if (parsedArgs.VersionInfo)
                            Helpers.DisplayVersionInfo();
 
                        if (parsedArgs.Help)
                            Console.Out.Write(Parser.ArgumentsUsage(typeof(AppArgs)));

                        return;
                    }
                    
                    LogWriter.LoggingLevel = (parsedArgs.Verbose ? LogWriter.LogLevel.Verbose : LogWriter.LogLevel.Normal);

                    var app = new DacMerge(parsedArgs);
                    app.Run();

                    Environment.ExitCode = ExitCode.Success.AsInt();
                }
                else
                {
                    Console.Out.Write(Parser.ArgumentsUsage(typeof(AppArgs)));
                    Environment.ExitCode = ExitCode.UnrecognizedCommand.AsInt();
                }
            }
            catch (System.ArgumentException ex)
            {
                Console.Error.WriteLine(DacMergeResource.ErrorInvalidParameter, ex.Message);
                Environment.ExitCode = ExitCode.PartialSuccess.AsInt();
            }
            catch (System.IO.DirectoryNotFoundException ex)
            {
                Console.Error.WriteLine(DacMergeResource.ErrorDirNotFound, ex.Message);
                Environment.ExitCode = ExitCode.PartialSuccess.AsInt();
            }
            catch (System.IO.FileNotFoundException ex)
            {
                Console.Error.WriteLine(DacMergeResource.ErrorFileNotFound, ex.Message);
                Environment.ExitCode = ExitCode.PartialSuccess.AsInt();
            }
            catch (System.ApplicationException ex)
            {
                LogWriter.WriteError(ex);
                Environment.ExitCode = ExitCode.NothingSucceeded.AsInt();
            }
            catch (System.Exception ex)
            {
                LogWriter.WriteError(ex);
                Environment.ExitCode = ExitCode.NothingSucceeded.AsInt();
            }

            return;
        }
    }
}
