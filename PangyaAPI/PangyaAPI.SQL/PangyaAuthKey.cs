using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaAuthKey
{
    public long Index { get; set; }

    public int? ServerUid { get; set; }

    public string Key { get; set; }

    public byte? Valid { get; set; }
}
