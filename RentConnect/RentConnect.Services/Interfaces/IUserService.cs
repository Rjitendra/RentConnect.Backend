

namespace RentConnect.Services.Interfaces
{
    using RentConnect.Models.Dtos;
    using RentConnect.Services.Utility;

    public interface IUserService
    {
        /// <summary>
        /// Acquires the current context's user ID.
        /// </summary>
        /// <returns></returns>
        public int GetUserId();

        long UpdateUser(ApplicationUserDto dto);

        long CreateUser(ApplicationUserDto dto);

        long CreateMultipleUser(IList<ApplicationUserDto> dto);

        Task<Result<IEnumerable<ApplicationUserRoleDto>>> Roles();

        bool DeleteApplicationUser(List<long> ids);

        Task<Result<ApplicationUserDto>> GetUserDetail(long id);
    }
}
