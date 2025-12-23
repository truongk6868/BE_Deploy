using System;
using System.Collections.Generic;

namespace CondotelManagement.Models;

public partial class Promotion
{
    public int PromotionId { get; set; }

    public int? CondotelId { get; set; }

    public string Name { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal DiscountPercentage { get; set; }

    public string? TargetAudience { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual Condotel? Condotel { get; set; }
}
