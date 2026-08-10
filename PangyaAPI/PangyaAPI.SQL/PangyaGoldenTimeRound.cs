using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaGoldenTimeRound
{
    public long Index { get; set; }

    public long GoldenTimeId { get; set; }

    public TimeOnly Time { get; set; }

    public DateTime RegDate { get; set; }
}
