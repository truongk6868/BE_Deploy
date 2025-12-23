using System;
using System.Collections.Generic;

namespace CondotelManagement.Models;

public partial class CondotelAmenity
{
    public int CondotelId { get; set; }

    public int AmenityId { get; set; }

    public DateOnly? DateAdded { get; set; }

    public string Status { get; set; } = null!;

    public virtual Amenity Amenity { get; set; } = null!;

    public virtual Condotel Condotel { get; set; } = null!;
}
