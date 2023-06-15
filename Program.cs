/*
 * This program takes a pcbnew file, and converts the straight lines and text on
 * the User.Drawings layer to the kicad_wks format. So you could import a DXF
 * format template/title block to pcbnew, then save the file and use this
 * program to convert it for use in eeschema and pcbnew. You will need to add
 * fields manually using pl_editor still.
 *
 * Curves are ignored because pl_editor doesn't support them, I ended up
 * replacing the curves with straight segments in a separate program and
 * inserting the result into pcbnew. Naturally it would be better if this
 * program could do that for you.
 *
 * Only suitable for KiCad 7.0 files and possibly later versions.
 */

using System.Text.RegularExpressions;

namespace wks_converter
{
    class Program
    {
        const int decimalPlacesSupportedInWKSFile = 4;
        const float lineWidth = 0.075f; // TODO make this an arg
        const float textLineWidth = 0.075f; // TODO make this an arg

        const int pageWidthMM = 594; // TODO make this an arg or parse the page size from the pcbnew file and look it up
        const int pageHeightMM = 420;

        static readonly Regex graphicalLineRegex = new Regex(@"\(gr_line \(start ([\d.]+) ([\d.]+)\) \(end ([\d.]+) ([\d.]+)\)");
        static readonly Regex graphicalTextRegex = new Regex(@"\(gr_text \""([^""]+)\"" \(at ([\d.]+) ([\d.]+)\) \(layer ""Dwgs.User""\) \(tstamp [\d\w-]+\)[\t\n\r ]+\(effects \(font \(size ([\d.]+) ([\d.]+)\) \(thickness ([\d.]+)\) *([\w ]*)\) \(justify ([\w ]+)\)\)[\t\n\r ]+\)");

        static void WriteOutputFileHeader(StreamWriter writer)
        {
            writer.WriteLine($@"(kicad_wks (version 20220228) (generator pl_editor)");
            writer.WriteLine($@"  (setup (textsize 1.5 1.5)(linewidth {lineWidth})(textlinewidth {textLineWidth})");
            writer.WriteLine($@"  (left_margin 0)(right_margin 0)(top_margin 0)(bottom_margin 0))");
        }

        static void WriteOutputFileFooter(StreamWriter writer)
        {
            writer.WriteLine($@")");
        }

        static double ParseDouble(string input)
        {
            double v = Double.Parse(input);
            return Math.Round(v, decimalPlacesSupportedInWKSFile);
        }

        static (double X, double Y) ParseVector2(string inputX, string inputY)
        {
            return (ParseDouble(inputX), ParseDouble(inputY));
        }

        static (double X, double Y) ParsePageCoords(string inputX, string inputY)
        {
            return (pageWidthMM - ParseDouble(inputX), pageHeightMM - ParseDouble(inputY));
        }

        static void ParseGraphicalLines(string fileContents, StreamWriter writer)
        {
            MatchCollection matches = graphicalLineRegex.Matches(fileContents);

            foreach (Match match in matches)
            {
                GroupCollection groups = match.Groups;

                var start = ParsePageCoords(groups[1].Value, groups[2].Value);
                var end = ParsePageCoords(groups[3].Value, groups[4].Value);

                writer.WriteLine($@"  (line (name """") (start {start.X} {start.Y}) (end {end.X} {end.Y}))");
            }
        }

        static void ParseGraphicalTextItems(string fileContents, StreamWriter writer)
        {
            MatchCollection matches = graphicalTextRegex.Matches(fileContents);

            foreach (Match match in matches)
            {
                GroupCollection groups = match.Groups;

                string text = groups[1].Value;

                var start = ParsePageCoords(groups[2].Value, groups[3].Value);
                var fontSize = ParseVector2(groups[5].Value, groups[4].Value);
                var fontThickness = ParseDouble(groups[6].Value);
                string style = groups[7].Value;
                string justification = groups[8].Value;

                text = text.Replace("{dblquote}", "\\\"");

                writer.WriteLine($@"  (tbtext ""{text}"" (name """") (pos {start.X} {start.Y})(font (size {fontSize.X} {fontSize.Y}) {style}) (justify {justification}))");
            }
        }

        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Specify input file path, then output file path. e.g. infile.kicad_pcb outfile.kicad_wks");
                return;
            }

            string inputFilePath = args[0];
            string outputFilePath = args[1];

            string fileContents = File.ReadAllText(inputFilePath);

            using (StreamWriter writer = new StreamWriter(outputFilePath))
            {
                WriteOutputFileHeader(writer);

                /* I will parse the whole file in one go rather than line by
                line, because some records take up more than one line. */

                ParseGraphicalLines(fileContents, writer);
                ParseGraphicalTextItems(fileContents, writer);

                WriteOutputFileFooter(writer);
                writer.Close();
            }

            Console.WriteLine("Finished");
        }
    }
}