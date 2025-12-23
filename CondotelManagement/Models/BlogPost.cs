using System;
using System.Collections.Generic;

namespace CondotelManagement.Models;

public partial class BlogPost
{
    public int PostId { get; set; }

    public string Title { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string? FeaturedImageUrl { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? PublishedAt { get; set; }

    public int AuthorUserId { get; set; }

    public int? CategoryId { get; set; }

    public virtual User AuthorUser { get; set; } = null!;

    public virtual BlogCategory? Category { get; set; }
}
