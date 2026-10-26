using Common.Users2;
using Domain.Entities;

namespace Domain.IRepositories
{
    public interface IUserAccountRepo2
    {
        Task<bool> AnyRootAsync();
        Task<int> CountUsersAsync();

        /// <summary>Oldest user that has a business name (an account created by the old sign-up page), or the oldest user.</summary>
        Task<UserMst?> FindLegacyOwnerAsync();
        Task<List<UserMst>> GetUsersWithoutBusinessAsync();

        Task<Business2?> GetBusinessAsync();
        void AddBusiness(Business2 business);

        /// <summary>Tracked user matched by email or user name, with the role loaded.</summary>
        Task<UserMst?> FindByLoginAsync(string identifier);
        Task<UserMst?> GetUserAsync(int userId);
        Task<UserMst?> GetRootAsync();

        Task<bool> EmailExistsAsync(string email, int excludeUserId);
        Task<bool> UserNameExistsAsync(string userName, int excludeUserId);
        void AddUser(UserMst user);

        Task<List<UserListItemDto2>> GetUserListAsync(UserFilterDto2 filter);
    }
}
