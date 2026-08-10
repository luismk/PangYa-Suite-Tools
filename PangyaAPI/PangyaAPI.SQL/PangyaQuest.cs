using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaQuest
{
    public int Id { get; set; }

    public int AchievementId { get; set; }

    public int Uid { get; set; }

    public string Name { get; set; }

    public int Typeid { get; set; }

    public int CounterItemId { get; set; }

    public DateTime? Date { get; set; }
}
