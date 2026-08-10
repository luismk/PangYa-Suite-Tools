using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaTicketReport
{
    public long Idx { get; set; }

    public int TrofelTypeid { get; set; }

    public short Flag { get; set; }

    public DateTime? RegDate { get; set; }

    public int Tipo { get; set; }
}
