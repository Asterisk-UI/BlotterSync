using System;
using System.Collections.Generic;
using BlotterSync.DTOs;

namespace BlotterSync.Models;

public partial class Officer
{
    public int OfficerId { get; set; }

    public string BadgeNumber { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public bool? ActiveStatus { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public virtual ICollection<BlotterRecord> BlotterRecords { get; set; } = new List<BlotterRecord>();
}
