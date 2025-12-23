using System;
using System.Collections.Generic;

namespace CondotelManagement.Models;

public partial class CondotelPrice
{
    public int PriceId { get; set; }

    public int CondotelId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal BasePrice { get; set; }

    public string PriceType { get; set; } = null!;

    public string? Description { get; set; }

    public string Status { get; set; } = null!;

    public virtual Condotel Condotel { get; set; } = null!;
}
