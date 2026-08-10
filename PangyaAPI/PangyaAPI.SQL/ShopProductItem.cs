using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class ShopProductItem
{
    public long Id { get; set; }

    public int ShopProductId { get; set; }

    public long? Cash { get; set; }

    public long? Pangs { get; set; }

    public string ItemId { get; set; }

    public DateTime CreatedAt { get; set; }

    public long? ItemQuantity { get; set; }

    public virtual ShopProduct ShopProduct { get; set; }
}
