using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class TuUcc
{
    public long Uid { get; set; }

    public long Typeid { get; set; }

    public string Uccidx { get; set; }

    public int Seq { get; set; }

    public string UccName { get; set; }

    public string UseYn { get; set; }

    public DateTime InDate { get; set; }

    public int? Copier { get; set; }

    public decimal ItemId { get; set; }

    public string CopierNick { get; set; }

    public DateTime? DrawDt { get; set; }

    public short Status { get; set; }

    public short Flag { get; set; }

    public string Skey { get; set; }

    public short Trade { get; set; }
}
