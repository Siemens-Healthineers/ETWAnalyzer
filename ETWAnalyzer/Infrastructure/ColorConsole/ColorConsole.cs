using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ETWAnalyzer.ProcessTools
{

    /* Examples 
            Console.WriteLine("\nUsing a splash of color in your Console code more easily... (plain text)\n");
            ColorConsole.WriteLine("Color me this - in Red", ConsoleColor.Red);
            ColorConsole.WriteWrappedHeader("Off with their green Heads!", headerColor: ConsoleColor.Green);
            ColorConsole.WriteWarning("\nWorking...\n");
            Console.WriteLine("Writing some mixed colors: (plain text)");
            ColorConsole.WriteEmbeddedColorLine(
                "Launch the site with [darkcyan]https://localhost:5200[/darkcyan] and press [yellow]Ctrl-c[/yellow] to exit.\n");
            ColorConsole.WriteSuccess("The operation completed successfully.");

    */


    /// <summary>
    /// https://weblog.west-wind.com/posts/2020/Jul/10/A-NET-Console-Color-Helper
    /// Console Color Helper class that provides coloring to individual commands
    /// </summary>
    public static class ColorConsole
    {
        /// <summary>
        /// For guys who do not like the color scheme make it possible to turn it off
        /// </summary>
        public static bool EnableColor
        {
            get; set;
        } = true;

        /// <summary>
        /// When true clip output to console buffer width to prevent newlines for wider output
        /// </summary>
        public static bool ClipToConsoleWidth
        {
            get;
            set;
        } = false;

        /// <summary>
        /// Assume constant console width during output printing
        /// </summary>
        static readonly int myConsoleWidth;

        /// <summary>
        /// Default color 
        /// </summary>
        static readonly System.ConsoleColor InitialForeColor = System.Console.ForegroundColor;

        /// <summary>
        /// Console Tab Width is 8
        /// </summary>
        const int ConsoleTabWidth = 8;


        static ColorConsole()
        {
            CtrlCHandler.Instance.Register(Console_CancelKeyPress);
            myConsoleWidth = int.MaxValue;
            try
            {
                myConsoleWidth = Console.BufferWidth;
            }
            catch(IOException) // when output is redirected we get an invalid handle exception
            { }
            
        }

        private static void Console_CancelKeyPress()
        {
            EnableColor = false; // other threads might still be printing after we have reset the console color
            Console.ForegroundColor = InitialForeColor;
        }

        /// <summary>
        /// WriteLine with color
        /// </summary>
        /// <param name="text"></param>
        /// <param name="color"></param>
        public static void WriteLine(string text, ConsoleColor? color = null)
        {
            if (color.HasValue && EnableColor)
            {
                var oldColor = System.Console.ForegroundColor;
                if (color == oldColor)
                {
                    Console.WriteLine(ClipToConsole(text));
                }
                else
                {
                    Console.ForegroundColor = color.Value;
                    Console.WriteLine(ClipToConsole(text));
                    Console.ForegroundColor = oldColor;
                }
            }
            else
                Console.WriteLine(ClipToConsole(text));
        }

        /// <summary>
        /// Writes out a line with a specific color as a string
        /// </summary>
        /// <param name="text">Text to write</param>
        /// <param name="color">A console color. Must match ConsoleColors collection names (case insensitive)</param>
        public static void WriteLine(string text, string color)
        {
            if (string.IsNullOrEmpty(color) || !EnableColor)
            {
                WriteLine(text);
                return;
            }

            if (!Enum.TryParse(color, true, out ConsoleColor col))
            {
                WriteLine(text);
            }
            else
            {
                WriteLine(text, col);
            }
        }

        /// <summary>
        /// Write with color
        /// </summary>
        /// <param name="text"></param>
        /// <param name="color"></param>
        public static void Write(string text, ConsoleColor? color = null)
        {
            if (color.HasValue && EnableColor)
            {
                var oldColor = System.Console.ForegroundColor;
                if (color == oldColor)
                {
                    Console.Write(ClipToConsole(text));
                }
                else
                {
                    Console.ForegroundColor = color.Value;
                    Console.Write(ClipToConsole(text));
                    Console.ForegroundColor = oldColor;
                }
            }
            else
            {
                Console.Write(ClipToConsole(text));
            }
        }

        /// <summary>
        /// Writes out a line with color specified as a string
        /// </summary>
        /// <param name="text">Text to write</param>
        /// <param name="color">A console color. Must match ConsoleColors collection names (case insensitive)</param>
        public static void Write(string text, string color)
        {
            if (string.IsNullOrEmpty(color) && !EnableColor)
            {
                Write(text);
                return;
            }

            if (!ConsoleColor.TryParse(color, true, out ConsoleColor col))
            {
                Write(text);
            }
            else
            {
                Write(text, col);
            }
        }

        #region Wrappers and Templates


        /// <summary>
        /// Writes a line of header text wrapped in a in a pair of lines of dashes:
        /// -----------
        /// Header Text
        /// -----------
        /// and allows you to specify a color for the header. The dashes are colored
        /// </summary>
        /// <param name="headerText">Header text to display</param>
        /// <param name="wrapperChar">wrapper character (-)</param>
        /// <param name="headerColor">Color for header text (yellow)</param>
        /// <param name="dashColor">Color for dashes (gray)</param>
        public static void WriteWrappedHeader(string headerText,
                                                char wrapperChar = '-',
                                                ConsoleColor headerColor = ConsoleColor.Yellow,
                                                ConsoleColor dashColor = ConsoleColor.DarkGray)
        {
            if (string.IsNullOrEmpty(headerText))
                return;

            string line = new(wrapperChar, headerText.Length);

            WriteLine(line, dashColor);
            WriteLine(headerText, headerColor);
            WriteLine(line, dashColor);
        }

        private static readonly Lazy<Regex> colorBlockRegEx = new Lazy<Regex>(
            () => new Regex("\\[(?<color>.*?)\\](?<text>[^[]*)\\[/\\k<color>\\]", RegexOptions.IgnoreCase),
            isThreadSafe: true);


        /// <summary>
        /// Clip text to console width
        /// </summary>
        /// <param name="text">text to print</param>
        /// <returns>Clipped string which fits onto the console line without wrapping.</returns>
        static string ClipToConsole(string text)
        {
            // cmd and Windows Terminal behave differently
            // cmd wraps at one character less while Windows Terminal allows one more. We need to 
            // clip correctly in cmd and Windows Terminal
            if(ClipToConsoleWidth && myConsoleWidth != int.MaxValue &&  (Console.CursorLeft + LenWithTabs(text)) > myConsoleWidth )
            {
                int len = myConsoleWidth - Console.CursorLeft - 5;
                if (len < 0)
                {
                    text = "";
                }
                else
                {
                    text = SubstringWithTabCount(text, len) + "...";
                }
            }

            return text;
        }

        /// <summary>
        /// Calculate string length with expanded tabs as spaces of <see cref="ConsoleTabWidth"/>.
        /// </summary>
        /// <param name="text">Input string</param>
        /// <returns>Number of characters including expanded tabs as spaces.</returns>
        static int LenWithTabs(string text)
        {
            int len = 0;
            if (text != null)
            {
                foreach (var c in text)
                {
                    if (c == '\t')
                    {
                        len += ConsoleTabWidth;
                    }
                    else
                    {
                        len++;
                    }
                }
            }

            return len;
        }


        /// <summary>
        /// Returns the next n characters as the console would display it with a tab width of <see cref="ConsoleTabWidth"/>
        /// </summary>
        /// <param name="text">Input string</param>
        /// <param name="len">Number of expanded characters (tabs) the string should be cutted to.</param>
        /// <returns>Cutted string to len.</returns>
        static string SubstringWithTabCount(string text, int len)
        {
            int charCount = 0;
            int splitLen = text.Length;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\t')
                {
                    charCount += ConsoleTabWidth;
                }
                else
                {
                    charCount++;
                }

                if (charCount > len)
                {
                    splitLen = i;
                    break;
                }
            }

            string lret = text.Substring(0, splitLen);

            return lret;
        }

        /// <summary>
        /// Allows a string to be written with embedded color values using:
        /// This is [red]Red[/red] text and this is [cyan]Blue[/blue] text
        /// </summary>
        /// <param name="text">Text to display</param>
        /// <param name="baseTextColor">Base text color</param>
        /// <param name="skipLineFeed">If true the line feed after the write is skipped.</param>
        public static void WriteEmbeddedColorLine(string text, ConsoleColor? baseTextColor = null, bool skipLineFeed=false)
        {
            if (baseTextColor == null)
                baseTextColor = Console.ForegroundColor;

            if (string.IsNullOrEmpty(text))
            {
                if (!skipLineFeed)
                {
                    WriteLine(string.Empty);
                }
                return;
            }

            int at = text.IndexOf("[", StringComparison.Ordinal);
            int at2 = text.IndexOf("]", StringComparison.Ordinal);
            if (at == -1 || at2 <= at)
            {
                if (skipLineFeed)
                {
                    Write(ClipToConsole(text), baseTextColor);
                }
                else
                {
                    WriteLine(ClipToConsole(text), baseTextColor);
                }
                return;
            }

            while (true)
            {
                var match = colorBlockRegEx.Value.Match(text);
                if (match.Length < 1)
                {
                    Write(ClipToConsole(text), baseTextColor);
                    break;
                }

                // write up to expression
                string upToExpression = text.Substring(0, match.Index);
                upToExpression = ClipToConsole(upToExpression);
                Write(upToExpression, baseTextColor);

                // strip out the expression
                string highlightText = match.Groups["text"].Value;
                string colorVal = match.Groups["color"].Value;

                highlightText = ClipToConsole(highlightText);
                Write(highlightText, colorVal);

                // remainder of string
                text = text.Substring(match.Index + match.Value.Length);
            }

            if (!skipLineFeed)
            {
                Console.WriteLine();
            }
        }

        #endregion

        #region Success, Error, Info, Warning Wrappers

        /// <summary>
        /// Write a Success Line - green
        /// </summary>
        /// <param name="text">Text to write out</param>
        public static void WriteSuccess(string text)
        {
            WriteLine(text, ConsoleColor.Green);
        }

        /// <summary>
        /// Write a Error Line - Red
        /// </summary>
        /// <param name="text">Text to write out</param>
        public static void WriteError(string text)
        {
            WriteLine(text, ConsoleColor.Red);
        }

        /// <summary>
        /// Write a Warning Line - Yellow
        /// </summary>
        /// <param name="text">Text to Write out</param>
        public static void WriteWarning(string text)
        {
            WriteLine(text, ConsoleColor.DarkYellow);
        }


        /// <summary>
        /// Write a Info Line - dark cyan
        /// </summary>
        /// <param name="text">Text to write out</param>
        public static void WriteInfo(string text)
        {
            WriteLine(text, ConsoleColor.DarkCyan);
        }

        #endregion
    }

}