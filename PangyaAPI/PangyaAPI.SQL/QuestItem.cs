using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class QuestItem
{
    public long Index { get; set; }

    public int Typeid { get; set; }

    public string Nome { get; set; }

    public int StuffTypeid { get; set; }
}
