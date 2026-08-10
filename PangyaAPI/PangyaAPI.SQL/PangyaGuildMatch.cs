using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaGuildMatch
{
    public long Index { get; set; }

    public int Guild1Uid { get; set; }

    public int Guild2Uid { get; set; }

    public int Guild1Point { get; set; }

    public int Guild2Point { get; set; }

    public int Guild1Pang { get; set; }

    public int Guild2Pang { get; set; }

    public DateTime RegDate { get; set; }
}
