using System;
using System.Collections.Generic;
using System.Text;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Xunit;
using Warehouse.Domain.Domain;
using Warehouse.Domain.Domain.Enums;
using Warehouse.Repository.Interface;
using Warehouse.Service.Interface;
using Warehouse.Service.Implementation;

namespace Warehouse.Tests.Unit.Services
{
    public class InventoryServiceTests
    {

        private readonly Mock<IRepository<StockBalance>> _stockRepo = new();
        private readonly InventoryService _sut;

        public InventoryServiceTests()
        {
            _sut = new InventoryService(_stockRepo.Object);
        }

        [Fact]
        public void SetInitialStock_WhenQuantityIsNegative_ShouldThrow()
        {
            var ex = Assert.Throws<Exception>(() =>
                    _sut.SetInitialStock(Guid.NewGuid(), -1, LocationType.Shelves));
            Assert.Equal("Quantity cannot be negative.", ex.Message);

            _stockRepo.Verify(r => r.Insert(It.IsAny<StockBalance>()), Times.Never);
            _stockRepo.Verify(r => r.Update(It.IsAny<StockBalance>()), Times.Never);
        }

        [Fact]
        public void SetInitialStock_WhenStockDoesNotExist_ShouldInsertNewRecord()
        {
            var productId= Guid.NewGuid();

            _stockRepo.Setup(r => r.Get(
                    It.IsAny<Expression<Func<StockBalance, StockBalance>>>(),
                    It.IsAny<Expression<Func<StockBalance, bool>>>(),
                    It.IsAny<Func<IQueryable<StockBalance>, IOrderedQueryable<StockBalance>>>(),
                    It.IsAny<Func<IQueryable<StockBalance>, IIncludableQueryable<StockBalance, object>>>()
                ))
                .Returns((StockBalance?)null);

            _sut.SetInitialStock(productId, 10, LocationType.Shelves);

            _stockRepo.Verify(r => r.Insert(It.Is<StockBalance>(sb =>
                sb.ProductId == productId &&
                sb.LocationType == LocationType.Shelves &&
                sb.Quantity == 10
            )), Times.Once);

            _stockRepo.Verify(r => r.Update(It.IsAny<StockBalance>()), Times.Never);
        }

        [Fact]
        public void SetInitialStock_WhenStockExists_ShouldIncreaseQuantity_AndUpdate()
        {
            var productId= Guid.NewGuid();
             StockBalance stock = new StockBalance{
                 Id=Guid.NewGuid(),
                 ProductId = productId,
                 LocationType = LocationType.Shelves,
                 Quantity = 10
             };

            _stockRepo.Setup(r => r.Get(
                    It.IsAny<Expression<Func<StockBalance, StockBalance>>>(),
                    It.IsAny<Expression<Func<StockBalance, bool>>>(),
                    It.IsAny<Func<IQueryable<StockBalance>, IOrderedQueryable<StockBalance>>>(),
                    It.IsAny<Func<IQueryable<StockBalance>, IIncludableQueryable<StockBalance, object>>>()
                ))
                .Returns(stock);

            _sut.SetInitialStock(productId, 5, LocationType.Shelves);

            Assert.Equal(15,stock.Quantity);
            _stockRepo.Verify(r => r.Update(It.IsAny<StockBalance>()), Times.Once);
            _stockRepo.Verify(r => r.Insert(It.IsAny<StockBalance>()), Times.Never);
        }

        [Fact]
        public void MoveToShipping_WhenQuantityIsZeroOrLess_ShouldThrow()
        {
            var ex = Assert.Throws<Exception>(() =>
                    _sut.MoveToShipping(Guid.NewGuid(), -1));
            Assert.Equal("Quantity must be > 0.", ex.Message);

            _stockRepo.Verify(r => r.Update(It.IsAny<StockBalance>()), Times.Never);
        }

        [Fact]
        public void MoveToShipping_WhenNotEnoughStock_ShouldThrow_AndNotUpdate()
        {
            var productId=Guid.NewGuid();
            var shelves = new StockBalance { Id = Guid.NewGuid(), ProductId = productId, LocationType = LocationType.Shelves, Quantity = 1 };
            var freezer = new StockBalance { Id = Guid.NewGuid(), ProductId = productId, LocationType = LocationType.Freezer, Quantity = 1 };
            var shipping = new StockBalance { Id = Guid.NewGuid(), ProductId = productId, LocationType = LocationType.Shipping, Quantity = 0 };

            _stockRepo.SetupSequence(r => r.Get(
                        It.IsAny<Expression<Func<StockBalance, StockBalance>>>(),
                        It.IsAny<Expression<Func<StockBalance, bool>>>(),
                        It.IsAny<Func<IQueryable<StockBalance>, IOrderedQueryable<StockBalance>>>(),
                        It.IsAny<Func<IQueryable<StockBalance>, IIncludableQueryable<StockBalance, object>>>()
                        ))
                        .Returns(shelves)   // 1st GetOrCreate -> Shelves
                        .Returns(freezer)   // 2nd -> Freezer
                        .Returns(shipping); // 3rd -> Shipping

            var ex = Assert.Throws<Exception>(() => _sut.MoveToShipping(productId, 5));
            Assert.Equal("Not enough stock in Shelves + Freezer.", ex.Message);

            _stockRepo.Verify(r => r.Update(It.IsAny<StockBalance>()), Times.Never);

        }

        [Fact]
        public void MoveToShipping_WhenShelvesAndFreezerHaveStock_ShouldMoveShelvesFirst_ThenFreezer_AndUpdateThreeTimes()
        {
            var productId = Guid.NewGuid();

            // shelves=3, freezer=10, shipping=1; need=5 => take 3 from shelves, 2 from freezer
            var shelves = new StockBalance { Id = Guid.NewGuid(), ProductId = productId, LocationType = LocationType.Shelves, Quantity = 3 };
            var freezer = new StockBalance { Id = Guid.NewGuid(), ProductId = productId, LocationType = LocationType.Freezer, Quantity = 10 };
            var shipping = new StockBalance { Id = Guid.NewGuid(), ProductId = productId, LocationType = LocationType.Shipping, Quantity = 1 };

            _stockRepo.SetupSequence(r => r.Get(
                        It.IsAny<Expression<Func<StockBalance, StockBalance>>>(),
                        It.IsAny<Expression<Func<StockBalance, bool>>>(),
                        It.IsAny<Func<IQueryable<StockBalance>, IOrderedQueryable<StockBalance>>>(),
                        It.IsAny<Func<IQueryable<StockBalance>, IIncludableQueryable<StockBalance, object>>>()
                        ))
                        .Returns(shelves)   // 1st GetOrCreate -> Shelves
                        .Returns(freezer)   // 2nd -> Freezer
                        .Returns(shipping); // 3rd -> Shipping

            _sut.MoveToShipping(productId, 5);

            Assert.Equal(0, shelves.Quantity);      // 3 taken
            Assert.Equal(8, freezer.Quantity);      // 2 taken
            Assert.Equal(6, shipping.Quantity);     // +5 to existing 1

            _stockRepo.Verify(r => r.Update(shelves), Times.Once);
            _stockRepo.Verify(r => r.Update(freezer), Times.Once);
            _stockRepo.Verify(r => r.Update(shipping), Times.Once);
        }

        [Fact]
        public void MoveFromShippingToStorage_WhenNotEnoughInShipping_ShouldThrow()
        {
            var productId = Guid.NewGuid();
            var shipping = new StockBalance { Id = Guid.NewGuid(), ProductId = productId, LocationType = LocationType.Shipping, Quantity = 1 };
            var shelves = new StockBalance { Id = Guid.NewGuid(), ProductId = productId, LocationType = LocationType.Shelves, Quantity = 0 };

            SetupGetOrCreate(productId, LocationType.Shipping, shipping);
            SetupGetOrCreate(productId, LocationType.Shelves, shelves);

            var ex = Assert.Throws<Exception>(() => _sut.MoveFromShippingToStorage(productId, 5));
            Assert.Equal("Not enough stock in Shipping.", ex.Message);

            _stockRepo.Verify(r => r.Update(It.IsAny<StockBalance>()), Times.Never);
        }

        [Fact]
        public void MoveFromShippingToStorage_WhenValid_ShouldDecreaseShipping_IncreaseShelves_AndUpdateTwice()
        {
            var productId = Guid.NewGuid();
            var shipping = new StockBalance { Id = Guid.NewGuid(), ProductId = productId, LocationType = LocationType.Shipping, Quantity = 10 };
            var shelves = new StockBalance { Id = Guid.NewGuid(), ProductId = productId, LocationType = LocationType.Shelves, Quantity = 2 };

            _stockRepo.SetupSequence(r => r.Get(
                        It.IsAny<Expression<Func<StockBalance, StockBalance>>>(),
                        It.IsAny<Expression<Func<StockBalance, bool>>>(),
                        It.IsAny<Func<IQueryable<StockBalance>, IOrderedQueryable<StockBalance>>>(),
                        It.IsAny<Func<IQueryable<StockBalance>, IIncludableQueryable<StockBalance, object>>>()
                        ))
                        .Returns(shipping)   // 1st GetOrCreate -> Shipping
                        .Returns(shelves); // 2nd -> Shelves

            _sut.MoveFromShippingToStorage(productId, 4);

            Assert.Equal(6, shipping.Quantity);
            Assert.Equal(6, shelves.Quantity);

            _stockRepo.Verify(r => r.Update(shipping), Times.Once);
            _stockRepo.Verify(r => r.Update(shelves), Times.Once);
        }

        // PutAway


        [Fact]
        public void PutAway_WhenTargetIsInvalid_ShouldThrow()
        {
            var productId = Guid.NewGuid();

            var ex = Assert.Throws<Exception>(() => _sut.PutAway(productId, 1, LocationType.Shipping));
            Assert.Equal("PutAway target must be Shelves or Freezer.", ex.Message);
        }

        [Fact]
        public void PutAway_WhenNotEnoughInReceiving_ShouldThrow_AndNotUpdate()
        {
            var productId = Guid.NewGuid();
            var receiving = new StockBalance { Id = Guid.NewGuid(), ProductId = productId, LocationType = LocationType.Receiving, Quantity = 1 };
            var shelves = new StockBalance { Id = Guid.NewGuid(), ProductId = productId, LocationType = LocationType.Shelves, Quantity = 0 };

            SetupGetOrCreate(productId, LocationType.Receiving, receiving);
            SetupGetOrCreate(productId, LocationType.Shelves, shelves);

            var ex = Assert.Throws<Exception>(() => _sut.PutAway(productId, 5, LocationType.Shelves));
            Assert.Equal("Not enough stock in Receiving.", ex.Message);

            _stockRepo.Verify(r => r.Update(It.IsAny<StockBalance>()), Times.Never);
        }

        [Fact]
        public void PutAway_WhenValid_ShouldMoveFromReceivingToTarget_AndUpdateTwice()
        {
            var productId = Guid.NewGuid();
            var receiving = new StockBalance { Id = Guid.NewGuid(), ProductId = productId, LocationType = LocationType.Receiving, Quantity = 10 };
            var shelves = new StockBalance { Id = Guid.NewGuid(), ProductId = productId, LocationType = LocationType.Shelves, Quantity = 3 };

            _stockRepo.SetupSequence(r => r.Get(
                        It.IsAny<Expression<Func<StockBalance, StockBalance>>>(),
                        It.IsAny<Expression<Func<StockBalance, bool>>>(),
                        It.IsAny<Func<IQueryable<StockBalance>, IOrderedQueryable<StockBalance>>>(),
                        It.IsAny<Func<IQueryable<StockBalance>, IIncludableQueryable<StockBalance, object>>>()
                        ))
                        .Returns(receiving)   // 1st GetOrCreate -> Receiving
                        .Returns(shelves);   // 2nd -> Shelves

            _sut.PutAway(productId, 4, LocationType.Shelves);

            Assert.Equal(6, receiving.Quantity);
            Assert.Equal(7, shelves.Quantity);

            _stockRepo.Verify(r => r.Update(receiving), Times.Once);
            _stockRepo.Verify(r => r.Update(shelves), Times.Once);
        }

        // ConsumeFromShipping

        [Fact]
        public void ConsumeFromShipping_WhenNoShippingRecord_ShouldThrow()
        {
            _stockRepo.Setup(r => r.Get(
                    It.IsAny<Expression<Func<StockBalance, StockBalance>>>(),
                    It.IsAny<Expression<Func<StockBalance, bool>>>(),
                    It.IsAny<Func<IQueryable<StockBalance>, IOrderedQueryable<StockBalance>>>(),
                    It.IsAny<Func<IQueryable<StockBalance>, IIncludableQueryable<StockBalance, object>>>()
                ))
                .Returns((StockBalance?)null);

            var ex = Assert.Throws<Exception>(() => _sut.ConsumeFromShipping(Guid.NewGuid(), 1));
            Assert.Equal("No stock in Shipping for this product.", ex.Message);
        }

        [Fact]
        public void ConsumeFromShipping_WhenQuantityBecomesZero_ShouldDelete()
        {
            var productId = Guid.NewGuid();
            var shipping = new StockBalance
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                LocationType = LocationType.Shipping,
                Quantity = 3
            };

            
            _stockRepo.Setup(r => r.Get(
                    It.IsAny<Expression<Func<StockBalance, StockBalance>>>(),
                    It.IsAny<Expression<Func<StockBalance, bool>>>(),
                    It.IsAny<Func<IQueryable<StockBalance>, IOrderedQueryable<StockBalance>>>(),
                    It.IsAny<Func<IQueryable<StockBalance>, IIncludableQueryable<StockBalance, object>>>()
                ))
                .Returns(shipping);

            _sut.ConsumeFromShipping(productId, 3);

            Assert.Equal(0, shipping.Quantity);
            _stockRepo.Verify(r => r.Delete(shipping), Times.Once);
            _stockRepo.Verify(r => r.Update(It.IsAny<StockBalance>()), Times.Never);
        }

        [Fact]
        public void ConsumeFromShipping_WhenQuantityRemainsPositive_ShouldUpdate()
        {
            var productId = Guid.NewGuid();
            var shipping = new StockBalance
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                LocationType = LocationType.Shipping,
                Quantity = 10
            };

            _stockRepo.Setup(r => r.Get(
                    It.IsAny<Expression<Func<StockBalance, StockBalance>>>(),
                    It.IsAny<Expression<Func<StockBalance, bool>>>(),
                    It.IsAny<Func<IQueryable<StockBalance>, IOrderedQueryable<StockBalance>>>(),
                    It.IsAny<Func<IQueryable<StockBalance>, IIncludableQueryable<StockBalance, object>>>()
                ))
                .Returns(shipping);

            _sut.ConsumeFromShipping(productId, 4);

            Assert.Equal(6, shipping.Quantity);
            _stockRepo.Verify(r => r.Update(shipping), Times.Once);
            _stockRepo.Verify(r => r.Delete(It.IsAny<StockBalance>()), Times.Never);
        }


        private void SetupGetOrCreate(Guid productId, LocationType type, StockBalance existing)
        {
            _stockRepo.Setup(r => r.Get(
                    It.IsAny<Expression<Func<StockBalance, StockBalance>>>(),
                    It.IsAny<Expression<Func<StockBalance, bool>>>(),
                    It.IsAny<Func<IQueryable<StockBalance>, IOrderedQueryable<StockBalance>>>(),
                    It.IsAny<Func<IQueryable<StockBalance>, IIncludableQueryable<StockBalance, object>>>()
                ))
                .Returns(existing);
        }

        
    }
}
