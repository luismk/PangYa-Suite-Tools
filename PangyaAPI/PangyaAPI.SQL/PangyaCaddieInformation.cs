using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaCaddieInformation
{
    public long ItemId { get; set; }

    public int Uid { get; set; }

    public int Typeid { get; set; }

    public int PartsTypeid { get; set; }

    public short GiftFlag { get; set; }

    public short CLevel { get; set; }

    public int Exp { get; set; }

    public DateTime RegDate { get; set; }

    public short Period { get; set; }

    public DateTime? EndDate { get; set; }

    public short RentFlag { get; set; }

    public short Purchase { get; set; }

    public DateTime? PartsEndDate { get; set; }

    public short CheckEnd { get; set; }

    public short Valid { get; set; }
}
