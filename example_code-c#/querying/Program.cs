using System.Text.RegularExpressions;
using System.Data.SQLite;
using System.Collections.Specialized;

namespace querying
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // make sure to use dot as decimal separator
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            Console.WriteLine("Enter a CEQ, or :q to exit (ex: k = 6, brand = 'volkswagen')");
            while (true)
            {
                string? ceq = Console.ReadLine();
                if (ceq is null) continue;
                if (ceq == ":q") break;

                (int k, List<CeqEntry> entries) = parse_ceq(ceq);
                if (entries.Count == 0) Console.WriteLine("I couldn't parse that, check your input");
                else query(k, entries);
            }
        }

        /// <summary>
        /// Parse a CEQ
        /// </summary>
        /// <param name="ceq">Possible CEQ</param>
        /// <returns>A tuple containing the found k and the list of entries</returns>
        static (int, List<CeqEntry>) parse_ceq(string ceq)
        {
            // default to 10
            int k = 10;
            List<CeqEntry> entries = [];

            // capture alphanum = 1234
            Regex capture_num = new Regex(@"(\w+)\s*=\s*(\d+)");

            // capture alphanum = "alphanum-and-dash"
            Regex capture_cat = new Regex(@"(\w+)\s*=\s*\'([0-9a-zA-Z-.+/()@ ]+)\'");

            // find all matches, then add it to the entry
            MatchCollection num_matches = capture_num.Matches(ceq);
            foreach (Match match in num_matches)
            {
                string key = match.Groups[1].Value;
                string value = match.Groups[2].Value;
                if (key == "k")
                {
                    // k is not part of what we want to search, but it instead determines how much we want to search
                    k = Int32.Parse(value);
                } else
                {
                    entries.Add(new CeqEntry(key, value));
                }
            }

            MatchCollection cat_matches = capture_cat.Matches(ceq);
            foreach (Match match in cat_matches)
            {
                string key = match.Groups[1].Value;
                string value = match.Groups[2].Value;
                entries.Add(new CeqEntry(key, value));
            }

            return (k, entries);
        }

        /// <summary>
        /// Pretty prints the results to the console, note that things aren't being sorted here
        /// If the table doesn't completely fit on the console, remember you can zoom out with ctrl -
        /// </summary>
        /// <param name="head">Header names</param>
        /// <param name="scores">Sorted list of scores for each row</param>
        /// <param name="rows">Sorted list of rows, with each row being a list of values</param>
        static void pretty_print_results(List<string> head, List<double> scores, List<List<string>> rows)
        {
            // add first 2 column names
            List<string> header = ["K", "Score"];
            header.AddRange(head);

            // attach score to each row
            List<List<string>> rows_with_score = [];
            for (int i = 0; i < rows.Count; i++)
            {
                List<string> row_with_score = [(i + 1).ToString(), scores[i].ToString("n4")];
                row_with_score.AddRange(rows[i]);
                rows_with_score.Add(row_with_score);
            }

            // figure out text widths
            List<int> text_widths = header.Select(h => h.Length).ToList();
            foreach (List<string> row in rows_with_score) 
            {
                for (int i = 0; i < row.Count; i++)
                {
                    text_widths[i] = Math.Max(text_widths[i], row[i].Length);
                }
            }

            // divider
            // first create dashes that we can then Join with plusses
            // we do +2 here because we add spaces besides the vertical dashes
            List<string> dashes = text_widths.Select(v => new string('-', v + 2)).ToList();
            string divider = " +" + String.Join('+', dashes) + "+";
            Console.WriteLine(divider);

            // padding the header with enough spaces
            for (int i = 0; i < header.Count; i++)
            {
                string h = header[i];
                int totalLength = text_widths[i];
                
                // the PadLeft and PadRight methods want to know how long the string should be AFTER the padding
                // using that, it determines how many spaces to place on the left, respectively right, side of the input string
                // so we first pass PadLeft the length of the string with only half of the spaces
                // then we pass the total length to PadRight
                // this way there should be equal number of spaces on either side (maybe a difference of 1)
                int neededSpaces = totalLength - h.Length;
                int spacesLeft = neededSpaces / 2;

                h = h.PadLeft(spacesLeft + h.Length, ' ');
                header[i] = h.PadRight(totalLength, ' ');
            }

            // print header
            Console.WriteLine(" | " + String.Join(" | ", header) + " | ");

            // divider
            Console.WriteLine(divider);

            // rows
            foreach (List<string> row in rows_with_score)
            {
                for (int i = 0; i < row.Count; i++)
                {
                    string val = row[i];

                    int totalLength = text_widths[i];
                    int neededSpaces = totalLength - val.Length;
                    int spacesLeft = neededSpaces / 2;

                    val = val.PadLeft(spacesLeft + val.Length, ' ');
                    row[i] = val.PadRight(totalLength, ' ');
                }

                Console.WriteLine(" | " + String.Join(" | ", row) + " | ");
            }

            Console.WriteLine(divider);
        }

        /// <summary>
        /// Run the query, and print the result
        /// </summary>
        /// <param name="k">Number of items to find</param>
        /// <param name="entries">Attribute values to search on</param>
        static void query(int k, List<CeqEntry> entries)
        {
            // In reality, don't do this as it's prone to SQL injections!!!
            string query = "SELECT * FROM autompg WHERE " + String.Join(" AND ", entries.Select(e => $"{e.attribute} = \"{e.value}\""));

            // get results
            string dir = Path.Join(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "resources");
            string connectionString = "Data Source=" + Path.Join(dir, "autompg.sqlite") + "; Version=3;";

            List<string> header = [];
            List<List<string>> results = [];

            using SQLiteConnection conn = new SQLiteConnection(connectionString);
            conn.Open();

            SQLiteCommand cmd = conn.CreateCommand();
            cmd.CommandText = query;
            SQLiteDataReader reader = cmd.ExecuteReader();

            for (int j = 0; j < reader.FieldCount; j++)
            {
                header.Add(reader.GetName(j));
            }

            // NOTE: this reads until there are no more results, or until k results have been grabbed!!
            int i = 0;
            while (reader.Read() && i < k)
            {
                i++;

                List<string> row = [];

                // collection of key,value pairs, where key is column name and value is the value in this row
                NameValueCollection kvs = reader.GetValues();
                for (int j = 0; j < kvs.Count; j++)
                {
                    string? key = kvs.GetKey(j);
                    string[]? values = kvs.GetValues(j);

                    if (key is not null && values is not null)
                    {
                        // there is only ever one value per attribute, so always take [0]
                        row.Add(values[0]);
                    }
                }
                results.Add(row);
            }
            reader.Close();
            conn.Close();

            // pretty print results
            // scores are simply 1 since there's no ranking yet
            pretty_print_results(header, Enumerable.Repeat(1d, results.Count).ToList(), results);
        }
    }
}
