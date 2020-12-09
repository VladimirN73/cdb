using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace MsSqlCloneDb.Lib.Common
{
    public class Result
    {
        [DebuggerStepThrough]
        protected Result()
        {
            Succeeded = true;
            FailureCategory = EFailureCategory.Unknown;
        }

        [DataMember]
        public bool Succeeded { get; set; }
        [DataMember]
        public EFailureCategory FailureCategory { get; set; }
        [DataMember]
        public string FailureCode { get; set; }

        [Obsolete]
        public EFailureReason FailureReason
        {
            get => FailureCategory.ToFailureReason(FailureCode);
            set
            {
                FailureCategory = value.ToFailureCategory();
                FailureCode = value.ToFailureCode();
            }
        }

        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public object FailureContext { get; set; }

        public bool Failed => !Succeeded;

        [DebuggerStepThrough]
        public static Result Success()
        {
            return new Result();
        }

        [DebuggerStepThrough]
        public static Result Failure(string message)
        {
            return new Result { Succeeded = false, Message = message };
        }

        [DebuggerStepThrough]
        public static Result Failure(string message, object context)
        {
            return new Result { Succeeded = false, Message = message, FailureContext = context };
        }

        [DebuggerStepThrough]
        public static Result Failure(EFailureCategory failureCategory, string message, object failureContext = null)
        {
            return new Result { Succeeded = false, FailureCategory = failureCategory, Message = message, FailureContext = failureContext };
        }

        [DebuggerStepThrough]
        public static Result Failure(EFailureCategory failureCategory, string failureCode, string message)
        {
            return new Result { Succeeded = false, FailureCategory = failureCategory, FailureCode = failureCode, Message = message };
        }

        [DebuggerStepThrough, Obsolete]
        public static Result Failure(EFailureReason failureReason, string message)
        {
            return new Result { Succeeded = false, FailureReason = failureReason, Message = message };
        }

        [DebuggerStepThrough]
        public Result Return()
        {
            return new Result
            {
                Succeeded = Succeeded,
                FailureCategory = FailureCategory,
                FailureCode = FailureCode,
                Message = Message,
                FailureContext = FailureContext
            };
        }

        [DebuggerStepThrough]
        public Result<TNew> Return<TNew>(TNew value)
        {
            return new Result<TNew>
            {
                Succeeded = Succeeded,
                FailureCategory = FailureCategory,
                FailureCode = FailureCode,
                Message = Message,
                FailureContext = FailureContext,
                Value = value
            };
        }

        [DebuggerStepThrough]
        public Result<TNew> Return<TNew>(Func<Result, TNew> conversionFunc)
        {
            return new Result<TNew>
            {
                Succeeded = Succeeded,
                FailureCategory = FailureCategory,
                FailureCode = FailureCode,
                Message = Message,
                FailureContext = FailureContext,
                Value = conversionFunc(this)
            };
        }

        [DebuggerStepThrough]
        public Result Continue(Action continueAction)
        {
            if (Succeeded) continueAction();
            return this;
        }

        [DebuggerStepThrough]
        public Result Continue(Func<Result> continueFunc)
        {
            return Succeeded ? continueFunc() ?? this : this;
        }

        [DebuggerStepThrough]
        public async Task<Result> ContinueAsync(Func<Task<Result>> continueFunc)
        {
            return Succeeded ? await continueFunc() ?? this : this;
        }

        [DebuggerStepThrough]
        public Result<TNew> Continue<TNew>(Func<TNew> continueFunc)
        {
            return Succeeded ? Result<TNew>.Success(continueFunc()) ?? Return(default(TNew)) : Return(default(TNew));
        }

        [DebuggerStepThrough]
        public async Task<Result<TNew>> ContinueAsync<TNew>(Func<Task<TNew>> continueFunc)
        {
            return Succeeded ? Result<TNew>.Success(await continueFunc()) ?? Return(default(TNew)) : Return(default(TNew));
        }

        [DebuggerStepThrough]
        public Result<TNew> Continue<TNew>(Func<Result<TNew>> continueFunc)
        {
            return Succeeded ? continueFunc() ?? Return(default(TNew)) : Return(default(TNew));
        }

        [DebuggerStepThrough]
        public IEnumerable<Result<TNew>> Continue<TNew>(Func<IEnumerable<Result<TNew>>> continueFunc)
        {
            return Succeeded ? continueFunc() ?? new[] { Return(default(TNew)) } : new[] { Return(default(TNew)) };
        }

        [DebuggerStepThrough]
        public Result OnSucceeded(Action<Result> succeededAction)
        {
            if (Succeeded) succeededAction(this);
            return this;
        }

        [DebuggerStepThrough]
        public Result OnFailure(Action<Result> failureAction)
        {
            if (!Succeeded) failureAction(this);
            return this;
        }

        public virtual object GetValue()
        {
            return null;
        }

        public virtual Type GetValueType()
        {
            return null;
        }

        /// <summary>
        /// Erstellt einen erfolgreich abgeschlossenen Task mit dem Ergebnis dieses Results
        /// </summary>
        public Task<Result> ToTask()
        {
            return Task.FromResult(this);
        }
    }

    [DebuggerDisplay("{BMW.ICLxOnline.Common.ObjectExtensions.ToJsonForDebugging(this)}")]
    public class Result<TValue> : Result
    {
        protected internal Result() { }

        [DataMember]
        public TValue Value { get; set; }

        public static Result<TValue> Failure(TValue value, string message)
        {
            return new Result<TValue> { Succeeded = false, Message = message, Value = value };
        }

        public static Result<TValue> Failure(TValue value, EFailureCategory failureCategory, string message)
        {
            return new Result<TValue> { Succeeded = false, FailureCategory = failureCategory, Message = message, Value = value };
        }

        public static Result<TValue> Failure(TValue value, EFailureCategory failureCategory, string failureCode, string message)
        {
            return new Result<TValue> { Succeeded = false, FailureCategory = failureCategory, FailureCode = failureCode, Message = message, Value = value };
        }

        public static Result<TValue> Failure(TValue value, EFailureCategory failureCategory, object failureContext, string message)
        {
            return new Result<TValue> { Succeeded = false, FailureCategory = failureCategory, Message = message, FailureContext = failureContext, Value = value };
        }

        public static Result<TValue> Failure(TValue value, object failureContext, string message)
        {
            return new Result<TValue> { Succeeded = false, Message = message, FailureContext = failureContext, Value = value };
        }

        [Obsolete]
        public static Result<TValue> Failure(TValue value, EFailureReason failureReason, string message)
        {
            return new Result<TValue> { Succeeded = false, FailureReason = failureReason, Message = message, Value = value };
        }

        [Obsolete]
        public static Result<TValue> Failure(TValue value, EFailureReason failureReason, object failureContext, string message)
        {
            return new Result<TValue> { Succeeded = false, FailureReason = failureReason, FailureContext = failureContext, Message = message, Value = value };
        }

        public static Result<TValue> Success(TValue value)
        {
            return new Result<TValue> { Value = value };
        }

        [Obsolete]
        public static Result<TValue> Success(TValue value, EFailureReason failureReason, string message = null)
        {
            return new Result<TValue> { Value = value, FailureReason = failureReason, Message = message };
        }

        public static Result<TValue> Clone(TValue value, Result source)
        {
            return new Result<TValue>
            {
                Succeeded = source.Succeeded,
                FailureCategory = source.FailureCategory,
                FailureCode = source.FailureCode,
                FailureContext = source.FailureContext,
                Message = source.Message,
                Value = value
            };
        }

        public Result<TNew> Return<TNew>(Func<TValue, TNew> conversionFunc)
        {
            return new Result<TNew>
            {
                Succeeded = Succeeded,
                FailureCategory = FailureCategory,
                FailureCode = FailureCode,
                Message = Message,
                FailureContext = FailureContext,
                Value = conversionFunc(Value)
            };
        }

        public Result<TNew> ReturnFromResult<TNew>(Func<Result<TValue>, Result<TNew>> conversionFunc)
        {
            return conversionFunc(this);
        }

        public Result<TNew> Return<TNew>() where TNew : class
        {
            return Return(value => value as TNew);
        }

        public Result<IEnumerable<TNew>> ReturnEnumerable<TNew>()
        {
            return Return(value => ((IEnumerable)value)?.Cast<TNew>());
        }

        public Result<IReadOnlyCollection<TNew>> ReturnReadOnlyCollection<TNew>()
        {
            return Return(value => ((IEnumerable)value)?.Cast<TNew>().ToReadOnlyCollection());
        }

        public Result<IList<TNew>> ReturnList<TNew>()
        {
            return Return(value => (IList<TNew>)((IEnumerable)value)?.Cast<TNew>().ToList());
        }

        [DebuggerStepThrough]
        public Result Continue(Func<TValue, Result> continueFunc)
        {
            return Succeeded ? continueFunc(Value) ?? this : this;
        }

        [DebuggerStepThrough]
        public Result<TValue> Continue(Func<TValue, Result<TValue>> continueFunc)
        {
            return Succeeded ? continueFunc(Value) ?? this : this;
        }

        [DebuggerStepThrough]
        public Result<TValue> Continue(Func<Result<TValue>> continueFunc)
        {
            return Succeeded ? continueFunc() ?? this : this;
        }

        [DebuggerStepThrough]
        public Result<TNew> Continue<TNew>(Func<TValue, TNew> continueFunc)
        {
            return Succeeded ? Result<TNew>.Success(continueFunc(Value)) ?? Return(default(TNew)) : Return(default(TNew));
        }

        [DebuggerStepThrough]
        public Result<TValue> Continue(Action<TValue> continueAction)
        {
            if (Succeeded) continueAction(Value);
            return this;
        }

        [DebuggerStepThrough]
        public Result<TNew> Continue<TNew>(Func<TValue, Result<TNew>> continueFunc, Func<TValue, TNew> failureFunc = null)
        {
            return Succeeded ? continueFunc(Value) ?? Return(default(TNew)) : Return(failureFunc != null ? failureFunc(Value) : default(TNew));
        }

        [DebuggerStepThrough]
        public async Task<Result> ContinueAsync(Func<TValue, Task<Result>> continueFunc)
        {
            return Succeeded ? await continueFunc(Value) ?? this : this;
        }

        [DebuggerStepThrough]
        public async Task<Result<TValue>> ContinueAsync(Func<Task<Result<TValue>>> continueFunc)
        {
            return Succeeded ? await continueFunc() ?? this : this;
        }

        [DebuggerStepThrough]
        public async Task<Result<TNew>> ContinueAsync<TNew>(Func<TValue, Task<Result<TNew>>> continueFunc)
        {
            return Succeeded ? await continueFunc(Value) ?? Return(default(TNew)) : Return(default(TNew));
        }

        [DebuggerStepThrough]
        public async Task<Result<TNew>> ContinueAsync<TNew>(Func<TValue, Task<TNew>> continueFunc)
        {
            return Succeeded ? Result<TNew>.Success(await continueFunc(Value)) ?? Return(default(TNew)) : Return(default(TNew));
        }

        [DebuggerStepThrough]
        public Result<TValue> OnFinished(Action<Result<TValue>> succeededAction, Action<Result<TValue>> failureAction)
        {
            if (Succeeded) succeededAction(this);
            else failureAction(this);
            return this;
        }

        [DebuggerStepThrough]
        public Result<TValue> OnSucceeded(Action<Result<TValue>> succeededAction)
        {
            if (Succeeded) succeededAction(this);
            return this;
        }

        [DebuggerStepThrough]
        public Result<TValue> OnSucceeded(Action<TValue> succeededAction)
        {
            if (Succeeded) succeededAction(Value);
            return this;
        }

        [DebuggerStepThrough]
        public Result<TValue> OnSucceeded(Action succeededAction)
        {
            if (Succeeded) succeededAction();
            return this;
        }

        [DebuggerStepThrough]
        public Result<TValue> OnFailure(Action<Result<TValue>> failureAction)
        {
            if (!Succeeded) failureAction(this);
            return this;
        }

        [DebuggerStepThrough]
        public Result<TValue> OnFailure(Action<TValue> failureAction)
        {
            if (!Succeeded) failureAction(Value);
            return this;
        }

        [DebuggerStepThrough]
        public Result<TValue> OnFailure(Action failureAction)
        {
            if (!Succeeded) failureAction();
            return this;
        }

        public override object GetValue()
        {
            return Value;
        }

        public override Type GetValueType()
        {
            return typeof(TValue);
        }

        public string ToMessageOrValueString()
        {
            return Succeeded ? Value?.ToString() : Message;
        }

        /// <summary>
        /// Erstellt einen erfolgreich abgeschlossenen Task mit dem Ergebnis dieses typisierten Results
        /// </summary>
        public Task<Result<TValue>> ToTaskTyped()
        {
            return Task.FromResult(this);
        }
    }

    public static class ResultExtensions
    {

        public static async Task<Result<TNew>> ContinueAsync<TValue, TNew>(this Task<Result<TValue>> task, Func<TValue, TNew> continueFunc)
        {
            return (await task).Continue(continueFunc);
        }

        /// <summary>
        /// Loggt das Result als Error, wenn Result.Failed == true
        /// </summary>
        public static TResult OnFailureLogError<TResult>(this TResult result, ILogger logger) where TResult : Result
        {
            return result.OnFailureLog(logger);
        }

        /// <summary>
        /// Loggt das Result, wenn Result.Failed == true
        /// </summary>
        public static TResult OnFailureLog<TResult>(this TResult result, ILogger logger) where TResult: Result
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (result.Succeeded) return result;

            logger.AddLog(result.ToString());  //result.ToJson() -- TODO review it

            return result;
        }

        /// <summary>
        /// Loggt das Result als Warning, wenn Result.Failed == true
        /// </summary>
        public static Task<TResult> OnFailureLogWarn<TResult>(this Task<TResult> result, ILogger logger) where TResult : Result
        {
            return result.OnFailureLog(logger);
        }

        /// <summary>
        /// Loggt das Result, wenn Result.Failed == true
        /// </summary>
        public static async Task<TResult> OnFailureLog<TResult>(this Task<TResult> result, ILogger logger) where TResult : Result
        {
            return (await result).OnFailureLog(logger);
        }

        /// <summary>
        /// Löst eine ResultFailedException aus, wenn Result.Failed == true
        /// </summary>
        public static TResult OnFailureThrowException<TResult>(this TResult result) where TResult : Result
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (result.Succeeded) return result;

            throw new Exception(result.ToString());
        }

        [DebuggerStepThrough]
        public static Result<TItem> ReturnSingle<TItem>(this Result<IEnumerable<TItem>> result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (result.Value == null) return result.Return(default(TItem));

            var resultValue = result.Value as IReadOnlyCollection<TItem> ?? result.Value.ToList();
            if (resultValue.Count > 1) throw new ArgumentException("Can't return single result: Result contains more than one value.");

            return result.Return(resultValue.FirstOrDefault());
        }

        [DebuggerStepThrough]
        public static Result<IList<TItem>> Union<TItem>(this Result<IList<TItem>> thisResult, Result<IList<TItem>> otherResult)
        {
            if (thisResult == null) return otherResult;
            if (otherResult == null) return thisResult;

            if (thisResult.Failed)
            {
                thisResult.Value = UnionResultValues(thisResult, otherResult);
                return thisResult;
            }

            otherResult.Value = UnionResultValues(otherResult, thisResult);
            return otherResult;
        }

        private static IList<TItem> UnionResultValues<TItem>(Result<IList<TItem>> targetResult, Result<IList<TItem>> resultToAdd)
        {
            if (targetResult.Value == null) return resultToAdd.Value;
            if (resultToAdd.Value == null) return targetResult.Value;

            return targetResult.Value.Union(resultToAdd.Value).ToList();
        }
    }
}