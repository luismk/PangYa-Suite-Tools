using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaLuciaAttendanceRewardLog
{
    public long Index { get; set; }

    public int Uid { get; set; }

    public int MsgId { get; set; }

    public string Error { get; set; }

    public DateTime RegDate { get; set; }
}
