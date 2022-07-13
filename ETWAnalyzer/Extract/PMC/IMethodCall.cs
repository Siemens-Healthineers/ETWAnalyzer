namespace ETWAnalyzer.Extract.PMC
{
    /// <summary>
    /// Method Call data recorded by LBR data
    /// </summary>
    public interface IMethodCall
    {
        /// <summary>
        /// Calling method
        /// </summary>
        string Caller { get; }

        /// <summary>
        /// Method Name
        /// </summary>
        string MethodName { get; }

        /// <summary>
        /// Sampled Call Count (actual call count is much higher)
        /// </summary>
        long Count { get; }

        /// <summary>
        /// Process in which these calls did occur
        /// </summary>
        ETWProcess Process { get; }
    }
}