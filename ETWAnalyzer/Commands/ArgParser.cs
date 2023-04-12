//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer;
using ETWAnalyzer.Commands;
using ETWAnalyzer.Extract;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

abstract class ArgParser : ICommand
{
    public abstract void Parse();
    public abstract void Run();


    /// <summary>
    /// Input arguments for command line parser
    /// </summary>
    internal const string FileOrDirectoryArg = "-filedir";
    internal const string FileOrDirectoryAlias = "-fd";
    internal const string ExtractArg = "-extract";
    internal const string SymFolderArg = "-symfolder";
    internal const string SymbolServerArg = "-symserver";
    internal const string SymCacheFolderArg = "-symcache";
    internal const string DumpArg = "-dump";
    internal const string RecursiveArg = "-recursive";
    internal const string DebugArg = "-debug";
    internal const string HelpArg = "-help";
    internal const string NoColorArg = "-nocolor";
    internal const string OutDirArg = "-outdir";
    internal const string PidArg = "-pid";
    internal const string PerThreadArg = "-perthread";
    internal const string AllExceptionsArgs = "-allexceptions";
    internal const string UnzipOperationArg = "-unzipoperation";


    internal const string SkipNTestsArg = "-skipntests";
    internal const string TestRunIndexArg = "-testrunindex";
    internal const string TRIArg = "-tri";
    internal const string TestRunCountArg = "-testruncount";
    internal const string TRCArg = "-trc";
    internal const string TestsPerRunArg = "-testsperrun";
    internal const string LastNDaysArg = "-lastndays";

    /// <summary>
    /// Supported file extensions
    /// </summary>
    internal const string EtlExtension = ".etl";
    internal const string ZipExtension = ".zip";
    internal const string SevenZipExtension = ".7z";
    internal const string JsonExtension = ".json";
    /// <summary>
    /// Command line arguments are stored in this input queue during the parse operation
    /// </summary>
    protected Queue<string> myInputArguments;
    /// <summary>
    /// To support process wise parallelization we keep all args so we can create multiple sub processes
    /// </summary>
    protected  string[] myOriginalInputArguments;

    /// <summary>
    /// Used symbol server to lookup method names
    /// </summary>
    public enum SymbolServers
    {
        None,

        /// <summary>
        /// Get remote symbol server from current environment and use the remote servers from there
        /// </summary>
        NtSymbolPath,

        /// <summary>
        /// The actual value is configured in App.Config file Settings.SymbolServerMS
        /// </summary>
        MS,

        /// <summary>
        /// The actual value is configured in App.Config file Settings.SymbolServerGoogle
        /// </summary>
        Google,

        /// <summary>
        /// The actual value is configured in App.Config file Settings.SymbolServerSyngo
        /// </summary>
        Syngo
    }

    /// <summary>
    /// Needed to resolve method names from stack traces from ETL files. We unzip our file usually deep
    /// into the file system. That leads to issues when the full path to the pdb file is longer than 250 characters.
    /// To work around that we create during unzipping the ETL file a symbolic link to a directory which the ETL
    /// file name which is hopefully short enough.
    /// </summary>
    internal SymbolPaths Symbols = new()
    {
        SymbolFolder = Settings.Default.SymbolDownloadFolder,
    };


    /// <summary>
    /// Help string for current command which can be context sensitive
    /// </summary>
    public abstract string Help
    {
         get;
    }

    /// <summary>
    /// When set this return code will be returned by main application.
    /// </summary>
    protected int? myReturnCode = null;

    public int? ReturnCode => myReturnCode;

    /// <summary>
    /// Create an argument parser of given command line
    /// </summary>
    /// <param name="args"></param>
    protected ArgParser(string[] args)
    {
        myOriginalInputArguments = args;
        myInputArguments = new Queue<string>(args);
    }


    /// <summary>
    /// Get next command line argument or null if no more are there but the queue state remains.
    /// </summary>
    /// <param name="result">Next argument or null if there is none.</param>
    /// <returns>true if next argument is a switch, false otherwise</returns>        
    protected internal bool PeekNextArgumentSwitch(out string result)
    {
        result = myInputArguments.Count > 0 ? myInputArguments.Peek() : null;

        // negative numbers are not treated as command line switches
        if (result == null || (result.StartsWith("-", StringComparison.InvariantCulture) && result.Length > 1 && !result.Skip(1).All(IsNumberChar) ) )
        {
            return true;
        }
        else
            return false;
    }

    /// <summary>
    /// Get argument list for current command line parameter.
    /// </summary>
    /// <param name="parent">current argument</param>
    /// <param name="bThrow">When true throw on empty list</param>
    /// <returns>List of parsed arguments</returns>
    /// <remarks>Throws InvalidDataExcption if no arguments were found.</remarks>
    protected List<string> GetArgList(string parent, bool bThrow = true)
    {
        List<string> args = new();
        string nextArg = null;
        while ((nextArg = GetNextNonArg(parent, false)) != null)
        {
            args.Add(nextArg);
        }

        if (args.Count == 0)
        {
            if (bThrow)
            {
                throw new InvalidDataException($"Input argument {parent} needs at least one option.");
            }
        }
        return args;
    }


    /// <summary>
    /// return next command line argument without - as prefix like -filename which would indicate a different command line argument
    /// </summary>
    /// <returns></returns>
    protected string GetNextNonArg(string parentArg, bool throwOnNonArg = true)
    {
        bool isSwitch = PeekNextArgumentSwitch(out string next);

        if (isSwitch)
        {
            if (throwOnNonArg)
            {
                throw new InvalidOperationException($"Command {parentArg} needs an argument. Got instead {next}");
            }
            else
            {
                next = null; // remove invalid argument. Return null as non arg.
            }
        }
        else
        {
            if (myInputArguments.Count == 0)
            {
                if (throwOnNonArg)
                {
                    throw new InvalidOperationException($"Command {parentArg} needs an argument.");
                }
                else
                {
                    return null;
                }
            }

            myInputArguments.Dequeue(); // remove parsed argument from input queue
        }

        return next;
    }

    /// <summary>
    /// Checks if a the Input/Output File or Directory exists. Compares the extension of the Input/Output-File with the expected Filetype (Extension: .etl, .7z, .zip, json).
    /// The expected Filetyps are defined by the current Argument.
    /// </summary>
    /// <param name="path">Input/ Output Filepath or Folder</param>
    /// <param name="ext">Expected extensions (.etl , .7z, .zip, .json)</param>
    /// <param name="ext2"></param>
    /// <param name="ext3"></param>
    /// <param name="ext4"></param>
    /// <returns></returns>
    static internal string CheckIfFileOrDirectoryExistsAndExtension(string path, string ext = "", string ext2 = "", string ext3 = "", string ext4 = "")
    {
        if (Directory.Exists(path))
        {
            return path;
        }
        if (File.Exists(path) && path.Length > 1) //Checks if File or Folder exists 
        {
            string tempExt = Path.GetExtension(path);

            if (tempExt == ext || tempExt == ext2 || tempExt == ext3 || tempExt == ext4 || tempExt.Length == 0) // Checks if Extension matches. The Extension of the Folder returns "":
            {
                return path;
            }
            throw new FormatException($"File {path} has wrong extension. Expected: {ext} { ext2 } {ext3}");
        }
        throw new FileNotFoundException($"File or Directory {path} does not exist");
    }

    /// <summary>
    /// Wrap enum parsing in a delegate and throw a more descriptive error message along with the invalid input value.
    /// </summary>
    /// <typeparam name="TEnum">Enum Type</typeparam>
    /// <param name="context">Context message which is formatted into the exception text.</param>
    /// <param name="input">Used input to parse enum</param>
    /// <param name="acc">Delegate which does call Enum.Parse</param>
    /// <param name="ignoredValues">Some values of the range of possible values can be filtered out e.g. Default or Invalid which are not valid flags.</param>
    /// <exception cref="ArgumentException">Throws a wrapped exception with a much more descriptive error message.</exception>
    internal static void ParseEnum<TEnum>(string context, string input, Action acc, params TEnum[] ignoredValues) where TEnum : Enum
    {
        try
        {
            acc();
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException($"Valid {context} values are {GetEnumValues<TEnum>(ignoredValues)}. Input value was {input ?? "none"}. ", ex);
        }
    }

    internal static TEnum ParseEnum<TEnum>(string context, string input, params TEnum[] allowedValues) where TEnum:Enum
    {
        try
        {
            TEnum value = (TEnum) Enum.Parse(typeof(TEnum), input, true);
            return value;
        }
        catch(ArgumentException ex)
        {
            throw new ArgumentException($"Valid {context} values are {String.Join(",", allowedValues.Select(x=>x.ToString()).ToArray())}. Input value was: {input ?? "none"}.", ex);
        }
    }

    /// <summary>
    /// Get filtered list of enum values as string
    /// </summary>
    /// <typeparam name="TEnum">Enum type</typeparam>
    /// <param name="ignored">Enum values which should be exempt from output.</param>
    /// <returns>String with the symbolic name of all enums separated by space.</returns>
    internal static string GetEnumValues<TEnum>(TEnum[] ignored) where TEnum : Enum
    {
        TEnum[] values = ((TEnum[])Enum.GetValues(typeof(TEnum))).Where(x => !(ignored ?? Array.Empty<TEnum>()).Contains(x)).Distinct().ToArray();
        return String.Join(" ", values);
    }


    /// <summary>
    /// Parse a number in local or invariant culture to get around decimal and thousand separator issues.
    /// If a 1.001 is parsed in German it would lead to 1001 and not 1.001.
    /// </summary>
    /// <param name="number">Input number string e.g. 1.5 or 1,5 </param>
    /// <returns>Parsed string</returns>
    internal static double ParseDouble(string number)
    {
        NumberStyles style = NumberStyles.Float;

        // first try in local culture
        if ( !double.TryParse(number, style, Thread.CurrentThread.CurrentCulture, out double lret) )
        {
            // and then in invariant culture
            if( !double.TryParse(number, style, CultureInfo.InvariantCulture, out lret) )
            {
                throw new InvalidDataException($"Could not parse string {number} to a floating point number.");
            }
        }

        return lret;
    }

    /// <summary>
    /// Return true if char is number or current culture decimal separator or number group separator
    /// </summary>
    /// <param name="digit"></param>
    /// <returns></returns>
    internal static bool IsNumberChar(char digit)
    {
        return Char.IsDigit(digit) || CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator.Contains(digit) || CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator.Contains(digit);
    }
}
