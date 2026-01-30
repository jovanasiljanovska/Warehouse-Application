using System;
using System.Collections.Generic;
using System.Text;
using Warehouse.Domain.Domain;

namespace Warehouse.Service.Interface;

public interface ICategoryService
{
    List<Category> GetAll();
    Category? GetById(Guid id);
    Category Insert(Category category);
    Category Update(Category category);
    Category DeleteById(Guid id);
}

