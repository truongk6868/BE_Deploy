using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace CondotelManagement.Models;
[Table("BlogCategories")]
public partial class BlogCategory
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public virtual ICollection<BlogPost> BlogPosts { get; set; } = new List<BlogPost>();
    public virtual ICollection<BlogRequest> BlogRequests { get; set; } = new List<BlogRequest>();
}
