using System;
using System.Collections.Generic;
using System.Text;

namespace Inno_Shop.Products.Domain
{
    public class Product
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; } = true;
        public string OwnerId { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsHidden { get; set; }
    }
}
