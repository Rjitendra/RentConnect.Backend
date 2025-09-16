namespace RentConnect.Services.Implementations
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using RentConnect.Models.Context;
    using RentConnect.Models.Dtos;
    using RentConnect.Models.Entities;
    using RentConnect.Services.Interfaces;
    using RentConnect.Services.Utility;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using RentConnect.Models.Enums;

    public class UserService : IUserService
    {
        public UserService(ApiContext db, IHttpContextAccessor context)

        {
            this.Db = db;
            this.CurrentUser = new CurrentUser(context.HttpContext.User);
        }

        /// <summary>
        /// Current user.
        /// </summary>
        private CurrentUser CurrentUser { get; set; }

        private ApiContext Db { get; set; }

        public int GetUserId()
        {
            return CurrentUser.ApplicationUserId;
        }

        public long CreateUser(ApplicationUserDto entity)
        {
            try
            {
                var userExisted = this.Db.Users.Where(a => a.UserName == entity.UserName || a.Email == entity.Email).SingleOrDefault();

                if (userExisted != null)
                {
                    // if Username is duplicated returns -1 and its handled in Angualr side to display error message
                    return -1;
                }

                var appUser = new ApplicationUser(entity);
                this.Db.Users.Add(appUser);
                this.Db.SaveChanges();

                if (appUser.Id > 0)
                {
                    this.AddUserToTheRole(entity, appUser.Id);

                    // Adding user information into the claims
                    this.AddOrUpdateUserClaim(IdentityServerClaims.FullName, $"{entity.FirstName} {entity.LastName}", appUser.Id);
                    this.AddOrUpdateUserClaim(IdentityServerClaims.FirstName, entity.FirstName, appUser.Id);
                    this.AddOrUpdateUserClaim(IdentityServerClaims.LastName, entity.LastName, appUser.Id);
                    this.AddOrUpdateUserClaim(IdentityServerClaims.Email, entity.Email, appUser.Id);

                    return appUser.Id;
                }
                else
                {
                    // if user info is not saved and its handled in Angualr side to display error message
                    return 0;
                }
            }
            catch (Exception ex) { throw ex; }
        }

        public long CreateMultipleUser(IList<ApplicationUserDto> dto)
        {
            try
            {
                var userExisted = this.Db.Users.Where(a => (dto.Select(x => x.Email).ToList()).Contains(a.Email)).AsNoTracking().ToList();
                if (userExisted.Count() != 0) { return -1; }
                long id = 0;
                foreach (var entity in dto)
                {
                    var result = CreateUser(entity);

                    id = result;
                }

                return id;
            }
            catch (Exception ex) { return -1; }
        }

        public long UpdateUser(ApplicationUserDto entity)
        {
            var user = this.Db.Users.Find(entity.ApplicationUserId);
            if (user == null)
            {
                return 0;
            }
            if ((entity.Postcode == user.Postcode) && (entity.Address == user.Address) && (entity.PhoneNumber == user.PhoneNumber)) { return user.Id; }
            user.UpdateAspNetUser(entity);
            this.Db.SaveChanges();

            return user.Id;
        }

        public bool DeleteApplicationUser(List<long> ids)
        {
            try
            {
                foreach (var id in ids)
                {
                    var user = this.Db.Users.Where(x => x.Id == id).SingleOrDefault();
                    this.Db.Users.Remove(user);
                    this.Db.SaveChanges();
                }
                ;

                return true;
            }
            catch (Exception ex)
            {
                // log the exception here
                return false;
            }
        }

        public async Task<Result<IEnumerable<ApplicationUserRoleDto>>> Roles()
        {
            try
            {
                var UserRoles = await this.Db.Roles.Select(a => new ApplicationUserRoleDto { Id = (ApplicationUserRole)a.Id, Name = a.Name }).ToListAsync();
                return Result<IEnumerable<ApplicationUserRoleDto>>.Success(UserRoles);
            }
            catch (Exception ex) { throw ex; }
        }

        public async Task<Result<ApplicationUserDto>> GetUserDetail(long id)
        {
            try
            {
                var user = await this.Db.Users.Where(x => x.Id == this.CurrentUser.ApplicationUserId).SingleOrDefaultAsync();
                var applicationUser = new ApplicationUserDto
                {
                    Email = this.CurrentUser.Email,
                    FirstName = this.CurrentUser.FirstName,
                    LastName = this.CurrentUser.LastName,
                    PhoneNumber = user.PhoneNumber,
                    Postcode = user.Postcode,
                    Address = user.Address,
                    DateCreated = user.DateCreated,
                };
                return Result<ApplicationUserDto>.Success(applicationUser);
            }
            catch (Exception ex) { throw ex; }
        }

        /// <summary>
        /// Method for Adding user into the role
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="userId"></param>
        private void AddUserToTheRole(ApplicationUserDto entity, long userId)
        {
            try
            {
                var userRole = new List<IdentityUserRole<long>>();

                // Create role(s) for user
                foreach (var role in entity.UserRoles)
                {
                    userRole.Add(new IdentityUserRole<long>()
                    {
                        RoleId = (int)role.Id,
                        UserId = userId
                    });
                }

                this.Db.UserRoles.AddRange(userRole);
                this.Db.SaveChanges();
            }
            catch (Exception ex) { throw ex; }
        }

        /// <summary>
        /// Adds or updates the specified user claim type for the specified user.
        /// </summary>
        /// <param name="claimType">Claim type.</param>
        /// <param name="claimValue">Claim value.</param>
        /// <param name="applicationUserId">User whose claims are to be modified.</param>
        /// <returns></returns>

        private void AddOrUpdateUserClaim(string claimType, string claimValue, long applicationUserId)
        {
            try
            {
                // fetch existing claim
                var claim = this.Db.UserClaims.SingleOrDefault(x => x.ClaimType == claimType && x.UserId == applicationUserId);

                // if no claim exists, create one
                if (claim == null)
                {
                    this.Db.UserClaims.Add(new IdentityUserClaim<long> { ClaimType = claimType, ClaimValue = claimValue, UserId = applicationUserId });
                    this.Db.SaveChanges();
                }
                else
                {
                    // otherwise, update the claim
                    claim.ClaimValue = claimValue;
                }
            }
            catch (Exception ex) { throw ex; }
        }
    }
}
