using System.Data.SQLite;

namespace preprocessing
{
    /// <summary>
    /// Represents the MetaDatabase, with functionality to fill the database.
    /// Automatically creates the contents of metadb.txt and metaload.txt
    /// </summary>
    class MetaDB
    {
        /// <summary>
        /// Will contain the contents of the metadb.txt file
        /// </summary>
        string metadb = "";
        
        /// <summary>
        /// Will contain the contents of the metaload.txt file
        /// </summary>
        string metaload = "";

        /// <summary>
        /// Adds a new table to the database.
        /// </summary>
        /// <param name="name">Table name</param>
        /// <param name="types">(Column name, Column type) pairs</param>
        /// <param name="values">Lists of possibly doubles or strings for the INSERT statements, one list per INSERT statement</param>
        public void add_table(string name, List<(string,string)> types, List<List<object>> values)
        {
            // this runs the following as SQL:
            // ```
            // CREATE TABLE name(
            //     types[0][0] types[0][1],
            //     types[1][0] types[1][1],
            //     ...
            // );
            // ```

            // and to fill the table:
            // ```
            // --name
            // INSERT INTO name VALUES(values[0][0], values[0][1], values[0][2], ...);
            // INSERT INTO name VALUES(values[1][0], values[1][1], values[1][2], ...);
            // INSERT INTO name VALUES ...;
            // ```

            // For strings, the needed "" are added automatically

            // To see the full output, run this file, 
            // then look at metadb.txt and metaload.txt

            // Constructing the CREATE TABLE statement
            // Don't do this when actually executing SQL queries
            metadb += "CREATE TABLE " + name + " (\n";

            for (int i = 0; i < types.Count; i++)
            {
                string colname = types[i].Item1;
                string coltype = types[i].Item2;

                // trailing comma only if not the last line
                metadb += $"    {colname} {coltype}" + (i < types.Count - 1 ? ",\n" : "\n");
            }

            metadb += ");\n\n";

            // Constructing the INSERT statements
            metaload += $"-- {name}\n";
            for (int i = 0; i < values.Count; i++)
            {
                List<object> row = values[i];

                metaload += $"INSERT INTO {name} VALUES (";
                for (int j = 0; j < row.Count; j++)
                {
                    object value = row[j];
                    if (value.GetType() == typeof(string))
                    {
                        metaload += $"\'{value}\'";
                    } else
                    {
                        metaload += $"{value}";
                    }

                    // comma only if not last
                    metaload += j < row.Count - 1 ? ", " : "";
                }
                metaload += ");\n";
            }
        }

        /// <summary>
        /// Creates the database as an sqlite database named metadb.sqlite,
        /// as well as metadb.txt and metaload.txt
        /// </summary>
        public void create()
        {
            string dir = Path.Join(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "resources");

            // clear existing database
            // note that this doesn't throw an exception if the file doesn't exist
            File.Delete(Path.Join(dir, "metadb.sqlite"));

            // write the files
            using (StreamWriter sw = new StreamWriter(Path.Join(dir, "metadb.txt")))
            {
                sw.Write(metadb);
            }
            using (StreamWriter sw = new StreamWriter(Path.Join(dir, "metaload.txt")))
            {
                sw.Write(metaload);
            }

            string connectionString = "Data Source=" + Path.Join(dir, "metadb.sqlite") + "; Version=3;";
            using SQLiteConnection conn = new SQLiteConnection(connectionString);
			conn.Open();

			SQLiteCommand command = conn.CreateCommand();
			command.CommandText = metadb;
			command.ExecuteNonQuery();

			command.CommandText = metaload;
			command.ExecuteNonQuery();

			conn.Close();
		}
    }
}
