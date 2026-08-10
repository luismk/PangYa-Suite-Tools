using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class PangyaServerList
{
    public string Name { get; set; }

    public int Uid { get; set; }

    public string Ip { get; set; }

    public int Port { get; set; }

    public int MaxUser { get; set; }

    public int CurrUser { get; set; }

    public short Type { get; set; }

    public DateTime UpdateTime { get; set; }

    public short State { get; set; }

    public short PcbangUser { get; set; }

    public int PangRate { get; set; }

    public string ServerVersion { get; set; }

    public string ClientVersion { get; set; }

    public int Property { get; set; }

    public int AngelicWingsNum { get; set; }

    public short EventFlag { get; set; }

    public int ExpRate { get; set; }

    public int RareItemRate { get; set; }

    public int CookieItemRate { get; set; }

    public int ServiceControl { get; set; }

    public short ImgNo { get; set; }

    public short AppRate { get; set; }

    public short ScratchRate { get; set; }

    public int EventMap { get; set; }

    public int EventDropRate { get; set; }

    public int HanbitUser { get; set; }

    public int ParanUser { get; set; }

    public short AuthState { get; set; }

    public short MasteryRate { get; set; }

    public short TreasureRate { get; set; }

    public short ChuvaRate { get; set; }
}
