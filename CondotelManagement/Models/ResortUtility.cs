using System;
using System.Collections.Generic;

namespace CondotelManagement.Models;

public partial class ResortUtility
{
    public int ResortId { get; set; }

    public int UtilityId { get; set; }

    public DateOnly? DateAdded { get; set; }

    public string Status { get; set; } = null!;

    public string? OperatingHours { get; set; }

    public decimal? Cost { get; set; }

    public string? DescriptionDetail { get; set; }

    public int? MaximumCapacity { get; set; }

    public virtual Resort Resort { get; set; } = null!;

    public virtual Utility Utility { get; set; } = null!;
}
