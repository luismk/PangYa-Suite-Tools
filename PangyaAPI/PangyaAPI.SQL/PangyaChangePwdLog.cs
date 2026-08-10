using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaChangePwdLog
{
    public int Uid { get; set; }

    public DateTime LastChange { get; set; }

    public DateTime ChangeDate { get; set; }

    public int Count { get; set; }
}
