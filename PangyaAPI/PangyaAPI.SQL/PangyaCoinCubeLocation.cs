using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaCoinCubeLocation
{
    public long Index { get; set; }

    public byte Course { get; set; }

    public byte Hole { get; set; }

    public byte Tipo { get; set; }

    public byte TipoLocation { get; set; }

    public long Rate { get; set; }

    public double X { get; set; }

    public double Y { get; set; }

    public double Z { get; set; }

    public DateTime RegDate { get; set; }
}
