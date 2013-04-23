//------------------------------------------------------------------------------
// <copyright file="ExitCode.cs" company="SQLProj.com">
//	 Copyright (c) 2013 SQLProj.com.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
namespace SqlProj.Common
{
    public enum ExitCode
    {
        Success = 0,
        PartialSuccess = 1,
        UnrecognizedCommand = 2,
        NothingSucceeded = 100
    }

    public static class ExitCodeExtensions
    {
        public static int AsInt(this ExitCode value)
        {
            return (int) value;
        }
    }
}