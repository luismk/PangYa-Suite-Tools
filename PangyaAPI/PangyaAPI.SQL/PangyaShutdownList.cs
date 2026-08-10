using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaShutdownList
{
    public long Id { get; set; }

    public DateTime? DateShutdown { get; set; }

    public int ReplayCount { get; set; }

    public int RefreshTime { get; set; }
}
