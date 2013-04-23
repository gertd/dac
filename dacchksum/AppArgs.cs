//------------------------------------------------------------------------------
// <copyright file="AppArgs.cs" company="SQLProj.com">
//         Copyright © 2013 SQLProj.com - All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
namespace SqlProj.Utils.Dac.Checksum
{
    using CommandLine;

    internal class AppArgs
    {
        [Argument(ArgumentType.AtMostOnce, LongName = "InputFilename", ShortName = "i")]
        public string InputFilename { get; private set; }

        [Argument(ArgumentType.AtMostOnce, LongName = "Update", ShortName = "u")]
        public bool Update { get; private set; }

        [Argument(ArgumentType.AtMostOnce, LongName = "Verbose", ShortName = "v")]
        public bool Verbose { get; private set; }

        [Argument(ArgumentType.AtMostOnce, LongName = "VersionInfo", ShortName = "vi")]
        public bool VersionInfo { get; private set; }

        [Argument(ArgumentType.AtMostOnce, LongName = "Help", ShortName = "?")]
        public bool Help { get; private set; }

        public AppArgs()
        {
            this.InputFilename = null;
            this.Update = false;
            this.Verbose = false;
            this.VersionInfo = false;
            this.Help = false;
        }
    }
}
