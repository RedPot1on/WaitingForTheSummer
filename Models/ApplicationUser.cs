using Microsoft.AspNetCore.Identity;

namespace WaitingForTheSummer.Models;

public class ApplicationUser : IdentityUser
{
    public Gender Gender { get; set; }
}
