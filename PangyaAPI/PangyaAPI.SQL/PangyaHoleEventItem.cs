using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaHoleEventItem
{
    public int? EventId { get; set; }

    public int HoleCount { get; set; }

    public string ItemName { get; set; }

    public int ItemTypeid { get; set; }

    public int ItemQntd { get; set; }

    public int ItemQntdTime { get; set; }

    public string EventDescription { get; set; }
}
