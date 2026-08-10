using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaGuildMarkLog
{
    public long Index { get; set; }

    public int MarkIdx { get; set; }

    public DateTime RegDate { get; set; }
}
