using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaGachaJpAllItemList
{
    public long Index { get; set; }

    public int Typeid { get; set; }

    public string Name { get; set; }

    public byte CharType { get; set; }

    public DateTime RegDate { get; set; }
}
