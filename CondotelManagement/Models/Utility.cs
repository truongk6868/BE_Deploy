using System;
using System.Collections.Generic;

namespace CondotelManagement.Models;

public partial class Utility
{
    public int UtilityId { get; set; }

	public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? Category { get; set; }

	public virtual ICollection<CondotelUtility> CondotelUtilities { get; set; } = new List<CondotelUtility>();

    public virtual ICollection<ResortUtility> ResortUtilities { get; set; } = new List<ResortUtility>();
}
