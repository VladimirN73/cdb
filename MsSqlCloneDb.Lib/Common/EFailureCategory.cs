namespace MsSqlCloneDb.Lib.Common
{
    public enum EFailureCategory
    {
        Unknown,
        Permission,
        Concurrency,
        Validation,
        Signature,
        UserCancellation,
        Conflict,

        /// <summary>
        /// Errors due to lack of or inavlid authentication
        /// e.g. when a iclx session or apisec token is missing or expired
        /// </summary>
        Authentication,
        NotFound
    }
}