using System;
using System.Collections.Generic;

namespace CondotelManagement.Models;

public partial class Resort
{
    public int ResortId { get; set; }

    public int LocationId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? Address { get; set; }

    public virtual ICollection<Condotel> Condotels { get; set; } = new List<Condotel>();

    public virtual Location Location { get; set; } = null!;

    public virtual ICollection<ResortUtility> ResortUtilities { get; set; } = new List<ResortUtility>();
}
