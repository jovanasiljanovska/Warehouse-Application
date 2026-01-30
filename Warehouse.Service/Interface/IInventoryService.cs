using System;
using System.Collections.Generic;
using System.Text;
using Warehouse.Domain.Domain.Enums;
namespace Warehouse.Service.Interface;

public interface IInventoryService
{
    void MoveToShipping(Guid productId, int quantity);
    void MoveFromShippingToStorage(Guid productId, int quantity);

    void AddToReceiving(Guid productId, int quantity);
    void PutAway(Guid productId, int quantity, LocationType target);
    void SetInitialStock(Guid productId, int quantity, LocationType locationType);
    void ConsumeFromShipping(Guid productId, int quantity);


}

