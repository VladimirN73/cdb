namespace MsSqlCloneDb
{
    public static class CloneExtensions
    {
        public static CloneParametersExt AdaptParameters(this CloneParametersExt config)
        {
            return CloneParametersExt.AdaptParameters(config);
        }

        public static void PrintParameters(this CloneParametersExt config, ILogSink logger)
        {
            CloneParametersExt.PrintParameters(config, logger);
        }
    }
}
