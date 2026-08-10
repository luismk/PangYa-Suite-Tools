using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaChangeEmailLog
{
    public long Index { get; set; }

    public int Uid { get; set; }

    public string EmailOld { get; set; }

    public string EmailNew { get; set; }

    public DateTime ChangeTime { get; set; }
}
