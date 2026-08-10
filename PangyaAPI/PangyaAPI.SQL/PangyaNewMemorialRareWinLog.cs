using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaNewMemorialRareWinLog
{
    public long Index { get; set; }

    public int Uid { get; set; }

    public int CoinTypeid { get; set; }

    public int ItemTypeid { get; set; }

    public int ItemQntd { get; set; }

    public int ItemRaridade { get; set; }

    public int ItemProbabilidade { get; set; }

    public DateTime? WinDate { get; set; }

    public int? MemorialNr { get; set; }
}
