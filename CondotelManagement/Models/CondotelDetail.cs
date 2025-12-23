using System;
using System.Collections.Generic;

namespace CondotelManagement.Models;

public partial class CondotelDetail
{
    public int DetailId { get; set; }

    public int CondotelId { get; set; }

    public string? BuildingName { get; set; }

    public string? RoomNumber { get; set; }

    public string? SafetyFeatures { get; set; }

    public string? HygieneStandards { get; set; }

    public string Status { get; set; } = null!;

    public virtual Condotel Condotel { get; set; } = null!;
}
