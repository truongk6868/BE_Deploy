using System;
using System.Collections.Generic;

namespace CondotelManagement.Models;

public partial class Location
{
    public int LocationId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public virtual ICollection<Resort> Resorts { get; set; } = new List<Resort>();
}
