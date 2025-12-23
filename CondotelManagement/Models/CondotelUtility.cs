using System;
using System.Collections.Generic;

namespace CondotelManagement.Models;

public partial class CondotelUtility
{
    public int CondotelId { get; set; }

    public int UtilityId { get; set; }

    public DateOnly? DateAdded { get; set; }

    public string Status { get; set; } = null!;

    public virtual Condotel Condotel { get; set; } = null!;

    public virtual Utility Utility { get; set; } = null!;
}
