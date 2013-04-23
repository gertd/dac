//------------------------------------------------------------------------------
// <copyright file="Helpers.cs" company="SQLProj.com">
//	 Copyright (c) 2013 SQLProj.com.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
namespace SqlProj.Common
{   
    using System;
    using System.Diagnostics;

    internal static class Helpers
    {
        public static void DisplayVersionInfo()
        {
            string[] arguments = Environment.GetCommandLineArgs();

            if (!string.IsNullOrEmpty(arguments[0]) && System.IO.File.Exists(arguments[0]))
            {
                FileVersionInfo vi = FileVersionInfo.GetVersionInfo(arguments[0]);

                Console.WriteLine("{0} ({1})", vi.Comments, vi.OriginalFilename);

                if (vi.IsDebug)
                    Console.Out.WriteLine("!!!DEBUG VERSION!!!");

                Console.WriteLine(vi.LegalCopyright);
                Console.WriteLine(
                    "Version {0}.{1}.{2}.{3}",
                    vi.FileMajorPart,
                    vi.FileMinorPart,
                    vi.FileBuildPart,
                    vi.FilePrivatePart);
            }
        }
    }
}