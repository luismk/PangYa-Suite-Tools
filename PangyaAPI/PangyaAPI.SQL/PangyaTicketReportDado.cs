using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaTicketReportDado
{
    public long ReportId { get; set; }

    public int PlayerUid { get; set; }

    public short PlayerScore { get; set; }

    public short PlayerMedalha { get; set; }

    public short PlayerTrofel { get; set; }

    public long PlayerPang { get; set; }

    public long PlayerBonusPang { get; set; }

    public int PlayerExp { get; set; }

    public int PlayerMascotTypeid { get; set; }

    public short PlayerState { get; set; }

    public short FlagItemPang { get; set; }

    public short FlagPremiumUser { get; set; }

    public DateTime? FinishDate { get; set; }
}
