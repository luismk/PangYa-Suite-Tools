using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaGoldenTimeInfo
{
    public int Index { get; set; }

    public byte Type { get; set; }

    public DateOnly Begin { get; set; }

    public DateOnly? End { get; set; }

    public int Rate { get; set; }

    public byte IsEnd { get; set; }

    public DateTime RegDate { get; set; }
}
