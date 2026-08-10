using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaChangeNicknameLog
{
    public long Index { get; set; }

    public int Uid { get; set; }

    public string Nickname { get; set; }

    public DateTime ChangeTime { get; set; }
}
