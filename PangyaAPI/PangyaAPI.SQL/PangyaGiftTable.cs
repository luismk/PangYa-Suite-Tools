using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaGiftTable
{
    public int Uid { get; set; }

    public long MsgId { get; set; }

    public short Flag { get; set; }

    public string Fromid { get; set; }

    public string Message { get; set; }

    public DateTime Giftdate { get; set; }

    public DateTime? Enddate { get; set; }

    public int ContadorVista { get; set; }

    public short LidaYn { get; set; }

    public short Valid { get; set; }
}
