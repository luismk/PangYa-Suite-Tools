using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaHioEventItem
{
    public int Idx { get; set; }

    public int HioCount { get; set; }

    public string ItemName { get; set; }

    public int ItemTypeid { get; set; }

    public int ItemQntd { get; set; }

    public int ItemQntdTime { get; set; }

    public string EventDescription { get; set; }

    public DateTime? EndEvent { get; set; }

    public DateTime? RegDate { get; set; }
}
