using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaShopGift
{
    public int GiftId { get; set; }

    public string GiftName { get; set; }

    public string ItemName { get; set; }

    public int ItemTypeid { get; set; }

    public int ItemQntd { get; set; }

    public int ItemQntdTime { get; set; }

    public int ItemPeriod { get; set; }

    public int RequiredPrice { get; set; }

    public DateTime? EndDate { get; set; }

    public DateTime? RegDate { get; set; }
}
