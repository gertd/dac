//------------------------------------------------------------------------------
// <copyright file="DacChkSum.cs" company="SQLProj.com">
//         Copyright © 2012 SQLProj.com - All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace SqlProj.Utils.Dac.Checksum
{
    using System;
    using System.Globalization;
    using System.IO;
    using SqlProj.Dac;

    internal class DacChkSum
    {
        private readonly AppArgs _args;

        public DacChkSum(AppArgs args)
        {
            this._args = args;
        }

        public void Run()
        {
            // check arguments
            if (string.IsNullOrEmpty(_args.InputFilename))
            {
                throw new ApplicationException(DacChkSumResource.ErrorNoInputFile);
            }

            var inputFile = new FileInfo(_args.InputFilename);
            if (!inputFile.Exists)
            {
                throw new FileNotFoundException(_args.InputFilename);
            }

            if (!DacPackage.IsDacPac(inputFile))
            {
                throw new FileFormatException(DacChkSumResource.ErrorNotValidDacPac);
            }

            if (_args.Verbose)
            {
                Console.Out.WriteLine("Starting:  [{0}]", DateTime.Now);
                Console.Out.WriteLine("Input:     [{0}]", inputFile.FullName);
                Console.Out.WriteLine("Update:    [{0}]", this._args.Update.ToString(CultureInfo.InvariantCulture));
            }

            using (var package = new DacPackage(inputFile))
            {
                package.Open();

                byte[] oldChkSum = package.ReadChecksum();
                byte[] newChkSum = package.CalculateChecksum();

                Console.Out.WriteLine("Model checksum");
                Console.Out.WriteLine("Stored:   :[{0}]", DacPackage.ByteArrayToString(oldChkSum));
                Console.Out.WriteLine("Calculated:[{0}]", DacPackage.ByteArrayToString(newChkSum));

                if ((_args.Update) && (!System.Linq.Enumerable.SequenceEqual(oldChkSum, newChkSum)))
                {
                    Console.Out.WriteLine("Updating checksum");
                    package.UpdateChecksum(newChkSum);
                    package.Save();
                }

                package.Close();
            }

            if (_args.Verbose)
            {
                Console.Out.WriteLine("Finished:  [{0}]", DateTime.Now);
            }
        }
    }
}
