using System;
using System.Collections.Generic;
using System.Text;
using Warehouse.Domain.Domain;

namespace Warehouse.Service.Interface
{
    public interface ICartService
    {
        ShoppingCart GetOrCreateCart(string customerId);
        ShoppingCart GetCart(string customerId);

        void AddToCart(string customerId, Guid productId, int quantity);
        void UpdateItemQuantity(string customerId, Guid productId, int quantity);
        void RemoveFromCart(string customerId, Guid productId);
        void ClearCart(string customerId);

        CustomerOrder Checkout(string customerId);
    }
}

