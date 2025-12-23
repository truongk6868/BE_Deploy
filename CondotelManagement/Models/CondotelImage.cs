using System;
using System.Collections.Generic;

namespace CondotelManagement.Models;

public partial class CondotelImage
{
    public int ImageId { get; set; }

    public int CondotelId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public string? Caption { get; set; }

    public virtual Condotel Condotel { get; set; } = null!;
}
