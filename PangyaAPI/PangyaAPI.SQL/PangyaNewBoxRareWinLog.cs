using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaNewBoxRareWinLog
{
    public long Index { get; set; }

    public int Uid { get; set; }

    public int BoxTypeid { get; set; }

    public int ItemTypeid { get; set; }

    public int Qntd { get; set; }

    public byte Raridade { get; set; }

    public DateTime WinDate { get; set; }
}
