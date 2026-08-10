using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaLoginRewardPlayer
{
    public long Index { get; set; }

    public long LoginRewardId { get; set; }

    public int Uid { get; set; }

    public int CountDays { get; set; }

    public int CountSeq { get; set; }

    public byte IsClear { get; set; }

    public DateTime UpdateDate { get; set; }

    public DateTime RegDate { get; set; }
}
