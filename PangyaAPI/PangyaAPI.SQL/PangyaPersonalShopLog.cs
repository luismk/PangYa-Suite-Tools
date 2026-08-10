using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaPersonalShopLog
{
    public long Index { get; set; }

    public int PlayerSellUid { get; set; }

    public int PlayerBuyUid { get; set; }

    public int ItemTypeid { get; set; }

    public int ItemIdSell { get; set; }

    public int ItemIdBuy { get; set; }

    public int ItemQntd { get; set; }

    public long ItemPang { get; set; }

    public long TotalPang { get; set; }

    public DateTime RegDate { get; set; }
}
