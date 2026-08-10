using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class ShopProduct
{
    public int Id { get; set; }

    public string Name { get; set; }

    public decimal Amount { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Category { get; set; }

    public virtual ICollection<ShopProductItem> ShopProductItems { get; set; } = new List<ShopProductItem>();

    public virtual ICollection<ShopPurchase> ShopPurchases { get; set; } = new List<ShopPurchase>();
}
