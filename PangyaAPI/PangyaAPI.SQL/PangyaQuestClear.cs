using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaQuestClear
{
    public long Index { get; set; }

    public int Uid { get; set; }

    public int QuestId { get; set; }

    public short Option { get; set; }
}
