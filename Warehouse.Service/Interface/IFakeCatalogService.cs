using System;
using System.Collections.Generic;
using System.Text;
using Warehouse.Domain.External;

namespace Warehouse.Service.Interface;

public interface IFakeStoreCatalogService
{
    List<string> GetCategories();
    List<ExternalCatalogItem> GetProductsByCategory(string category);
    ExternalCatalogItem? GetByExternalId(string externalId);
}

