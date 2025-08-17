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

        public static Result Failure(string message = null)
        {
            return new Result(ResultStatusType.Failure, message);
        }

        public static Result NotFound()
        {
            return new Result(ResultStatusType.NotFound);
        }

        public static Result Success()
        {
            return new Result(ResultStatusType.Success);
        }
    }
}