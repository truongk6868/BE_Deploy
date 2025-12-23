using System;
using System.Collections.Generic;

namespace CondotelManagement.Models;

public partial class Condotel
{
    public int CondotelId { get; set; }

    public int HostId { get; set; }

    public int? ResortId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal PricePerNight { get; set; }

    public int Beds { get; set; }

    public int Bathrooms { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<CondotelAmenity> CondotelAmenities { get; set; } = new List<CondotelAmenity>();

    public virtual ICollection<CondotelDetail> CondotelDetails { get; set; } = new List<CondotelDetail>();

    public virtual ICollection<CondotelImage> CondotelImages { get; set; } = new List<CondotelImage>();

    public virtual ICollection<CondotelPrice> CondotelPrices { get; set; } = new List<CondotelPrice>();

    public virtual ICollection<CondotelUtility> CondotelUtilities { get; set; } = new List<CondotelUtility>();

    public virtual Host Host { get; set; } = null!;

    public virtual ICollection<Promotion> Promotions { get; set; } = new List<Promotion>();

    public virtual Resort? Resort { get; set; }

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();
}
