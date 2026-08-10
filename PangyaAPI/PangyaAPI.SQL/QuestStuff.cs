using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class QuestStuff
{
    public long Index { get; set; }

    public int Typeid { get; set; }

    public string Nome { get; set; }

    public int CounterTypeid { get; set; }

    public int CounterQntd { get; set; }
}
