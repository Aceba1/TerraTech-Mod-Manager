/* 
 *  ORIGINAL SOURCE:
 *  https://github.com/StasTserk/DndStuff/blob/b77907800beefed61e6c0a1758d5b46135f2dea1/Providers/MarkdownToAnsiRtfProvider.cs
 */

using System;
using System.Text.RegularExpressions;

namespace TerraTechModManager
{
    static class MarkdownToRTF
    {
        // Limited conversion 
        public static string ToRTFString(string input)
        {
            input = EscapeBackslash(input);
            input = Regex.Replace(input, @"^\s*-\s+", @" \bullet  ", RegexOptions.Multiline);
            //input = ReplaceSignificantWhitespace(input);
            //input = RemoveInsignificantWhitespaceFromString(input);
            input = Regex.Replace(input, @"(\r?\n){2,}", "\\par\\par ");
            input = Regex.Replace(input, @"(\r?\n)+", "\\par ");

            input = AddFormatting(input);

            var output = String.Concat(@"{\rtf\ansi ", input, "}");
            return output;
        }

        private static string EscapeBackslash(string input)
        {
            var backslashRegex = new Regex(@"\\");
            return backslashRegex.Replace(input, @"\\");
        }

        private static string AddFormatting(string input)
        {

            input = AddFormatting(input, new Regex(@"\*\*"), @"\b");
            input = AddFormatting(input, new Regex(@"\*"), @"\i");
            input = AddFormatting(input, new Regex(@"_"), @"\i");
            input = AddFormatting(input, new Regex(@"\~\~"), @"\strike");

            var InBox = new Regex(@"`");
            input = AddFormatting(input, InBox, InBox, @"{\f1\cb14\cf1 ", @"}");
            return input;
        }

        private static string AddFormatting(string input, Regex WrapRegex, string formatOption)
        {
            while (WrapRegex.IsMatch(input))
            {
                // Add opening italics mark
                input = WrapRegex.Replace(input, String.Concat(formatOption, " "), 1);

                // Disable on the next occurance of a match
                input = WrapRegex.Replace(input, String.Concat(formatOption, "0 "), 1);
            }

            return input;
        }

        private static string AddFormatting(string input, Regex PreRegex, Regex PostRegex, string PreFormat, string PostFormat)
        {
            while (PreRegex.IsMatch(input))
            {
                // Add opening italics mark
                input = PreRegex.Replace(input, PreFormat, 1);

                // Disable on the next occurance of a match
                input = PostRegex.Replace(input, PostFormat, 1);
            }

            return input;
        }

        private static string ReplaceSignificantWhitespace(string input)
        {
            // Significant whitespace is 2 or more new lines
            var whitespaceRegex = new Regex(@"(\n)(\s)*(\n)");

            if (whitespaceRegex.IsMatch(input))
            {
                // Replace any Group of whitespace characters with a single space
                return whitespaceRegex.Replace(input, @"\par ");
            }

            return input;
        }

        private static string RemoveInsignificantWhitespaceFromString(string input)
        {
            var whitespaceRegex = new Regex(@"\s{2,}");

            if (whitespaceRegex.IsMatch(input))
            {
                // Replace any Group of whitespace characters with a single space
                return whitespaceRegex.Replace(input, " ");
            }

            return input;
        }
    }
}

