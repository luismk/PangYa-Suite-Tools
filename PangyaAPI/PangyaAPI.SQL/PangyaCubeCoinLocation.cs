using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaCubeCoinLocation
{
    public long Index { get; set; }

    public int Tipo { get; set; }

    public int Config2 { get; set; }

    public short Course { get; set; }

    public short Hole { get; set; }

    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }
}
