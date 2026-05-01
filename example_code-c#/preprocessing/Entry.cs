namespace preprocessing
{
    /// <summary>
    /// Used for common properties of autompg entries
    /// </summary>
    /// <param name="attribute">Database column this entry is from</param>
    /// <param name="id">ID of the row this entry is from</param>
    abstract class Entry(string attribute, int id)
    {
        /// <summary>
        /// Database column this entry is from
        /// </summary>
        public string attribute = attribute;

        /// <summary>
        /// ID of the row this entry is from
        /// </summary>
        public int id = id;
    }

    /// <summary>
    /// A numerical entry in the autompg database
    /// </summary>
    /// <param name="attribute">Database column this entry is from</param>
    /// <param name="id">ID of row this entry is from</param>
    /// <param name="value">The numerical value of the corresponding attribute</param>
    class NumericalEntry(string attribute, int id, double value) : Entry(attribute, id)
    {
        /// <summary>
        /// The numerical value of the corresponding attribute
        /// </summary>
        public double value = value;
    }

    /// <summary>
    /// A categorical entry in the autompg database
    /// </summary>
    /// <param name="attribute">Database column this entry is from</param>
    /// <param name="id">ID of row this entry is from</param>
    /// <param name="value">The categorical value of the corresponding attribute</param>
    class CategoricalEntry(string attribute, int id, string value) : Entry(attribute, id)
    {
        /// <summary>
        /// The categorical value of the corresponding attribute
        /// </summary>
        public string value = value;
    }
}
