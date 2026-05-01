using System.Collections.Specialized;
using System.Data.SQLite;
using System.Text.RegularExpressions;

namespace preprocessing
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // make sure to use dot as decimal separator
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            preprocess();
        }

        /// <summary>
        /// Preprocesses all data, and creates the metadb
        /// </summary>
        static void preprocess()
        {
            // TODO: Implement this!
            // The template only provides an example that adds the counts of the categorical entries to the MetaDB

            // TODO: inspect the database, and choose for yourself which attributes should be considered
            // numerical and which ones should be considered categorical
            // place the names of those attributes in these lists,
            // such that the values in the database are properly classified according to your choices
            List<string> categorical_atts = [];
            List<string> numerical_atts = [];

            // reading and using all the data from the txt files
            (List<WorkloadCategoricalItem> workload_categorical,
                List<WorkloadNumericalItem> workload_numerical,
                List<WorkloadList> workload_lists) = read_workload(numerical_atts);

            (List<NumericalEntry> database_numerical, 
                List<CategoricalEntry> database_categorical) = read_and_create_database(numerical_atts);

            // count all categorical entries
            Dictionary<(string, string), int> counts = [];
            foreach (WorkloadCategoricalItem entry in workload_categorical)
            {
                (string, string) key = (entry.attribute, entry.value);
                int count = counts.ContainsKey(key) ? counts[key] : 0;
                counts[key] = count + entry.count;
            }

            // initialize the metadb
            MetaDB metadb = new MetaDB();

            List<List<object>> values = counts.Select(kv => new List<object> { kv.Key.Item1, kv.Key.Item2, kv.Value }).ToList();

            // add the counts table
            metadb.add_table(
                "counts_categorical",
                [("attribute", "TEXT"), ("value", "TEXT"), ("count", "real")],
                values);

            // actually create the database, metadb.txt and metaload.txt
            metadb.create();
        }

        /// <summary>
        /// Read in the query workload from workload.txt
        /// </summary>
        /// <param name="numerical_atts">List of numerical attributes</param>
        /// <returns>A tuple containing both categorical and numerical (column = value) results,
        /// and the workload lists (column IN (...)) results</returns>
        static (List<WorkloadCategoricalItem>, List<WorkloadNumericalItem>, List<WorkloadList>) read_workload(List<string> numerical_atts)
        {
            string dir = Path.Join(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "resources");

            string content;
            using (StreamReader sr = new StreamReader(Path.Join(dir, "workload.txt")))
            {
                content = sr.ReadToEnd();
            }

            List<WorkloadCategoricalItem> categorical = [];
            List<WorkloadNumericalItem> numerical = [];
            List<WorkloadList> lists = [];

            // capture 'number times: ...'
            Regex capture_count = new Regex(@"(\d+) times: SELECT .* FROM .* WHERE (.*)");

            // capture 'key = 'value-separated''
            Regex capture_cat = new Regex(@"(\S*)\s*=\s*\'([a-zA-Z-]*)\'");

            // capture 'key = 123.456'
            Regex capture_num = new Regex(@"(\S*)\s*=\s*\'([\d\.]*)\'");

            // capture 'key IN ('a','b')
            Regex capture_in = new Regex(@"(\S*)\s*IN\s*\((.*)\)");

            // capture 'string'
            Regex capture_str = new Regex(@"\'([\w -]*)\'");

            MatchCollection count_matches = capture_count.Matches(content);

            foreach (Match count_match in count_matches)
            {
                int count = Int32.Parse(count_match.Groups[1].Value);
                string query = count_match.Groups[2].Value;

                // find all the ... IN ...
                MatchCollection in_matches = capture_in.Matches(query);

                foreach (Match in_match in in_matches)
                {
                    string key = in_match.Groups[1].Value;
                    string values = in_match.Groups[2].Value;
                    MatchCollection separated_values = capture_str.Matches(values);

                    List<string> vals = separated_values.Select(s => s.Groups[1].ToString()).ToList();

                    lists.Add(new WorkloadList(count, key, vals));
                }

                // find all the key = 123.456
                MatchCollection num_matches = capture_num.Matches(query);

                foreach (Match num_match in num_matches)
                {
                    string key = num_match.Groups[1].Value;

                    if (numerical_atts.Contains(key))
                    {
                        double value = double.Parse(num_match.Groups[2].Value);
                        WorkloadNumericalItem item = new(count, key, value);

                        numerical.Add(item);
                    } else
                    {
                        string value = num_match.Groups[2].Value;
                        WorkloadCategoricalItem item = new(count, key, value);

                        categorical.Add(item);
                    }
                }

                // find all key = 'value'
                MatchCollection cat_matches = capture_cat.Matches(query);

                foreach (Match cat_match in cat_matches)
                {
                    string key = cat_match.Groups[1].Value;
                    string value = cat_match.Groups[2].Value;
                    WorkloadCategoricalItem item = new(count, key, value);

                    categorical.Add(item);
                }
            }

            return (categorical, numerical, lists);
        }

        /// <summary>
        /// Reads the autompg database from the file that fills it,
        /// and creates a new sqlite database
        /// </summary>
        /// <param name="numerical_atts">List of numerical attributes</param>
        /// <returns>A tuple containing both lists of categorical and numerical entries of the autompg database</returns>
        static (List<NumericalEntry>, List<CategoricalEntry>) read_and_create_database(List<string> numerical_atts)
        {
            List<NumericalEntry> num_entries = [];
            List<CategoricalEntry> cat_entries = [];

            string dir = Path.Join(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "resources");

            // clear existing database
            // note that this doesn't throw an exception if the file doesn't exist
            File.Delete(Path.Join(dir, "autompg.sqlite"));

            string connectionString = "Data Source=" + Path.Join(dir, "autompg.sqlite") + "; Version=3;";
            using SQLiteConnection conn = new SQLiteConnection(connectionString);
            conn.Open();

            // load in the database
            using (StreamReader sr = new StreamReader(Path.Join(dir, "autompg.sql")))
            {
                string content = sr.ReadToEnd();

                SQLiteCommand command = conn.CreateCommand();
                command.CommandText = content;
                command.ExecuteNonQuery();
            }

            // read all the entries from the database
            SQLiteCommand command_selectall = conn.CreateCommand();
            command_selectall.CommandText = "SELECT * FROM autompg";
            SQLiteDataReader reader = command_selectall.ExecuteReader();

            // reads each result one by one
            while (reader.Read())
            {
                // row id
                int id = reader.GetInt32(0);

                // collection of key,value pairs, where key is column name and value is the value in this row
                NameValueCollection kvs = reader.GetValues();

                // start at 1 so we don't read id column
                for (int i = 1; i < kvs.Count; i++)
                {
                    string? key = kvs.GetKey(i);
                    string[]? values = kvs.GetValues(i);

                    if (key is not null && values is not null)
                    {
                        // there is only ever one value per attribute, so always take [0]
                        string value = values[0];

                        // if this attribute is numerical...
                        if (numerical_atts.Contains(key))
                        {
                            num_entries.Add(new NumericalEntry(key, id, double.Parse(value)));
                        }
                        else
                        {
                            cat_entries.Add(new CategoricalEntry(key, id, value));
                        }
                    }
                }
            }
            reader.Close();
            conn.Close();

            return (num_entries, cat_entries);
        }
    }
}
