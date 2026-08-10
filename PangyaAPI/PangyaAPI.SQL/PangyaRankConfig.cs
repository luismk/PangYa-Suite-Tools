using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaRankConfig
{
    public long Index { get; set; }

    public int RefreshTimeH { get; set; }

    public DateTime? RegDate { get; set; }
}
