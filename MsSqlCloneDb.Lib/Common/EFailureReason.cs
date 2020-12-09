using System;

namespace MsSqlCloneDb.Lib.Common
{
    [Obsolete]
    public enum EFailureReason
    {
        None,
        Permission,
        Concurrency,
        OtherSql,
        Warning,
        NonFatalImportError,
        Other,
        Signature,
        MissingEntries,
        RecordNotFound,
        UserActionRequired,
        VersionMissmatch,
        Facebook, // Datenschutzerklärung
        Validation
    }

    public static class EFailureReasonExtension
    {
        private const string FailureCodeOtherSql = "OtherSql";
        private const string FailureCodeWarning = "Warning";
        private const string FailureCodeNonFatalImportError = "NonFatalImportError";
        private const string FailureCodeOther = "Other";
        private const string FailureCodeMissingEntries = "MissingEntries";
        private const string FailureCodeUserActionRequired = "UserActionRequired";
        private const string FailureCodeVersionMissmatch = "VersionMissmatch";
        private const string FailureCodeDatenschutzerklaerung = "Datenschutzerklaerung";

        [Obsolete]
        public static EFailureCategory ToFailureCategory(this EFailureReason failureReason)
        {
            switch (failureReason)
            {
                case EFailureReason.Permission: return EFailureCategory.Permission;
                case EFailureReason.Concurrency: return EFailureCategory.Concurrency;
                case EFailureReason.RecordNotFound: return EFailureCategory.NotFound;
                case EFailureReason.Signature: return EFailureCategory.Signature;
                case EFailureReason.Validation: return EFailureCategory.Validation;
            }

            return EFailureCategory.Unknown;
        }

        [Obsolete]
        public static string ToFailureCode(this EFailureReason failureReason)
        {
            switch (failureReason)
            {
                case EFailureReason.OtherSql: return FailureCodeOtherSql;
                case EFailureReason.Warning: return FailureCodeWarning;
                case EFailureReason.NonFatalImportError: return FailureCodeNonFatalImportError;
                case EFailureReason.Other: return FailureCodeOther;
                case EFailureReason.MissingEntries: return FailureCodeMissingEntries;
                case EFailureReason.UserActionRequired: return FailureCodeUserActionRequired;
                case EFailureReason.VersionMissmatch: return FailureCodeVersionMissmatch;
                case EFailureReason.Facebook: return FailureCodeDatenschutzerklaerung;
            }

            return null;
        }

        [Obsolete]
        public static EFailureReason ToFailureReason(this EFailureCategory failureCategory, string failureCode)
        {
            switch (failureCategory)
            {
                case EFailureCategory.Permission: return EFailureReason.Permission;
                case EFailureCategory.Concurrency: return EFailureReason.Concurrency;
                case EFailureCategory.Validation: return EFailureReason.Validation;
                case EFailureCategory.NotFound: return EFailureReason.RecordNotFound;
                case EFailureCategory.Signature: return EFailureReason.Signature;
            }

            switch (failureCode)
            {
                case FailureCodeOtherSql: return EFailureReason.OtherSql;
                case FailureCodeWarning: return EFailureReason.Warning;
                case FailureCodeNonFatalImportError: return EFailureReason.NonFatalImportError;
                case FailureCodeOther: return EFailureReason.Other;
                case FailureCodeMissingEntries: return EFailureReason.MissingEntries;
                case FailureCodeUserActionRequired: return EFailureReason.UserActionRequired;
                case FailureCodeVersionMissmatch: return EFailureReason.VersionMissmatch;
                case FailureCodeDatenschutzerklaerung: return EFailureReason.Facebook;
            }

            return EFailureReason.None;
        }
    }
}