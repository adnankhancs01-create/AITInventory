using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.SupportiveEntities
{
    public class ApplicationUserIdentity : IdentityUser
    {
        public int UserId { get; set; }
    }
}
