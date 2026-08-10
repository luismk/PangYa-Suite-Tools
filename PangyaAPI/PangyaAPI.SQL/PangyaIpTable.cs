using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaIpTable
{
    public long Index { get; set; }

    public string Ip { get; set; }

    public string Mask { get; set; }

    public DateTime Date { get; set; }
}
