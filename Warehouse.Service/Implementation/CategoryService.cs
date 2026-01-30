using System;
using System.Collections.Generic;
using System.Text;
using Warehouse.Domain.Domain;
using Warehouse.Repository.Interface;
using Warehouse.Service.Interface;

namespace Warehouse.Service.Implementation;

public class CategoryService : ICategoryService
{
    private readonly IRepository<Category> _categoryRepo;

    public CategoryService(IRepository<Category> categoryRepo)
    {
        _categoryRepo = categoryRepo;
    }

    public List<Category> GetAll()
        => _categoryRepo.GetAll(x => x).ToList();

    public Category? GetById(Guid id)
        => _categoryRepo.Get(x => x, c => c.Id == id);

    public Category Insert(Category product)
    {
        if (product == null) throw new ArgumentNullException(nameof(product));

        if (string.IsNullOrWhiteSpace(product.Name))
            throw new Exception("Category name is required.");

        if (product.Id == Guid.Empty)
            throw new Exception("CategoryId is required.");

        return _categoryRepo.Insert(product);
    }

    public Category Update(Category product)
    {
        
        return _categoryRepo.Update(product);
    }

    public Category DeleteById(Guid id)
    {
        var product = _categoryRepo.Get(
            selector: x => x,
            predicate: p => p.Id == id
        );

        if (product == null)
            throw new Exception("Product not found.");

        return _categoryRepo.Delete(product);
    }
}


