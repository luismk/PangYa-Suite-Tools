using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaPointEvent
{
    public int Uid { get; set; }

    public long Points { get; set; }

    public long LimitBuy { get; set; }

    public DateTime? LastDay { get; set; }

    public DateTime? RegDate { get; set; }
}
