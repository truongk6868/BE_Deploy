using System;
using System.Collections.Generic;

namespace CondotelManagement.Models;

public partial class ServicePackage
{
    public int ServiceId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public string Status { get; set; } = null!;

	public int HostID { get; set; }

	public Host Host { get; set; }

	public virtual ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();
}
