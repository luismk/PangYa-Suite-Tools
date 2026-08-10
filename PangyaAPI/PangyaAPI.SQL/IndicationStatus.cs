using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class IndicationStatus
{
    public int Id { get; set; }

    public int IndicatedUid { get; set; }

    public int ReferrerUid { get; set; }

    public int LevelRequired { get; set; }

    public string Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int CurrentLevel { get; set; }
}
