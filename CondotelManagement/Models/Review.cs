using System;
using System.Collections.Generic;

namespace CondotelManagement.Models;

public partial class Review
{
    public int ReviewId { get; set; }

    public int CondotelId { get; set; }

    public byte Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }

    public int UserId { get; set; }

    public int? BookingId { get; set; }

	public string? Reply { get; set; }
	public string Status { get; set; } = "Visible";

	public virtual Booking? Booking { get; set; }

    public virtual Condotel Condotel { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
