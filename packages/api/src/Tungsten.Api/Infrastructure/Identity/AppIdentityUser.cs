using Microsoft.AspNetCore.Identity;

namespace Tungsten.Api.Infrastructure.Identity;

public class AppIdentityUser : IdentityUser
{
    public Guid? AppUserId { get; set; }
}
