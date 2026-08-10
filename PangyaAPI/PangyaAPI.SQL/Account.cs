using System;
using System.Collections.Generic;

namespace PangyaAPI.SQL;

public partial class Account
{
    public string Id { get; set; }

    public long Uid { get; set; }

    public string Password { get; set; }

    public long Idstate { get; set; }

    public DateTime? LastLogonTime { get; set; }

    public int BlockTime { get; set; }

    public short Logon { get; set; }

    public short FirstLogin { get; set; }

    public DateTime? RegDate { get; set; }

    public string Nick { get; set; }

    public short FirstSet { get; set; }

    public int GuildUid { get; set; }

    public short Sex { get; set; }

    public short DoTutorial { get; set; }

    public string NomeCompleto { get; set; }

    public DateTime? BirthDay { get; set; }

    public string UserName { get; set; }

    public string UserIp { get; set; }

    public string ServerId { get; set; }

    public string GameServerId { get; set; }

    public DateTime? LastLeaveTime { get; set; }

    public long LogonCount { get; set; }

    public DateTime? BlockRegDate { get; set; }

    public int School { get; set; }

    public int Capability { get; set; }

    public short Event { get; set; }

    public short MannerFlag { get; set; }

    public short Event1 { get; set; }

    public int Event2 { get; set; }

    public int Domainid { get; set; }

    public short ChannelFlag { get; set; }

    public DateTime? ChangeNick { get; set; }

    public string Question { get; set; }

    public string Answer { get; set; }

    public string MacAddress { get; set; }

    public bool DonationPrivate { get; set; }

    public bool? HasClaimedActiveGift { get; set; }

    public bool ClaimedReturnerBonus { get; set; }

    public string ProfileImage { get; set; }

    public string PasswordResetToken { get; set; }

    public DateTime? PasswordResetExpires { get; set; }

    public virtual ICollection<ShopPurchase> ShopPurchases { get; set; } = new List<ShopPurchase>();
}
