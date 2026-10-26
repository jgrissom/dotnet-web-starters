using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Cryptids.Web.Models;

// A member of the Registry. Everything Identity needs — the email, the
// password hash, the lockout counters — comes from IdentityUser. This class
// is where the Registry's own facts about a member go.
public class Member : IdentityUser
{
    // The name a member signs their reports with. A new column on AspNetUsers.
    [StringLength(40)]
    public string FieldName { get; set; } = "";
}
