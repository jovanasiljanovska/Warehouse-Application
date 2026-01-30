using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using Warehouse.Domain.Domain;
using Warehouse.Domain.Domain.Enums;
using Warehouse.Repository.Interface;
using Warehouse.Service.Interface;

namespace Warehouse.Service.Implementation;

public class ProductService : IProductService
{
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<StockBalance> _stockRepo;

    public ProductService(IRepository<Product> productRepository)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
    }

    public List<Product> GetAll()
        => _productRepository.GetAll(
                selector: x => x,
                include: q => q.Include(p => p.Category)
                              .Include(p => p.Supplier)
                              .Include(p => p.StockBalances)
           ).ToList();

    public Product? GetById(Guid id)
        => _productRepository.Get(
                selector: x => x,
                predicate: p => p.Id == id,
                include: q => q.Include(p => p.Category)
                              .Include(p => p.Supplier)
                              .Include(p => p.StockBalances)
           );

    public Product Insert(Product product)
    {
        if (product == null) throw new ArgumentNullException(nameof(product));

        if (string.IsNullOrWhiteSpace(product.Name))
            throw new Exception("Product name is required.");

        if (product.CategoryId == Guid.Empty)
            throw new Exception("CategoryId is required.");

        return _productRepository.Insert(product);
    }

    public Product Update(Product product)
    {
        //if (product == null) throw new ArgumentNullException(nameof(product));
        //if (product.Id == Guid.Empty) throw new Exception("Product Id is required.");

        //var existing = _productRepository.Get(
        //    selector: x => x,
        //    predicate: p => p.Id == product.Id
        //);

        //if (existing == null)
        //    throw new Exception("Product not found.");

        //// Controlled update (safer than blindly updating)
        //existing.Name = product.Name;
        //existing.SKU = product.SKU;
        //existing.CategoryId = product.CategoryId;
        //existing.SupplierId = product.SupplierId;
        //existing.ImageURL = product.ImageURL;
        //existing.UnitPrice = product.UnitPrice;

        return _productRepository.Update(product);
    }

    public Product DeleteById(Guid id)
    {
        var product = _productRepository.Get(
            selector: x => x,
            predicate: p => p.Id == id
        );

        if (product == null)
            throw new Exception("Product not found.");

        return _productRepository.Delete(product);
    }
}
