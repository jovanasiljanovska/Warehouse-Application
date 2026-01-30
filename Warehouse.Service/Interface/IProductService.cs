using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warehouse.Domain.Domain;
using Warehouse.Domain.Domain.Enums;

namespace Warehouse.Service.Interface;

public interface IProductService
{
    List<Product> GetAll();
    Product? GetById(Guid id);

    Product Insert(Product product);
    Product Update(Product product);
    Product DeleteById(Guid id);
}
