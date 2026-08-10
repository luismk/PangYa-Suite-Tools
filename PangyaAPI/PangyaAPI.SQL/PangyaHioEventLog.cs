using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaHioEventLog
{
    public int Id { get; set; }

    public int Uid { get; set; }

    public int ItemTypeid { get; set; }

    public int HioCount { get; set; }

    public DateTime? ReceivedAt { get; set; }

    public int Status { get; set; }
}
