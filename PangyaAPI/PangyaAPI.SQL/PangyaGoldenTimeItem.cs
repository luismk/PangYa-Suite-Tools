using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaGoldenTimeItem
{
    public long Index { get; set; }

    public long GoldenTimeId { get; set; }

    public int Typeid { get; set; }

    public int Qntd { get; set; }

    public int QntdTime { get; set; }

    public int Rate { get; set; }

    public DateTime RegDate { get; set; }
}
