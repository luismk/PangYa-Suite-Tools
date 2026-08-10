using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaMascotInfo
{
    public long ItemId { get; set; }

    public int Uid { get; set; }

    public int Typeid { get; set; }

    public short MLevel { get; set; }

    public int MExp { get; set; }

    public short Flag { get; set; }

    public short Tipo { get; set; }

    public DateTime RegDate { get; set; }

    public short Period { get; set; }

    public DateTime? EndDate { get; set; }

    public string Message { get; set; }

    public short IsCash { get; set; }

    public int Price { get; set; }

    public short Valid { get; set; }
}
