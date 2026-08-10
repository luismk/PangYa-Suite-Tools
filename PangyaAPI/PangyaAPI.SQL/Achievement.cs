using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class Achievement
{
    public long Index { get; set; }

    public int Typeid { get; set; }

    public string Nome { get; set; }

    public short Tipo { get; set; }

    public short Option { get; set; }

    public int QuestTypeid { get; set; }
}
