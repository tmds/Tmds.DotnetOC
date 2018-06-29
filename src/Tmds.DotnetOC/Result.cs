using System;

namespace Tmds.DotnetOC
{
    class Result
    {
        private static readonly Result s_success = new Result(isSuccess: true, content: string.Empty);

        public Result(bool isSuccess, string content)
        {
            IsSuccess = isSuccess;
            Content = content;
        }

        public static Result Success(string content = null)
        {
            if (string.IsNullOrEmpty(content))
            {
                return s_success;
            }
            return new Result(isSuccess: true, content: content);
        }

        public static Result Error(string msg) => new Result(isSuccess: false, content: msg ?? "Unknown error");

        public bool IsSuccess { get; }
        public string Content { get; }
    }

    static class ResultExtensions
    {
        public static bool CheckFailed(this Result result, IConsole console)
        {
            if (!result.IsSuccess)
            {
                console.WriteErrorLine(result.Content);
            }
            return !result.IsSuccess;
        }

        public static bool CheckFailed(this Result result, IConsole console, out string val)
        {
            if (!result.IsSuccess)
            {
                console.WriteErrorLine(result.Content);
                val = null;
            }
            else
            {
                val = result.Content;
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

    class Result<T>
    {
        public Result(bool isSuccess, T content, string errorMessage)
        {
            IsSuccess = isSuccess;
            Value = content;
            ErrorMessage = errorMessage;
        }

        public static Result<T> Error(string msg) => new Result<T>(isSuccess: false, content: default(T), errorMessage: msg ?? "Unknown error");

        public static Result<T> Success(T val) => new Result<T>(isSuccess: true, content: val, errorMessage: null);

        public static implicit operator Result<T>(T val) => Success(val);
        public static implicit operator Result<T>(Result val)
        {
            if (val.IsSuccess)
            {
                throw new InvalidOperationException();
            }
            return Result<T>.Error(val.Content);
        }

        public bool IsSuccess { get; }
        public T Value { get; }
        public string ErrorMessage { get; }
    }
}