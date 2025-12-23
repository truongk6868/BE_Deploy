using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CondotelManagement.Models;

[Table("HostPackage")]
public partial class HostPackage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int HostPackageId { get; set; }

    [Required]
    public int HostId { get; set; }

    [Required]
    public int PackageId { get; set; }

    [Column(TypeName = "date")]
    public DateOnly? StartDate { get; set; }

    [Column(TypeName = "date")]
    public DateOnly? EndDate { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Active";

    [StringLength(50)]
    public string? OrderCode { get; set; }

    public int? DurationDays { get; set; }

    // Navigation properties
    [ForeignKey("HostId")]
    public virtual Host Host { get; set; } = null!;

    [ForeignKey("PackageId")]
    public virtual Package Package { get; set; } = null!;
}
