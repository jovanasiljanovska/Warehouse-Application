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
    public class CategoryServiceTests
    {
        private readonly Mock<IRepository<Category>> _categoryRepo;
        private readonly CategoryService _sut;

        public CategoryServiceTests()
        {
            _categoryRepo = new Mock<IRepository<Category>>();
            _sut = new CategoryService(_categoryRepo.Object);
        }

        [Fact]
        public void Insert_WhenCategoryIsNull_ShouldThrowArgumentNullException()
        {
            var act = () => _sut.Insert(null!);

            Assert.Throws<ArgumentNullException>(act);
            _categoryRepo.Verify(r => r.Insert(It.IsAny<Category>()), Times.Never);
        }

        [Fact]
        public void Insert_WhenNameIsEmpty_ShouldThrowException_AndNotInsert()
        {
            var c = new Category
            {
                Id = Guid.NewGuid(),
                Name = "   "
            };

            var ex = Assert.Throws<Exception>(() => _sut.Insert(c));

            Assert.Equal("Category name is required.", ex.Message);
            _categoryRepo.Verify(r => r.Insert(It.IsAny<Category>()), Times.Never);
        }

        [Fact]
        public void Insert_WhenIdIsEmpty_ShouldThrowException_AndNotInsert()
        {
            var c = new Category
            {
                Id = Guid.Empty,
                Name = "Food"
            };

            var ex = Assert.Throws<Exception>(() => _sut.Insert(c));

            Assert.Equal("CategoryId is required.", ex.Message);
            _categoryRepo.Verify(r => r.Insert(It.IsAny<Category>()), Times.Never);
        }

        [Fact]
        public void Insert_WhenValid_ShouldInsertAndReturnCategory()
        {
            var c = new Category
            {
                Id = Guid.NewGuid(),
                Name = "Food"
            };

            _categoryRepo.Setup(r => r.Insert(c)).Returns(c);

            var result = _sut.Insert(c);

            Assert.Same(c, result);
            _categoryRepo.Verify(r => r.Insert(c), Times.Once);
        }

        [Fact]
        public void Update_ShouldCallRepositoryUpdate_AndReturnUpdatedCategory()
        {
            var c = new Category
            {
                Id = Guid.NewGuid(),
                Name = "Updated"
            };

            _categoryRepo.Setup(r => r.Update(c)).Returns(c);

            var result = _sut.Update(c);

            Assert.Same(c, result);
            _categoryRepo.Verify(r => r.Update(c), Times.Once);
        }

        [Fact]
        public void DeleteById_WhenCategoryNotFound_ShouldThrowException_AndNotDelete()
        {
            var id = Guid.NewGuid();

            _categoryRepo.Setup(r => r.Get(
                    It.IsAny<Expression<Func<Category, Category>>>(),
                    It.IsAny<Expression<Func<Category, bool>>>(),
                    It.IsAny<Func<IQueryable<Category>, IOrderedQueryable<Category>>>(),
                    It.IsAny<Func<IQueryable<Category>, IIncludableQueryable<Category, object>>>()
                ))
                .Returns((Category?)null);

            var ex = Assert.Throws<Exception>(() => _sut.DeleteById(id));

            
            Assert.Equal("Category not found.", ex.Message);

            _categoryRepo.Verify(r => r.Delete(It.IsAny<Category>()), Times.Never);
        }

        [Fact]
        public void DeleteById_WhenCategoryExists_ShouldDeleteAndReturnDeletedCategory()
        {
            var id = Guid.NewGuid();
            var existing = new Category
            {
                Id = id,
                Name = "Food"
            };

            _categoryRepo.Setup(r => r.Get(
                    It.IsAny<Expression<Func<Category, Category>>>(),
                    It.IsAny<Expression<Func<Category, bool>>>(),
                    It.IsAny<Func<IQueryable<Category>, IOrderedQueryable<Category>>>(),
                    It.IsAny<Func<IQueryable<Category>, IIncludableQueryable<Category, object>>>()
                ))
                .Returns(existing);

            _categoryRepo.Setup(r => r.Delete(existing)).Returns(existing);

            var result = _sut.DeleteById(id);

            Assert.Same(existing, result);
            _categoryRepo.Verify(r => r.Delete(existing), Times.Once);
        }

        [Fact]
        public void GetById_WhenNotFound_ShouldReturnNull()
        {
            var id = Guid.NewGuid();

            _categoryRepo.Setup(r => r.Get(
                    It.IsAny<Expression<Func<Category, Category>>>(),
                    It.IsAny<Expression<Func<Category, bool>>>(),
                    It.IsAny<Func<IQueryable<Category>, IOrderedQueryable<Category>>>(),
                    It.IsAny<Func<IQueryable<Category>, IIncludableQueryable<Category, object>>>()
                ))
                .Returns((Category?)null);

            var result = _sut.GetById(id);

            Assert.Null(result);
        }

        [Fact]
        public void GetAll_WhenRepositoryReturnsItems_ShouldReturnList()
        {
            var items = new List<Category>
            {
                new Category { Id = Guid.NewGuid(), Name = "C1" },
                new Category { Id = Guid.NewGuid(), Name = "C2" }
            };

            _categoryRepo.Setup(r => r.GetAll(
                    It.IsAny<Expression<Func<Category, Category>>>(),
                    null,
                    null,
                    null
                ))
                .Returns(items);

            var result = _sut.GetAll();

            Assert.Equal(2, result.Count);
        }
    }
}
