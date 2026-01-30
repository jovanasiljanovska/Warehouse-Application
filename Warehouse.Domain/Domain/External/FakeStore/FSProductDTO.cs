using System;
using System.Collections.Generic;
using System.Text;
namespace Warehouse.Domain.External.FakeStore;

public class FSProductDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public double Price { get; set; }
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public string Image { get; set; } = "";
}

