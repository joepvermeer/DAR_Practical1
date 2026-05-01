namespace preprocessing
{
    /// <summary>
    /// Abstract class used for common properties of Workload items
    /// </summary>
    abstract class WorkloadItem(int count, string attribute)
    {
        /// <summary>
        /// How often this query occurred
        /// </summary>
        public int count = count;

        /// <summary>
        /// The attribute (column) that is queried
        /// </summary>
        public string attribute = attribute;
    }

    /// <summary>
    /// A numerical item read from the workload
    /// </summary>
    /// <param name="count">How often this query occurred</param>
    /// <param name="attribute">The attribute (column) that is queried</param>
    /// <param name="value">The value that's queried</param>
    class WorkloadNumericalItem(int count, string attribute, double value) : WorkloadItem(count, attribute)
    {
        /// <summary>
        /// The value that's queried
        /// </summary>
        public double value = value;

        public override string ToString()
        {
            return $"WorkloadNumericalItem('{count} times: {attribute} = {value}')";
        }
    }

    /// <summary>
    /// A numerical item read from the workload
    /// </summary>
    /// <param name="count">How often this query occurred</param>
    /// <param name="attribute">The attribute (column) that is queried</param>
    /// <param name="value">The value that's queried</param>
    class WorkloadCategoricalItem(int count, string attribute, string value) : WorkloadItem(count, attribute)
    {
        /// <summary>
        /// The value that's queried
        /// </summary>
        public string value = value;

        public override string ToString()
        {
            return $"WorkloadCategoricalItem('{count} times: {attribute} = {value}')";
        }
    }

    /// <summary>
    /// A list of values from an IN query read from the workload
    /// </summary>
    /// <param name="count">How often this query occurred</param>
    /// <param name="attribute">The attribute (column) that is queried</param>
    /// <param name="values">The list of associated values</param>
    class WorkloadList(int count, string attribute, List<string> values) : WorkloadItem(count, attribute)
    {
        /// <summary>
        /// The list of associated values
        /// </summary>
        public List<string> values = values;

        public override string ToString()
        {
            return $"WorkloadList('{count} times: {attribute} IN [{String.Join(',', values)}]')";
        }
    }
}
