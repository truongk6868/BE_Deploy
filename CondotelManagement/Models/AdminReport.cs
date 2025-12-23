using System;
using System.Collections.Generic;

namespace CondotelManagement.Models;

public partial class AdminReport
{
    public int ReportId { get; set; }

    public int AdminId { get; set; }

    public string? ReportType { get; set; }

    public DateTime GeneratedDate { get; set; }

    public string? FileUrl { get; set; }

    public virtual User Admin { get; set; } = null!;
}
