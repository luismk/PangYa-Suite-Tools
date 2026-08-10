using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaShopGiftLog
{
    public int Index { get; set; }

    public int Uid { get; set; }

    public int GiftId { get; set; }

    public int ItemTypeid { get; set; }

    public int ItemQntd { get; set; }

    public DateTime? RegDate { get; set; }
}
