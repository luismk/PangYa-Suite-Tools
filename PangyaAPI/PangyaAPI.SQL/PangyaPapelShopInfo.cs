using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaPapelShopInfo
{
    public long Index { get; set; }

    public int Uid { get; set; }

    public short CurrentCnt { get; set; }

    public short RemainCnt { get; set; }

    public short LimitCnt { get; set; }

    public DateTime? LastUpdate { get; set; }
}
