using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaTreasureHunterEventItem
{
    public int Index { get; set; }

    public int Position { get; set; }

    public string Name { get; set; }

    public int Typeid { get; set; }

    public int Quantidade { get; set; }

    public int Probabilidade { get; set; }

    public int Tipo { get; set; }

    public short Flag { get; set; }
}
