using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class AchievementQuest
{
    public decimal Idx { get; set; }

    public int Uid { get; set; }

    public int IdAchievement { get; set; }

    public int TypeIdAchieve { get; set; }

    public int CountId { get; set; }

    public int DataSec { get; set; }

    public int ObjetivoQuest { get; set; }
}
