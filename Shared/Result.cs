using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Shared
{
    [DataContract]
    public class Result
    {
        public static Result<T> Empty<T>()
        {
            return new Result<T>(true, default);
        }

        public static Result Fail(string error)
        {
            return new Result(false, error);
        }
        public static Result Fail(Exception ex)
        {
            return new Result(false, null, ex);
        }

        public static Result<T> Fail<T>(string error)
        {
            return new Result<T>(false, default, error);
        }

        public static Result<T> Fail<T>(Exception ex)
        {
            return new Result<T>(false, default, exception: ex);
        }

        public static Result Ok()
        {
            return new Result(true);
        }

        public static Result<T> Ok<T>(T value)
        {
            return new Result<T>(true, value, null);
        }


        public Result(bool isSuccess, string? error = null, Exception? exception = null)
        {
            IsSuccess = isSuccess;
            Error = error;
            Exception = exception;

            if (!IsSuccess && string.IsNullOrWhiteSpace(Error) && Exception is null)
            {
                throw new ArgumentException("An error message or exception must be supplied for an unsuccessful result.");
            }

            if (string.IsNullOrWhiteSpace(Error) && Exception is not null)
            {
                Error = Exception.Message;
            }

            if (Exception is null && !string.IsNullOrWhiteSpace(Error))
            {
                Exception = new Exception(Error);
            }
        }

        [DataMember]
        public bool IsSuccess { get; init; }

        [DataMember]
        public string? Error { get; init; }

        [DataMember]
        public Exception? Exception { get; init; }


    }

    [DataContract]
    public class Result<T>
    {
        public Result(bool isSuccess, T? value, string? error = null, Exception? exception = null)
        {
            IsSuccess = isSuccess;
            Error = error;
            Value = value;
            Exception = exception;

            if (!IsSuccess && string.IsNullOrWhiteSpace(Error) && Exception is null)
            {
                throw new ArgumentException("An error message or exception must be supplied for an unsuccessful result.");
            }

            if (string.IsNullOrWhiteSpace(Error) && Exception is not null)
            {
                Error = Exception.Message;
            }

            if (Exception is null && !string.IsNullOrWhiteSpace(Error))
            {
                Exception = new Exception(Error);
            }
        }


        [DataMember]
        public bool IsSuccess { get; init; }

        [DataMember]
        public string? Error { get; init; }

        [DataMember]
        public Exception? Exception { get; init; }

        [DataMember]
        public T? Value { get; init; }
    }
}
