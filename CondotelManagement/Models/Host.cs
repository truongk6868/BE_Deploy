using Google.Apis.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace CondotelManagement.Models;

public partial class Host
{
    public int HostId { get; set; }

    public int UserId { get; set; }

    public string? CompanyName { get; set; }


    public string? Address { get; set; }

    public string? PhoneContact { get; set; }

    public string Status { get; set; } = null!;

    public string? IdCardFrontUrl { get; set; }

    public string? IdCardBackUrl { get; set; }

    public string? VerificationStatus { get; set; } // Pending, Approved, Rejected

    public DateTime? VerifiedAt { get; set; }

    public string? VerificationNote { get; set; }

    public virtual ICollection<Condotel> Condotels { get; set; } = new List<Condotel>();

    public virtual ICollection<HostPackage> HostPackages { get; set; } = new List<HostPackage>();
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    public virtual ICollection<Wallet> Wallets { get; set; } = new List<Wallet>();

	public virtual ICollection<ServicePackage> ServicePackages { get; set; }
    

    public virtual ICollection<BlogRequest> BlogRequests { get; set; } = new List<BlogRequest>();

	public virtual ICollection<Amenity> Amenities { get; set; }

	public HostVoucherSetting VoucherSetting { get; set; }

}
