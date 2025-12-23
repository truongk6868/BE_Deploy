using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CondotelManagement.Models;

[Table("Package")]
public partial class Package
{
    [Key]
    public int PackageId { get; set; }

    [Required]
    [StringLength(150)]
    public string Name { get; set; } = null!;

    [StringLength(255)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(12, 2)")]
    public decimal? Price { get; set; }

    [StringLength(50)]
    public string? Duration { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Active";
    public int? MaxListingCount { get; set; }
    public bool? CanUseFeaturedListing { get; set; }
    public int? MaxBlogRequestsPerMonth { get; set; }
    public bool? IsVerifiedBadgeEnabled { get; set; }
    public string? DisplayColorTheme { get; set; }
    public int? PriorityLevel { get; set; }
    // Navigation
    public virtual ICollection<HostPackage> HostPackages { get; set; } = new List<HostPackage>();
}