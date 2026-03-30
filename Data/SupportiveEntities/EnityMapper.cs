using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.SupportiveEntities
{
    public static class EnityMapper
    {
        public static ApplicationUser ToDomain(ApplicationUserIdentity user)
        {
            if (user == null) return null;

            return new ApplicationUser
            {
                UserId = user.UserId,
                //Email = user.Email,
                //Password = user.Password,
                //UserName = user.UserName
            };
        }
    }
}
