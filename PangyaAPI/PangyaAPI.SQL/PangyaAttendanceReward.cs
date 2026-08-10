using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaAttendanceReward
{
    public int Uid { get; set; }

    public int Counter { get; set; }

    public int ItemTypeidNow { get; set; }

    public int ItemQntdNow { get; set; }

    public int ItemTypeidAfter { get; set; }

    public int ItemQntdAfter { get; set; }

    public DateTime? LastLogin { get; set; }
}
