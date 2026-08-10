using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class ScratchyItem
{
    public string Name { get; set; }

    public int TypeId { get; set; }

    public int Numero { get; set; }

    public int Quantidade { get; set; }

    public int Probabilidade { get; set; }

    public int Tipo { get; set; }

    public short Flag { get; set; }

    public short Active { get; set; }
}
