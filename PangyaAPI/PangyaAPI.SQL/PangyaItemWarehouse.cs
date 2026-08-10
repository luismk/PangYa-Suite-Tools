using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaItemWarehouse
{
    public long ItemId { get; set; }

    public int Uid { get; set; }

    public int Typeid { get; set; }

    public short Valid { get; set; }

    public DateTime? Regdate { get; set; }

    public short GiftFlag { get; set; }

    public short Flag { get; set; }

    public DateTime? Applytime { get; set; }

    public DateTime? EndDate { get; set; }

    public short C0 { get; set; }

    public short C1 { get; set; }

    public short C2 { get; set; }

    public short C3 { get; set; }

    public short C4 { get; set; }

    public short Purchase { get; set; }

    public short ItemType { get; set; }

    public short ClubSetWorkShopFlag { get; set; }

    public short ClubSetWorkShopC0 { get; set; }

    public short ClubSetWorkShopC1 { get; set; }

    public short ClubSetWorkShopC2 { get; set; }

    public short ClubSetWorkShopC3 { get; set; }

    public short ClubSetWorkShopC4 { get; set; }

    public int MasteryPts { get; set; }

    public int RecoveryPts { get; set; }

    public int Level { get; set; }

    public int Up { get; set; }

    public long TotalMasteryPts { get; set; }

    public int MasteryGasto { get; set; }
}
