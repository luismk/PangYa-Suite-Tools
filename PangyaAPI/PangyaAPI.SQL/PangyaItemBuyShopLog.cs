using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaItemBuyShopLog
{
    public long Index { get; set; }

    public int Uid { get; set; }

    public int ItemId { get; set; }

    public int ItemTypeid { get; set; }

    public int ItemTime { get; set; }

    public int ItemType { get; set; }

    public int ItemQntd { get; set; }

    public long ItemPang { get; set; }

    public long ItemCookie { get; set; }

    public DateTime RegDate { get; set; }
}
