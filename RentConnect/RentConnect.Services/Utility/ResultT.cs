using RentConnect.Models.Enums;

namespace RentConnect.Services.Utility
{
    public class Result<T>
    {
        private Result(ResultStatusType status, string message = null)
        {
            this.Status = status;
            this.Message = message;
        }

        private Result(ResultStatusType status, T entity, string message = null)
        {
            this.Status = status;
            this.Entity = entity;
            this.Message = message;
        }

        public T Entity { get; }

        public bool IsSuccess => this.Status == ResultStatusType.Success;

        public string Message { get; }

        public ResultStatusType Status { get; }

        public static Result<T> Failure(string message = "Opertation failed")
        {
            return new Result<T>(ResultStatusType.Failure, message);
        }

        public static Result<T> Failure(T entity, string message = "Opertation failed")
        {
            return new Result<T>(ResultStatusType.Failure, entity, message);
        }

        public static Result<T> NotFound(string message = "Resource not found")
        {
            return new Result<T>(ResultStatusType.NotFound, default(T), message);
        }

        public static Result<T> Success(T entity, string message = "Operation completed successfully")
        {
            return new Result<T>(ResultStatusType.Success, entity, message);
        }
    }
}