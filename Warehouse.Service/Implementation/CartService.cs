using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Warehouse.Domain.Domain;
using Warehouse.Repository.Interface;
using Warehouse.Service.Interface;

namespace Warehouse.Service.Implementation
{
    public class CartService : ICartService
    {
        private readonly IRepository<ShoppingCart> _cartRepo;
        private readonly IRepository<ProductInShoppingCart> _itemRepo;
        private readonly IRepository<Product> _productRepo;

        private readonly IRepository<CustomerOrder> _orderRepo;
        private readonly IRepository<ProductInOrder> _orderLineRepo;

        private readonly IInventoryService _inventory;

        public CartService(
            IRepository<ShoppingCart> cartRepo,
            IRepository<ProductInShoppingCart> itemRepo,
            IRepository<Product> productRepo,
            IRepository<CustomerOrder> orderRepo,
            IRepository<ProductInOrder> orderLineRepo,
            IInventoryService inventory)
        {
            _cartRepo = cartRepo;
            _itemRepo = itemRepo;
            _productRepo = productRepo;
            _orderRepo = orderRepo;
            _orderLineRepo = orderLineRepo;
            _inventory = inventory;
        }

        public ShoppingCart GetOrCreateCart(string customerId)
        {
            var cart = _cartRepo.Get(
                selector: x => x,
                predicate: c => c.UserId == customerId,
                include: q => q.Include(c => c.Items).ThenInclude(i => i.Product)
            );

            if (cart != null) return cart;

            cart = new ShoppingCart
            {
                Id = Guid.NewGuid(),
                UserId = customerId
            };

            _cartRepo.Insert(cart);

            return _cartRepo.Get(
                selector: x => x,
                predicate: c => c.Id == cart.Id,
                include: q => q.Include(c => c.Items).ThenInclude(i => i.Product)
            )!;
        }

        public ShoppingCart GetCart(string customerId)
        {
            return _cartRepo.Get(
                selector: x => x,
                predicate: c => c.UserId == customerId,
                include: q => q.Include(c => c.Items).ThenInclude(i => i.Product)
            ) ?? throw new Exception("Cart not found.");
        }

        public void AddToCart(string customerId, Guid productId, int quantity)
        {
            if (quantity <= 0) throw new Exception("Quantity must be > 0.");

            var cart = GetOrCreateCart(customerId);

            var product = _productRepo.Get(selector: x => x, predicate: p => p.Id == productId)
                          ?? throw new Exception("Product not found.");

            var existingItem = _itemRepo.Get(
                selector: x => x,
                predicate: i => i.ShoppingCartId == cart.Id && i.ProductId == productId
            );

            if (existingItem == null)
            {
                _itemRepo.Insert(new ProductInShoppingCart
                {
                    Id = Guid.NewGuid(),
                    ShoppingCartId = cart.Id,
                    ProductId = product.Id,
                    Quantity = quantity
                });
            }
            else
            {
                existingItem.Quantity += quantity;
                _itemRepo.Update(existingItem);
            }
        }

        public void UpdateItemQuantity(string customerId, Guid productId, int quantity)
        {
            var cart = GetOrCreateCart(customerId);

            var item = _itemRepo.Get(
                selector: x => x,
                predicate: i => i.ShoppingCartId == cart.Id && i.ProductId == productId
            ) ?? throw new Exception("Cart item not found.");

            if (quantity <= 0)
            {
                _itemRepo.Delete(item);
                return;
            }

            item.Quantity = quantity;
            _itemRepo.Update(item);
        }

        public void RemoveFromCart(string customerId, Guid productId)
        {
            var cart = GetOrCreateCart(customerId);

            var item = _itemRepo.Get(
                selector: x => x,
                predicate: i => i.ShoppingCartId == cart.Id && i.ProductId == productId
            );

            if (item != null) _itemRepo.Delete(item);
        }

        public void ClearCart(string customerId)
        {
            var cart = GetOrCreateCart(customerId);

            var items = _itemRepo.GetAll(
                selector: x => x,
                predicate: i => i.ShoppingCartId == cart.Id
            ).ToList();

            foreach (var item in items)
                _itemRepo.Delete(item);
        }

        public CustomerOrder Checkout(string customerId)
        {
            var cart = GetCart(customerId);

            var items = cart.Items?.ToList() ?? new List<ProductInShoppingCart>();
            if (!items.Any()) throw new Exception("Cart is empty.");

            // 1) Reserve/move inventory for each line
            foreach (var item in items)
            {
                _inventory.MoveToShipping(item.ProductId, item.Quantity);
            }

            // 2) Create order
            var order = new CustomerOrder
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                DateCreated = DateTime.UtcNow,
                Status = Warehouse.Domain.Domain.Enums.OrderStatus.Ordered
            };

            _orderRepo.Insert(order);

            // 3) Create lines
            foreach (var item in items)
            {
                _orderLineRepo.Insert(new ProductInOrder
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity
                });
            }

            // 4) Clear cart
            ClearCart(customerId);

            return order;
        }
    }
}

