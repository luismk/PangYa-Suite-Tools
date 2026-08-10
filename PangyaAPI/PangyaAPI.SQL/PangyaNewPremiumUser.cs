using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaNewPremiumUser
{
    public long Index { get; set; }

    public int Uid { get; set; }

    public short LimitCnt { get; set; }

    public DateTime? Start { get; set; }

    public DateTime? End { get; set; }

    public short Received { get; set; }

    public DateTime? LastUpdate { get; set; }
}
