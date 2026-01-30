using System;
using System.Collections.Generic;
using System.Text;
using Warehouse.Domain.Domain.Enums;
namespace Warehouse.Domain.External;

public class ExternalCatalogItem
{
    public string ExternalId { get; set; } = ""; // ex: "FS-12"
    public string Name { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public int? UnitPrice { get; set; }
    public string? ImageUrl { get; set; }
}

