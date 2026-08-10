using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaGachaJpItemList
{
    public long Index { get; set; }

    public byte Active { get; set; }

    public int GachaNum { get; set; }

    public int Typeid1 { get; set; }

    public int? Typeid2 { get; set; }

    public long Qnty1 { get; set; }

    public long? Qnty2 { get; set; }

    public byte RarityType { get; set; }

    public DateTime RegDate { get; set; }
}
