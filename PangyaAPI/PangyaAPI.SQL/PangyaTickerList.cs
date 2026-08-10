using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaTickerList
{
    public long TickerId { get; set; }

    public string Message { get; set; }

    public string Nick { get; set; }

    public int ReplayCount { get; set; }

    public int RefreshTime { get; set; }
}
