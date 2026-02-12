using System;
using System.Collections.Generic;
using System.Linq;
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
    public class CartServiceTests
    {
        private readonly Mock<IRepository<ShoppingCart>> _cartRepo = new();
        private readonly Mock<IRepository<ProductInShoppingCart>> _itemRepo = new();
        private readonly Mock<IRepository<Product>> _productRepo = new();
        private readonly Mock<IRepository<CustomerOrder>> _orderRepo = new();
        private readonly Mock<IRepository<ProductInOrder>> _orderLineRepo = new();
        private readonly Mock<IInventoryService> _inventory = new();

        private readonly CartService _sut;

        public CartServiceTests()
        {
            _sut = new CartService(
                _cartRepo.Object,
                _itemRepo.Object,
                _productRepo.Object,
                _orderRepo.Object,
                _orderLineRepo.Object,
                _inventory.Object
            );
        }


        // GetCart / GetOrCreateCart

        [Fact]
        public void GetCart_WhenCartNotFound_ShouldThrow()
        {
            _cartRepo.Setup(r => r.Get(
                    It.IsAny<Expression<Func<ShoppingCart, ShoppingCart>>>(),
                    It.IsAny<Expression<Func<ShoppingCart, bool>>>(),
                    It.IsAny<Func<IQueryable<ShoppingCart>, IOrderedQueryable<ShoppingCart>>>(),
                    It.IsAny<Func<IQueryable<ShoppingCart>, IIncludableQueryable<ShoppingCart, object>>>()
                ))
                .Returns((ShoppingCart?)null);

            var ex = Assert.Throws<Exception>(() => _sut.GetCart("cust-1"));
            Assert.Equal("Cart not found.", ex.Message);
        }

        [Fact]
        public void GetOrCreateCart_WhenCartExists_ShouldReturnExisting_AndNotInsert()
        {
            var existing = new ShoppingCart
            {
                Id = Guid.NewGuid(),
                UserId = "cust-1",
                Items = new List<ProductInShoppingCart>()
            };

            _cartRepo.Setup(r => r.Get(
                    It.IsAny<Expression<Func<ShoppingCart, ShoppingCart>>>(),
                    It.IsAny<Expression<Func<ShoppingCart, bool>>>(),
                    It.IsAny<Func<IQueryable<ShoppingCart>, IOrderedQueryable<ShoppingCart>>>(),
                    It.IsAny<Func<IQueryable<ShoppingCart>, IIncludableQueryable<ShoppingCart, object>>>()
                ))
                .Returns(existing);

            var result = _sut.GetOrCreateCart("cust-1");

            Assert.Same(existing, result);
            _cartRepo.Verify(r => r.Insert(It.IsAny<ShoppingCart>()), Times.Never);
        }

        [Fact]
        public void GetOrCreateCart_WhenCartDoesNotExist_ShouldInsert_AndReturnReloadedCart()
        {
            var createdId = Guid.NewGuid();

            
            _cartRepo.SetupSequence(r => r.Get(
                    It.IsAny<Expression<Func<ShoppingCart, ShoppingCart>>>(),
                    It.IsAny<Expression<Func<ShoppingCart, bool>>>(),
                    It.IsAny<Func<IQueryable<ShoppingCart>, IOrderedQueryable<ShoppingCart>>>(),
                    It.IsAny<Func<IQueryable<ShoppingCart>, IIncludableQueryable<ShoppingCart, object>>>()
                ))
                .Returns((ShoppingCart?)null)
                .Returns(new ShoppingCart
                {
                    Id = createdId,
                    UserId = "cust-1",
                    Items = new List<ProductInShoppingCart>()
                });

            
            ShoppingCart? inserted = null;
            _cartRepo.Setup(r => r.Insert(It.IsAny<ShoppingCart>()))
                     .Callback<ShoppingCart>(c =>
                     {
                         inserted = c;
                         c.Id = createdId;
                     })
                     .Returns<ShoppingCart>(c => c);

            var result = _sut.GetOrCreateCart("cust-1");

            Assert.NotNull(inserted);
            Assert.Equal("cust-1", inserted!.UserId);
            _cartRepo.Verify(r => r.Insert(It.IsAny<ShoppingCart>()), Times.Once);

            Assert.Equal(createdId, result.Id);
            Assert.Equal("cust-1", result.UserId);
        }


        // AddToCart


        [Fact]
        public void AddToCart_WhenQuantityIsZeroOrLess_ShouldThrow_AndDoNothing()
        {
            var ex = Assert.Throws<Exception>(() => _sut.AddToCart("cust-1", Guid.NewGuid(), 0));
            Assert.Equal("Quantity must be > 0.", ex.Message);

            _itemRepo.Verify(r => r.Insert(It.IsAny<ProductInShoppingCart>()), Times.Never);
            _itemRepo.Verify(r => r.Update(It.IsAny<ProductInShoppingCart>()), Times.Never);
        }

        [Fact]
        public void AddToCart_WhenProductNotFound_ShouldThrow_AndNotInsertItem()
        {
            SetupGetOrCreateCartReturns(new ShoppingCart { Id = Guid.NewGuid(), UserId = "cust-1" });

            _productRepo.Setup(r => r.Get(
                    It.IsAny<Expression<Func<Product, Product>>>(),
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<Func<IQueryable<Product>, IOrderedQueryable<Product>>>(),
                    It.IsAny<Func<IQueryable<Product>, IIncludableQueryable<Product, object>>>()
                ))
                .Returns((Product?)null);

            var ex = Assert.Throws<Exception>(() => _sut.AddToCart("cust-1", Guid.NewGuid(), 1));
            Assert.Equal("Product not found.", ex.Message);

            _itemRepo.Verify(r => r.Insert(It.IsAny<ProductInShoppingCart>()), Times.Never);
            _itemRepo.Verify(r => r.Update(It.IsAny<ProductInShoppingCart>()), Times.Never);
        }

        [Fact]
        public void AddToCart_WhenItemDoesNotExist_ShouldInsertNewItem()
        {
            var cart = new ShoppingCart { Id = Guid.NewGuid(), UserId = "cust-1" };
            var productId = Guid.NewGuid();
           
            SetupGetOrCreateCartReturns(cart);

            _productRepo.Setup(r => r.Get(
                    It.IsAny<Expression<Func<Product, Product>>>(),
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<Func<IQueryable<Product>, IOrderedQueryable<Product>>>(),
                    It.IsAny<Func<IQueryable<Product>, IIncludableQueryable<Product, object>>>()
                ))
                .Returns(new Product { Id = productId, Name = "P", CategoryId = Guid.NewGuid() });

            _itemRepo.Setup(r => r.Get(
                    It.IsAny<Expression<Func<ProductInShoppingCart, ProductInShoppingCart>>>(),
                    It.IsAny<Expression<Func<ProductInShoppingCart, bool>>>(),
                    It.IsAny<Func<IQueryable<ProductInShoppingCart>, IOrderedQueryable<ProductInShoppingCart>>>(),
                    It.IsAny<Func<IQueryable<ProductInShoppingCart>, IIncludableQueryable<ProductInShoppingCart, object>>>()
                ))
                .Returns((ProductInShoppingCart?)null);

            _sut.AddToCart("cust-1", productId, 3);

            _itemRepo.Verify(r => r.Insert(It.Is<ProductInShoppingCart>(i =>
                i.ShoppingCartId == cart.Id &&
                i.ProductId == productId &&
                i.Quantity == 3
            )), Times.Once);

            _itemRepo.Verify(r => r.Update(It.IsAny<ProductInShoppingCart>()), Times.Never);
        }

        [Fact]
        public void AddToCart_WhenItemExists_ShouldIncreaseQuantity_AndUpdate()
        {
            var cart = new ShoppingCart { Id = Guid.NewGuid(), UserId = "cust-1" };
            var productId = Guid.NewGuid();

            SetupGetOrCreateCartReturns(cart);

            _productRepo.Setup(r => r.Get(
                    It.IsAny<Expression<Func<Product, Product>>>(),
                    It.IsAny<Expression<Func<Product, bool>>>(),
                    It.IsAny<Func<IQueryable<Product>, IOrderedQueryable<Product>>>(),
                    It.IsAny<Func<IQueryable<Product>, IIncludableQueryable<Product, object>>>()
                ))
                .Returns(new Product { Id = productId, Name = "Product", CategoryId = Guid.NewGuid() });

            var existing = new ProductInShoppingCart
            {
                Id = Guid.NewGuid(),
                ShoppingCartId = cart.Id,
                ProductId = productId,
                Quantity = 2
            };

            _itemRepo.Setup(r => r.Get(
                    It.IsAny<Expression<Func<ProductInShoppingCart, ProductInShoppingCart>>>(),
                    It.IsAny<Expression<Func<ProductInShoppingCart, bool>>>(),
                    It.IsAny<Func<IQueryable<ProductInShoppingCart>, IOrderedQueryable<ProductInShoppingCart>>>(),
                    It.IsAny<Func<IQueryable<ProductInShoppingCart>, IIncludableQueryable<ProductInShoppingCart, object>>>()
                ))
                .Returns(existing);

            _sut.AddToCart("cust-1", productId, 5);

            Assert.Equal(7, existing.Quantity);
            _itemRepo.Verify(r => r.Update(existing), Times.Once);
            _itemRepo.Verify(r => r.Insert(It.IsAny<ProductInShoppingCart>()), Times.Never);
        }


        // UpdateItemQuantity / Remove / Clear


        [Fact]
        public void UpdateItemQuantity_WhenItemNotFound_ShouldThrow()
        {
            var cart = new ShoppingCart { Id = Guid.NewGuid(), UserId = "cust-1" };
            var productId = Guid.NewGuid();

            SetupGetOrCreateCartReturns(cart);

            _itemRepo.Setup(r => r.Get(
                    It.IsAny<Expression<Func<ProductInShoppingCart, ProductInShoppingCart>>>(),
                    It.IsAny<Expression<Func<ProductInShoppingCart, bool>>>(),
                    It.IsAny<Func<IQueryable<ProductInShoppingCart>, IOrderedQueryable<ProductInShoppingCart>>>(),
                    It.IsAny<Func<IQueryable<ProductInShoppingCart>, IIncludableQueryable<ProductInShoppingCart, object>>>()
                ))
                .Returns((ProductInShoppingCart?)null);

            var ex = Assert.Throws<Exception>(() => _sut.UpdateItemQuantity("cust-1", productId, 2));
            Assert.Equal("Cart item not found.", ex.Message);
        }

        [Fact]
        public void UpdateItemQuantity_WhenQuantityIsZeroOrLess_ShouldDeleteItem()
        {
            var cart = new ShoppingCart { Id = Guid.NewGuid(), UserId = "cust-1" };
            var productId = Guid.NewGuid();

            SetupGetOrCreateCartReturns(cart);

            var item = new ProductInShoppingCart
            {
                Id = Guid.NewGuid(),
                ShoppingCartId = cart.Id,
                ProductId = productId,
                Quantity = 10
            };

            _itemRepo.Setup(r => r.Get(
                    It.IsAny<Expression<Func<ProductInShoppingCart, ProductInShoppingCart>>>(),
                    It.IsAny<Expression<Func<ProductInShoppingCart, bool>>>(),
                    It.IsAny<Func<IQueryable<ProductInShoppingCart>, IOrderedQueryable<ProductInShoppingCart>>>(),
                    It.IsAny<Func<IQueryable<ProductInShoppingCart>, IIncludableQueryable<ProductInShoppingCart, object>>>()
                ))
                .Returns(item);

            _sut.UpdateItemQuantity("cust-1", productId, 0);

            _itemRepo.Verify(r => r.Delete(item), Times.Once);
            _itemRepo.Verify(r => r.Update(It.IsAny<ProductInShoppingCart>()), Times.Never);
        }

        [Fact]
        public void RemoveFromCart_WhenItemNotFound_ShouldNotDelete()
        {
            var cart = new ShoppingCart { Id = Guid.NewGuid(), UserId = "cust-1" };
            var productId = Guid.NewGuid();

            SetupGetOrCreateCartReturns(cart);

            _itemRepo.Setup(r => r.Get(
                    It.IsAny<Expression<Func<ProductInShoppingCart, ProductInShoppingCart>>>(),
                    It.IsAny<Expression<Func<ProductInShoppingCart, bool>>>(),
                    It.IsAny<Func<IQueryable<ProductInShoppingCart>, IOrderedQueryable<ProductInShoppingCart>>>(),
                    It.IsAny<Func<IQueryable<ProductInShoppingCart>, IIncludableQueryable<ProductInShoppingCart, object>>>()
                ))
                .Returns((ProductInShoppingCart?)null);

            _sut.RemoveFromCart("cust-1", productId);

            _itemRepo.Verify(r => r.Delete(It.IsAny<ProductInShoppingCart>()), Times.Never);
        }

        [Fact]
        public void ClearCart_WhenCartHasItems_ShouldDeleteEachItem()
        {
            var cart = new ShoppingCart { Id = Guid.NewGuid(), UserId = "cust-1" };
            SetupGetOrCreateCartReturns(cart);

            var items = new List<ProductInShoppingCart>
            {
                new() { Id = Guid.NewGuid(), ShoppingCartId = cart.Id, ProductId = Guid.NewGuid(), Quantity = 1 },
                new() { Id = Guid.NewGuid(), ShoppingCartId = cart.Id, ProductId = Guid.NewGuid(), Quantity = 2 }
            };

            _itemRepo.Setup(r => r.GetAll(
                    It.IsAny<Expression<Func<ProductInShoppingCart, ProductInShoppingCart>>>(),
                    It.IsAny<Expression<Func<ProductInShoppingCart, bool>>>(),
                    It.IsAny<Func<IQueryable<ProductInShoppingCart>, IOrderedQueryable<ProductInShoppingCart>>>(),
                    It.IsAny<Func<IQueryable<ProductInShoppingCart>, IIncludableQueryable<ProductInShoppingCart, object>>>()
                ))
                .Returns(items);

            _sut.ClearCart("cust-1");

            _itemRepo.Verify(r => r.Delete(It.IsAny<ProductInShoppingCart>()), Times.Exactly(2));
        }


        // Checkout


        [Fact]
        public void Checkout_WhenCartIsEmpty_ShouldThrow_AndDoNoSideEffects()
        {
            
            var cart = new ShoppingCart { Id = Guid.NewGuid(), UserId = "cust-1", Items = new List<ProductInShoppingCart>() };

            _cartRepo.Setup(r => r.Get(
                    It.IsAny<Expression<Func<ShoppingCart, ShoppingCart>>>(),
                    It.IsAny<Expression<Func<ShoppingCart, bool>>>(),
                    It.IsAny<Func<IQueryable<ShoppingCart>, IOrderedQueryable<ShoppingCart>>>(),
                    It.IsAny<Func<IQueryable<ShoppingCart>, IIncludableQueryable<ShoppingCart, object>>>()
                ))
                .Returns(cart);

            var ex = Assert.Throws<Exception>(() => _sut.Checkout("cust-1"));
            Assert.Equal("Cart is empty.", ex.Message);

            _inventory.Verify(i => i.MoveToShipping(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
            _orderRepo.Verify(r => r.Insert(It.IsAny<CustomerOrder>()), Times.Never);
            _orderLineRepo.Verify(r => r.Insert(It.IsAny<ProductInOrder>()), Times.Never);
        }

        [Fact]
        public void Checkout_WhenValid_ShouldMoveInventory_CreateOrder_CreateLines_AndClearCart()
        {
            var customerId = "cust-1";
            var cartId = Guid.NewGuid();

            var items = new List<ProductInShoppingCart>
            {
                new() { Id = Guid.NewGuid(), ShoppingCartId = cartId, ProductId = Guid.NewGuid(), Quantity = 2 },
                new() { Id = Guid.NewGuid(), ShoppingCartId = cartId, ProductId = Guid.NewGuid(), Quantity = 1 }
            };

           
            var cart = new ShoppingCart { Id = cartId, UserId = customerId, Items = items };

            //_cartRepo.Setup(r => r.Get(
            //        It.IsAny<Expression<Func<ShoppingCart, ShoppingCart>>>(),
            //        It.IsAny<Expression<Func<ShoppingCart, bool>>>(),
            //        It.IsAny<Func<IQueryable<ShoppingCart>, IOrderedQueryable<ShoppingCart>>>(),
            //        It.IsAny<Func<IQueryable<ShoppingCart>, IIncludableQueryable<ShoppingCart, object>>>()
            //    ))
            //    .Returns(cart);

            
            SetupGetOrCreateCartReturns(cart);

            _itemRepo.Setup(r => r.GetAll(
                    It.IsAny<Expression<Func<ProductInShoppingCart, ProductInShoppingCart>>>(),
                    It.IsAny<Expression<Func<ProductInShoppingCart, bool>>>(),
                    It.IsAny<Func<IQueryable<ProductInShoppingCart>, IOrderedQueryable<ProductInShoppingCart>>>(),
                    It.IsAny<Func<IQueryable<ProductInShoppingCart>, IIncludableQueryable<ProductInShoppingCart, object>>>()
                ))
                .Returns(items);

            CustomerOrder? insertedOrder = null;
            _orderRepo.Setup(r => r.Insert(It.IsAny<CustomerOrder>()))
                      .Callback<CustomerOrder>(o => insertedOrder = o)
                      .Returns<CustomerOrder>(o => o);

            _sut.Checkout(customerId);

            
            _inventory.Verify(i => i.MoveToShipping(It.IsAny<Guid>(), It.IsAny<int>()), Times.Exactly(2));
            _inventory.Verify(i => i.MoveToShipping(items[0].ProductId, items[0].Quantity), Times.Once);
            _inventory.Verify(i => i.MoveToShipping(items[1].ProductId, items[1].Quantity), Times.Once);

            
            _orderRepo.Verify(r => r.Insert(It.IsAny<CustomerOrder>()), Times.Once);
            Assert.NotNull(insertedOrder);
            Assert.Equal(customerId, insertedOrder!.CustomerId);
            Assert.Equal(OrderStatus.Ordered, insertedOrder.Status);

            
            _orderLineRepo.Verify(r => r.Insert(It.Is<ProductInOrder>(l => l.OrderId == insertedOrder!.Id)), Times.Exactly(2));

            
            _itemRepo.Verify(r => r.Delete(It.IsAny<ProductInShoppingCart>()), Times.Exactly(2));
        }


        // Helpers


        private void SetupGetOrCreateCartReturns(ShoppingCart cart)
        {
            _cartRepo.Setup(r => r.Get(
                    It.IsAny<Expression<Func<ShoppingCart, ShoppingCart>>>(),
                    It.IsAny<Expression<Func<ShoppingCart, bool>>>(),
                    It.IsAny<Func<IQueryable<ShoppingCart>, IOrderedQueryable<ShoppingCart>>>(),
                    It.IsAny<Func<IQueryable<ShoppingCart>, IIncludableQueryable<ShoppingCart, object>>>()
                ))
                .Returns(cart);
        }
    }
}
