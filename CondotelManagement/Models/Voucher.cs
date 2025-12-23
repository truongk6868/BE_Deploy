using System;
using System.Collections.Generic;

namespace CondotelManagement.Models;

public partial class Voucher
{
    public int VoucherId { get; set; }

    public int? CondotelId { get; set; }

	public int? UserId { get; set; }

	public string Code { get; set; } = null!;

    public decimal? DiscountAmount { get; set; }

    public decimal? DiscountPercentage { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public int? UsageLimit { get; set; }

    public int? UsedCount { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual Condotel? Condotel { get; set; }

	public virtual User User { get; set; }
}
