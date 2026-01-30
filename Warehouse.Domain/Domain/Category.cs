using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Warehouse.Domain.Domain
{
    public class Category : BaseEntity
    {
        [Required]
        public  string Name { get; set; }
        public  string? Description { get; set; }
        public string? ImageURL { get; set; }
        public virtual ICollection<Product>? Products { get; set; }
    }
}
