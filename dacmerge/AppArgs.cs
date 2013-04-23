//------------------------------------------------------------------------------
// <copyright file="AppArgs.cs" company="SQLProj.com">
//         Copyright © 2012 SQLProj.com - All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
namespace SqlProj.Utils.Dac.Merge
{
    using CommandLine;

    internal class AppArgs
    {
        [Argument(ArgumentType.AtMostOnce, LongName = "InputFilename", ShortName = "i")]
        public string InputFilename { get; private set; }

        [Argument(ArgumentType.AtMostOnce, LongName = "OutputFilename", ShortName = "o")]
        public string OutputFilename { get; private set; }

        [Argument(ArgumentType.AtMostOnce, LongName = "ReferenceLoadPath", ShortName = "p")]
        public string ReferenceLoadPath { get; private set; }

        [Argument(ArgumentType.AtMostOnce, LongName = "Overwrite", ShortName = "x")]
        public bool Overwrite { get; private set; }

        [Argument(ArgumentType.AtMostOnce, LongName = "Backup", ShortName = "b")]
        public bool Backup { get; private set; }

        [Argument(ArgumentType.AtMostOnce, LongName = "Verbose", ShortName = "v")]
        public bool Verbose { get; private set; }

        [Argument(ArgumentType.AtMostOnce, LongName = "VersionInfo", ShortName = "vi")]
        public bool VersionInfo { get; private set; }

        [Argument(ArgumentType.AtMostOnce, LongName = "Help", ShortName = "?")]
        public bool Help { get; private set; }

        public AppArgs()
        {
            this.InputFilename = null;
            this.OutputFilename = null;
            this.ReferenceLoadPath = null;
            this.Overwrite = false;
            this.Backup = false;
            this.Verbose = false;
            this.VersionInfo = false;
            this.Help = false;
        }
    }
}
