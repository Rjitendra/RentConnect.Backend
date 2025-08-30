namespace RentConnect.Services.Utility
{
    using RentConnect.Models.Enums;

    public class Result
    {
        private Result(ResultStatusType status, string message = null)
        {
            this.Status = status;
            this.Message = message;
        }

        public string Message { get; set; }

        public ResultStatusType Status { get; }

        public static Result Failure(string message = "Opertation failed")
        {
            return new Result(ResultStatusType.Failure, message);
        }

        public static Result NotFound(string message = "Resource not found")
        {
            return new Result(ResultStatusType.NotFound, message);
        }

        public static Result Success(string message = "Operation completed successfully")
        {
            return new Result(ResultStatusType.Success, message);
        }
    }
}