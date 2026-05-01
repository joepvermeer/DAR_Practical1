namespace querying
{
    /// <summary>
    /// A single entry in the CEQ
    /// </summary>
    internal class CeqEntry(string attribute, string value)
    {
        /// <summary>
        /// Attribute that's being queried
        /// </summary>
        public string attribute = attribute;
        
        /// <summary>
        /// Value that's being queried
        /// Note that we don't pay attention to whether this is technically a number or string anymore
        /// </summary>
        public string value = value;
    }
}
