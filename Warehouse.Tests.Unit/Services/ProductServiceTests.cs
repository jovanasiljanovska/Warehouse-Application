using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Xunit;
using Warehouse.Domain.Domain;
using Warehouse.Repository.Interface;
using Warehouse.Service.Implementation;

namespace Warehouse.Tests.Unit.Services
{
    public class ProductServiceTests
    {
        private readonly Mock<IRepository<Product>> _productRepo;
        private readonly ProductService _sut;

        public ProductServiceTests()
        {
            _productRepo = new Mock<IRepository<Product>>();
            _sut = new ProductService(_productRepo.Object);
        }

        [Fact]
        public void Constructor_WhenRepositoryIsNull_ShouldThrowArgumentNullException()
        {
            var act = () => new ProductService(null!);
            Assert.Throws<ArgumentNullException>(act);
        }

        [Fact]
        public void Insert_WhenProductIsNull_ShouldThrowArgumentNullException()
        {
            var act = () => _sut.Insert(null!);

            Assert.Throws<ArgumentNullException>(act);
            _productRepo.Verify(r => r.Insert(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public void Insert_WhenNameIsEmpty_ShouldThrowException_AndNotInsert()
        {
            var p = new Product
            {
                Id = Guid.NewGuid(),
                Name = "   ",
                CategoryId = Guid.NewGuid()
            };

            var ex = Assert.Throws<Exception>(() => _sut.Insert(p));

            Assert.Equal("Product name is required.", ex.Message);
            _productRepo.Verify(r => r.Insert(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public void Insert_WhenCategoryIdIsEmpty_ShouldThrowException_AndNotInsert()
        {
            var p = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Milk",
                CategoryId = Guid.Empty
            };

            var ex = Assert.Throws<Exception>(() => _sut.Insert(p));

            Assert.Equal("CategoryId is required.", ex.Message);
            _productRepo.Verify(r => r.Insert(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public void Insert_WhenValid_ShouldCallRepositoryInsert_AndReturnInsertedProduct()
        {
            var p = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Milk",
                CategoryId = Guid.NewGuid()
            };

            _productRepo.Setup(r => r.Insert(p)).Returns(p);

            var result = _sut.Insert(p);

            Assert.Same(p, result);
            _productRepo.Verify(r => r.Insert(p), Times.Once);
        }

        [Fact]
        public void Update_ShouldCallRepositoryUpdate_AndReturnUpdatedProduct()
        {
            var p = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Updated name",
                CategoryId = Guid.NewGuid()
            };

            _productRepo.Setup(r => r.Update(p)).Returns(p);

            var result = _sut.Update(p);

            Assert.Same(p, result);
            _productRepo.Verify(r => r.Update(p), Times.Once);
        }

        [Fact]
        public void DeleteById_WhenProductNotFound_ShouldThrowException_AndNotDelete()
        {
            var id = Guid.NewGuid();

            _productRepo.Setup(r => r.Get(
                    It.IsAny<Expression<Func<Product, Product>>>(),
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<Func<IQueryable<Product>, IOrderedQueryable<Product>>>(),
                    It.IsAny<Func<IQueryable<Product>, IIncludableQueryable<Product, object>>>()
                ))
                .Returns((Product?)null);

            var ex = Assert.Throws<Exception>(() => _sut.DeleteById(id));

            Assert.Equal("Product not found.", ex.Message);
            _productRepo.Verify(r => r.Delete(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public void DeleteById_WhenProductExists_ShouldDeleteAndReturnDeletedProduct()
        {
            var id = Guid.NewGuid();
            var existing = new Product
            {
                Id = id,
                Name = "Milk",
                CategoryId = Guid.NewGuid()
            };

            // Note: predicate/include in service are provided; we match them with It.IsAny
            _productRepo.Setup(r => r.Get(
                    It.IsAny<Expression<Func<Product, Product>>>(),
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<Func<IQueryable<Product>, IOrderedQueryable<Product>>>(),
                    It.IsAny<Func<IQueryable<Product>, IIncludableQueryable<Product, object>>>()
                ))
                .Returns(existing);

            _productRepo.Setup(r => r.Delete(existing)).Returns(existing);

            var result = _sut.DeleteById(id);

            Assert.Same(existing, result);
            _productRepo.Verify(r => r.Delete(existing), Times.Once);
        }

        [Fact]
        public void GetById_WhenRepositoryReturnsNull_ShouldReturnNull()
        {
            var id = Guid.NewGuid();

            _productRepo.Setup(r => r.Get(
                    It.IsAny<Expression<Func<Product, Product>>>(),
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<Func<IQueryable<Product>, IOrderedQueryable<Product>>>(),
                    It.IsAny<Func<IQueryable<Product>, IIncludableQueryable<Product, object>>>()
                ))
                .Returns((Product?)null);

            var result = _sut.GetById(id);

            Assert.Null(result);
        }

        [Fact]
        public void GetAll_WhenRepositoryReturnsItems_ShouldReturnList()
        {
            var items = new List<Product>
            {
                new Product { Id = Guid.NewGuid(), Name = "P1", CategoryId = Guid.NewGuid() },
                new Product { Id = Guid.NewGuid(), Name = "P2", CategoryId = Guid.NewGuid() }
            };

            _productRepo.Setup(r => r.GetAll(
                    It.IsAny<Expression<Func<Product, Product>>>(),
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<Func<IQueryable<Product>, IOrderedQueryable<Product>>>(),
                    It.IsAny<Func<IQueryable<Product>, IIncludableQueryable<Product, object>>>()
                ))
                .Returns(items);

            var result = _sut.GetAll();

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }
    }
}
