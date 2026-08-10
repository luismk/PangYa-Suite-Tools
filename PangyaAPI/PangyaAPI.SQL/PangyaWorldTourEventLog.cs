using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

/// <summary>
/// envia o presente sim ou nao
/// </summary>
public partial class PangyaWorldTourEventLog
{
    public int Index { get; set; }

    public int Uid { get; set; }

    public int SendGift { get; set; }

    public DateTime? FinishData { get; set; }
}
