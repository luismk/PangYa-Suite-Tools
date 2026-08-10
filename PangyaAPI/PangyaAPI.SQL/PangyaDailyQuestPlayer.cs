using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaDailyQuestPlayer
{
    public long Uid { get; set; }

    public DateTime? LastQuestAccept { get; set; }

    public DateTime? TodayQuest { get; set; }
}
