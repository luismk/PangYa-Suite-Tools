using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaLoginReward
{
    public long Index { get; set; }

    public string Name { get; set; }

    public byte Type { get; set; }

    public int DaysToGift { get; set; }

    public int NTimesGift { get; set; }

    public int ItemTypeid { get; set; }

    public int ItemQntd { get; set; }

    public int ItemQntdTime { get; set; }

    public byte IsEnd { get; set; }

    public DateTime? EndDate { get; set; }

    public DateTime RegDate { get; set; }
}
