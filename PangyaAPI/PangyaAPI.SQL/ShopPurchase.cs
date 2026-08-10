using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class ShopPurchase
{
    public long Id { get; set; }

    public string AccountId { get; set; }

    public int ShopProductId { get; set; }

    public decimal Amount { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Status { get; set; }

    public string PaymentLink { get; set; }

    public virtual Account Account { get; set; }

    public virtual ShopProduct ShopProduct { get; set; }
}
