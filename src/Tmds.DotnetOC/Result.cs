using System;

namespace Tmds.DotnetOC
{
    class Result
    {
        private static readonly Result s_success = new Result(isSuccess: true, errorMessage: null);

        protected Result(bool isSuccess, string errorMessage)
        {
            if (!isSuccess && errorMessage == null)
            {
                throw new ArgumentNullException(nameof(errorMessage));
            }
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }

        public static Result Success() => s_success;
        public static Result<T> Success<T>(T val) => Result<T>.Success(val);

        public static Result Error(string errorMessage) => new Result(isSuccess: false, errorMessage: errorMessage);

        public bool IsSuccess { get; }
        public string ErrorMessage { get; }
    }

    static class ResultExtensions
    {
        public static bool CheckFailed(this Result result, IConsole console)
        {
            if (!result.IsSuccess)
            {
                console.WriteErrorLine(result.ErrorMessage);
            }
            return !result.IsSuccess;
        }

        public static bool CheckFailed<T>(this Result<T> result, IConsole console, out T val)
        {
            if (!result.IsSuccess)
            {
                console.WriteErrorLine(result.ErrorMessage);
                val = default(T);
            }
            else
            {
                val = result.Value;
            }
            return !result.IsSuccess;
        }
    }

    class Result<T> : Result
    {
        private Result(bool isSuccess, T content, string errorMessage)
            : base(isSuccess, errorMessage)
        {
            Value = content;
        }

        public static new Result<T> Error(string msg) => new Result<T>(isSuccess: false, content: default(T), errorMessage: msg ?? "Unknown error");

        public static Result<T> Success(T val) => new Result<T>(isSuccess: true, content: val, errorMessage: null);

        public static implicit operator Result<T>(T val) => Success(val);

        public T Value { get; }
    }
}