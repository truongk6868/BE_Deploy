using System;
using System.Collections.Generic;

namespace CondotelManagement.Models;

public partial class Amenity
{
    public int AmenityId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? Category { get; set; }

	public int HostID { get; set; }

	public Host Host { get; set; }

	public virtual ICollection<CondotelAmenity> CondotelAmenities { get; set; } = new List<CondotelAmenity>();
}
