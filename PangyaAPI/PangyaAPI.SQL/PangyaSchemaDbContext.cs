using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace PangyaAPI.SQL;

public partial class PangyaSchemaDbContext : DbContext
{
    public PangyaSchemaDbContext(DbContextOptions<PangyaSchemaDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Achievement> Achievements { get; set; }

    public virtual DbSet<AchievementQuest> AchievementQuests { get; set; }

    public virtual DbSet<AchievementTipo> AchievementTipos { get; set; }

    public virtual DbSet<AuthkeyGame> AuthkeyGames { get; set; }

    public virtual DbSet<AuthkeyLogin> AuthkeyLogins { get; set; }

    public virtual DbSet<CharEquip> CharEquips { get; set; }

    public virtual DbSet<ContasBetum> ContasBeta { get; set; }

    public virtual DbSet<CountItem> CountItems { get; set; }

    public virtual DbSet<CounterItem> CounterItems { get; set; }

    public virtual DbSet<IndicationStatus> IndicationStatuses { get; set; }

    public virtual DbSet<ManiaCookie> ManiaCookies { get; set; }

    public virtual DbSet<Pangya1stAnniversary> Pangya1stAnniversaries { get; set; }

    public virtual DbSet<Pangya1stAnniversaryPlayerWinCp> Pangya1stAnniversaryPlayerWinCps { get; set; }

    public virtual DbSet<PangyaAchievement> PangyaAchievements { get; set; }

    public virtual DbSet<PangyaApproachMission> PangyaApproachMissions { get; set; }

    public virtual DbSet<PangyaAssistente> PangyaAssistentes { get; set; }

    public virtual DbSet<PangyaAttendanceReward> PangyaAttendanceRewards { get; set; }

    public virtual DbSet<PangyaAttendanceTableItemReward> PangyaAttendanceTableItemRewards { get; set; }

    public virtual DbSet<PangyaAuthKey> PangyaAuthKeys { get; set; }

    public virtual DbSet<PangyaBotGmEventReward> PangyaBotGmEventRewards { get; set; }

    public virtual DbSet<PangyaBotGmEventTime> PangyaBotGmEventTimes { get; set; }

    public virtual DbSet<PangyaCaddieInformation> PangyaCaddieInformations { get; set; }

    public virtual DbSet<PangyaCard> PangyaCards { get; set; }

    public virtual DbSet<PangyaCardEquip> PangyaCardEquips { get; set; }

    public virtual DbSet<PangyaChangeEmailLog> PangyaChangeEmailLogs { get; set; }

    public virtual DbSet<PangyaChangeNicknameLog> PangyaChangeNicknameLogs { get; set; }

    public virtual DbSet<PangyaChangePwdLog> PangyaChangePwdLogs { get; set; }

    public virtual DbSet<PangyaCharacterInformation> PangyaCharacterInformations { get; set; }

    public virtual DbSet<PangyaCharacterPartPadrao> PangyaCharacterPartPadraos { get; set; }

    public virtual DbSet<PangyaClubsetEnchant> PangyaClubsetEnchants { get; set; }

    public virtual DbSet<PangyaCoinCubeInfo> PangyaCoinCubeInfos { get; set; }

    public virtual DbSet<PangyaCoinCubeLocation> PangyaCoinCubeLocations { get; set; }

    public virtual DbSet<PangyaCometRefill> PangyaCometRefills { get; set; }

    public virtual DbSet<PangyaCommand> PangyaCommands { get; set; }

    public virtual DbSet<PangyaCommandGmLog> PangyaCommandGmLogs { get; set; }

    public virtual DbSet<PangyaConfig> PangyaConfigs { get; set; }

    public virtual DbSet<PangyaCookiePointItemLog> PangyaCookiePointItemLogs { get; set; }

    public virtual DbSet<PangyaCookiePointLog> PangyaCookiePointLogs { get; set; }

    public virtual DbSet<PangyaCounterItem> PangyaCounterItems { get; set; }

    public virtual DbSet<PangyaCouponDesconto> PangyaCouponDescontos { get; set; }

    public virtual DbSet<PangyaCourseCubeCoinTemporadum> PangyaCourseCubeCoinTemporada { get; set; }

    public virtual DbSet<PangyaCourseRewardTreasure> PangyaCourseRewardTreasures { get; set; }

    public virtual DbSet<PangyaCubeCoinLocation> PangyaCubeCoinLocations { get; set; }

    public virtual DbSet<PangyaDailyQuest> PangyaDailyQuests { get; set; }

    public virtual DbSet<PangyaDailyQuestPlayer> PangyaDailyQuestPlayers { get; set; }

    public virtual DbSet<PangyaDolfiniLocker> PangyaDolfiniLockers { get; set; }

    public virtual DbSet<PangyaDolfiniLockerItem> PangyaDolfiniLockerItems { get; set; }

    public virtual DbSet<PangyaDonationEpin> PangyaDonationEpins { get; set; }

    public virtual DbSet<PangyaDonationItemLog> PangyaDonationItemLogs { get; set; }

    public virtual DbSet<PangyaDonationLog> PangyaDonationLogs { get; set; }

    public virtual DbSet<PangyaDonationNew> PangyaDonationNews { get; set; }

    public virtual DbSet<PangyaEventItensSite> PangyaEventItensSites { get; set; }

    public virtual DbSet<PangyaEventSite> PangyaEventSites { get; set; }

    public virtual DbSet<PangyaExceptionLog> PangyaExceptionLogs { get; set; }

    public virtual DbSet<PangyaFastPassEvent> PangyaFastPassEvents { get; set; }

    public virtual DbSet<PangyaFriendList> PangyaFriendLists { get; set; }

    public virtual DbSet<PangyaGacha> PangyaGachas { get; set; }

    public virtual DbSet<PangyaGachaCoin> PangyaGachaCoins { get; set; }

    public virtual DbSet<PangyaGachaItem> PangyaGachaItems { get; set; }

    public virtual DbSet<PangyaGachaJpAllItemList> PangyaGachaJpAllItemLists { get; set; }

    public virtual DbSet<PangyaGachaJpItemList> PangyaGachaJpItemLists { get; set; }

    public virtual DbSet<PangyaGachaJpPlayerWin> PangyaGachaJpPlayerWins { get; set; }

    public virtual DbSet<PangyaGachaJpRate> PangyaGachaJpRates { get; set; }

    public virtual DbSet<PangyaGachaUserKey> PangyaGachaUserKeys { get; set; }

    public virtual DbSet<PangyaGachaUserWon> PangyaGachaUserWons { get; set; }

    public virtual DbSet<PangyaGiftTable> PangyaGiftTables { get; set; }

    public virtual DbSet<PangyaGmGiftWebLog> PangyaGmGiftWebLogs { get; set; }

    public virtual DbSet<PangyaGoldenTimeInfo> PangyaGoldenTimeInfos { get; set; }

    public virtual DbSet<PangyaGoldenTimeItem> PangyaGoldenTimeItems { get; set; }

    public virtual DbSet<PangyaGoldenTimeRound> PangyaGoldenTimeRounds { get; set; }

    public virtual DbSet<PangyaGrandZodiacPonto> PangyaGrandZodiacPontos { get; set; }

    public virtual DbSet<PangyaGrandZodiacTime> PangyaGrandZodiacTimes { get; set; }

    public virtual DbSet<PangyaGrandprixClear> PangyaGrandprixClears { get; set; }

    public virtual DbSet<PangyaGrandprixEventConfig> PangyaGrandprixEventConfigs { get; set; }

    public virtual DbSet<PangyaGuild> PangyaGuilds { get; set; }

    public virtual DbSet<PangyaGuildAtividadePlayer> PangyaGuildAtividadePlayers { get; set; }

    public virtual DbSet<PangyaGuildBb> PangyaGuildBbs { get; set; }

    public virtual DbSet<PangyaGuildBbsRe> PangyaGuildBbsRes { get; set; }

    public virtual DbSet<PangyaGuildIntroImgLog> PangyaGuildIntroImgLogs { get; set; }

    public virtual DbSet<PangyaGuildMarkLog> PangyaGuildMarkLogs { get; set; }

    public virtual DbSet<PangyaGuildMatch> PangyaGuildMatches { get; set; }

    public virtual DbSet<PangyaGuildMember> PangyaGuildMembers { get; set; }

    public virtual DbSet<PangyaGuildNotice> PangyaGuildNotices { get; set; }

    public virtual DbSet<PangyaGuildPrivateBb> PangyaGuildPrivateBbs { get; set; }

    public virtual DbSet<PangyaGuildPrivateBbsRe> PangyaGuildPrivateBbsRes { get; set; }

    public virtual DbSet<PangyaGuildRanking> PangyaGuildRankings { get; set; }

    public virtual DbSet<PangyaGuildUpdateActivity> PangyaGuildUpdateActivities { get; set; }

    public virtual DbSet<PangyaGzEvent2016121600RareWin> PangyaGzEvent2016121600RareWins { get; set; }

    public virtual DbSet<PangyaHioEvent> PangyaHioEvents { get; set; }

    public virtual DbSet<PangyaHioEventItem> PangyaHioEventItems { get; set; }

    public virtual DbSet<PangyaHioEventLog> PangyaHioEventLogs { get; set; }

    public virtual DbSet<PangyaHoleEvent> PangyaHoleEvents { get; set; }

    public virtual DbSet<PangyaHoleEventConfig> PangyaHoleEventConfigs { get; set; }

    public virtual DbSet<PangyaHoleEventItem> PangyaHoleEventItems { get; set; }

    public virtual DbSet<PangyaIpTable> PangyaIpTables { get; set; }

    public virtual DbSet<PangyaItemBuff> PangyaItemBuffs { get; set; }

    public virtual DbSet<PangyaItemBuyShopLog> PangyaItemBuyShopLogs { get; set; }

    public virtual DbSet<PangyaItemMail> PangyaItemMails { get; set; }

    public virtual DbSet<PangyaItemTypelist> PangyaItemTypelists { get; set; }

    public virtual DbSet<PangyaItemWarehouse> PangyaItemWarehouses { get; set; }

    public virtual DbSet<PangyaLastPlayersUser> PangyaLastPlayersUsers { get; set; }

    public virtual DbSet<PangyaLastUpClubset> PangyaLastUpClubsets { get; set; }

    public virtual DbSet<PangyaLoginReward> PangyaLoginRewards { get; set; }

    public virtual DbSet<PangyaLoginRewardPlayer> PangyaLoginRewardPlayers { get; set; }

    public virtual DbSet<PangyaLuciaAttendance> PangyaLuciaAttendances { get; set; }

    public virtual DbSet<PangyaLuciaAttendanceRewardLog> PangyaLuciaAttendanceRewardLogs { get; set; }

    public virtual DbSet<PangyaMacTable> PangyaMacTables { get; set; }

    public virtual DbSet<PangyaManiadonationLog> PangyaManiadonationLogs { get; set; }

    public virtual DbSet<PangyaMascotInfo> PangyaMascotInfos { get; set; }

    public virtual DbSet<PangyaMsgUser> PangyaMsgUsers { get; set; }

    public virtual DbSet<PangyaMyroom> PangyaMyrooms { get; set; }

    public virtual DbSet<PangyaNewBox> PangyaNewBoxes { get; set; }

    public virtual DbSet<PangyaNewBoxItem> PangyaNewBoxItems { get; set; }

    public virtual DbSet<PangyaNewBoxRareWinLog> PangyaNewBoxRareWinLogs { get; set; }

    public virtual DbSet<PangyaNewCard> PangyaNewCards { get; set; }

    public virtual DbSet<PangyaNewCardPack> PangyaNewCardPacks { get; set; }

    public virtual DbSet<PangyaNewCourseDrop> PangyaNewCourseDrops { get; set; }

    public virtual DbSet<PangyaNewCourseDropItem> PangyaNewCourseDropItems { get; set; }

    public virtual DbSet<PangyaNewMemorialCoin> PangyaNewMemorialCoins { get; set; }

    public virtual DbSet<PangyaNewMemorialLevel> PangyaNewMemorialLevels { get; set; }

    public virtual DbSet<PangyaNewMemorialLuckySet> PangyaNewMemorialLuckySets { get; set; }

    public virtual DbSet<PangyaNewMemorialNormalItem> PangyaNewMemorialNormalItems { get; set; }

    public virtual DbSet<PangyaNewMemorialRareItem> PangyaNewMemorialRareItems { get; set; }

    public virtual DbSet<PangyaNewMemorialRareWinLog> PangyaNewMemorialRareWinLogs { get; set; }

    public virtual DbSet<PangyaNewPremiumUser> PangyaNewPremiumUsers { get; set; }

    public virtual DbSet<PangyaNewPremiumUserItem> PangyaNewPremiumUserItems { get; set; }

    public virtual DbSet<PangyaNewPremiumUserLog> PangyaNewPremiumUserLogs { get; set; }

    public virtual DbSet<PangyaNoticeList> PangyaNoticeLists { get; set; }

    public virtual DbSet<PangyaPapelShopConfig> PangyaPapelShopConfigs { get; set; }

    public virtual DbSet<PangyaPapelShopCoupon> PangyaPapelShopCoupons { get; set; }

    public virtual DbSet<PangyaPapelShopInfo> PangyaPapelShopInfos { get; set; }

    public virtual DbSet<PangyaPapelShopItem> PangyaPapelShopItems { get; set; }

    public virtual DbSet<PangyaPapelShopRareWinLog> PangyaPapelShopRareWinLogs { get; set; }

    public virtual DbSet<PangyaPartsList> PangyaPartsLists { get; set; }

    public virtual DbSet<PangyaPersonalShopConfig> PangyaPersonalShopConfigs { get; set; }

    public virtual DbSet<PangyaPersonalShopLog> PangyaPersonalShopLogs { get; set; }

    public virtual DbSet<PangyaPlayerBirthDayLog> PangyaPlayerBirthDayLogs { get; set; }

    public virtual DbSet<PangyaPlayerIp> PangyaPlayerIps { get; set; }

    public virtual DbSet<PangyaPlayerLocation> PangyaPlayerLocations { get; set; }

    public virtual DbSet<PangyaPointEvent> PangyaPointEvents { get; set; }

    public virtual DbSet<PangyaPointEventItem> PangyaPointEventItems { get; set; }

    public virtual DbSet<PangyaPremioindicacaoLog> PangyaPremioindicacaoLogs { get; set; }

    public virtual DbSet<PangyaQuest> PangyaQuests { get; set; }

    public virtual DbSet<PangyaQuestClear> PangyaQuestClears { get; set; }

    public virtual DbSet<PangyaRankAnte> PangyaRankAntes { get; set; }

    public virtual DbSet<PangyaRankAtual> PangyaRankAtuals { get; set; }

    public virtual DbSet<PangyaRankAtualCharacter> PangyaRankAtualCharacters { get; set; }

    public virtual DbSet<PangyaRankConfig> PangyaRankConfigs { get; set; }

    public virtual DbSet<PangyaRecord> PangyaRecords { get; set; }

    public virtual DbSet<PangyaRescuePwdLog> PangyaRescuePwdLogs { get; set; }

    public virtual DbSet<PangyaRewardSsc> PangyaRewardSscs { get; set; }

    public virtual DbSet<PangyaRoomLog> PangyaRoomLogs { get; set; }

    public virtual DbSet<PangyaServerList> PangyaServerLists { get; set; }

    public virtual DbSet<PangyaShopGift> PangyaShopGifts { get; set; }

    public virtual DbSet<PangyaShopGiftLog> PangyaShopGiftLogs { get; set; }

    public virtual DbSet<PangyaShutdownList> PangyaShutdownLists { get; set; }

    public virtual DbSet<PangyaTickerList> PangyaTickerLists { get; set; }

    public virtual DbSet<PangyaTicketReport> PangyaTicketReports { get; set; }

    public virtual DbSet<PangyaTicketReportDado> PangyaTicketReportDados { get; set; }

    public virtual DbSet<PangyaTikiPoint> PangyaTikiPoints { get; set; }

    public virtual DbSet<PangyaTikiPointsItem> PangyaTikiPointsItems { get; set; }

    public virtual DbSet<PangyaTransformeClubsetTemp> PangyaTransformeClubsetTemps { get; set; }

    public virtual DbSet<PangyaTreasureHunterEventItem> PangyaTreasureHunterEventItems { get; set; }

    public virtual DbSet<PangyaTreasureItem> PangyaTreasureItems { get; set; }

    public virtual DbSet<PangyaTrofelEspecial> PangyaTrofelEspecials { get; set; }

    public virtual DbSet<PangyaTrofelGrandprix> PangyaTrofelGrandprixes { get; set; }

    public virtual DbSet<PangyaUserEquip> PangyaUserEquips { get; set; }

    public virtual DbSet<PangyaUserMacro> PangyaUserMacros { get; set; }

    public virtual DbSet<PangyaUsersEditorIff> PangyaUsersEditorIffs { get; set; }

    public virtual DbSet<PangyaWeblinkCookiesKey> PangyaWeblinkCookiesKeys { get; set; }

    public virtual DbSet<PangyaWeblinkKey> PangyaWeblinkKeys { get; set; }

    public virtual DbSet<PangyaWorldTourConfig> PangyaWorldTourConfigs { get; set; }

    public virtual DbSet<PangyaWorldTourEvent> PangyaWorldTourEvents { get; set; }

    public virtual DbSet<PangyaWorldTourEventItem> PangyaWorldTourEventItems { get; set; }

    public virtual DbSet<PangyaWorldTourEventLog> PangyaWorldTourEventLogs { get; set; }

    public virtual DbSet<QuestItem> QuestItems { get; set; }

    public virtual DbSet<QuestStuff> QuestStuffs { get; set; }

    public virtual DbSet<ScratchyItem> ScratchyItems { get; set; }

    public virtual DbSet<ScratchyRareWin> ScratchyRareWins { get; set; }

    public virtual DbSet<ScratchyRate> ScratchyRates { get; set; }

    public virtual DbSet<ShopProduct> ShopProducts { get; set; }

    public virtual DbSet<ShopProductItem> ShopProductItems { get; set; }

    public virtual DbSet<ShopPurchase> ShopPurchases { get; set; }

    public virtual DbSet<TdCharEquipS4> TdCharEquipS4s { get; set; }

    public virtual DbSet<TdRoomDatum> TdRoomData { get; set; }

    public virtual DbSet<TempCounterTypeidInit> TempCounterTypeidInits { get; set; }

    public virtual DbSet<TempTmp> TempTmps { get; set; }

    public virtual DbSet<TempTypeid> TempTypeids { get; set; }

    public virtual DbSet<TrofelStat> TrofelStats { get; set; }

    public virtual DbSet<TuUcc> TuUccs { get; set; }

    public virtual DbSet<Tutorial> Tutorials { get; set; }

    public virtual DbSet<TypeList> TypeLists { get; set; }

    public virtual DbSet<UserInfo> UserInfos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("PK_account_UID");

            entity.ToTable("account", "pangya");

            entity.HasIndex(e => e.Id, "IX_account").IsUnique();

            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.Answer)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.BirthDay).HasColumnType("datetime");
            entity.Property(e => e.BlockRegDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.Capability).HasColumnName("capability");
            entity.Property(e => e.ChangeNick)
                .HasPrecision(0)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("change_nick");
            entity.Property(e => e.ClaimedReturnerBonus).HasColumnName("claimed_returner_bonus");
            entity.Property(e => e.DoTutorial).HasColumnName("doTutorial");
            entity.Property(e => e.Domainid).HasColumnName("domainid");
            entity.Property(e => e.DonationPrivate)
                .HasDefaultValue(true)
                .HasColumnName("donation_private");
            entity.Property(e => e.FirstLogin).HasColumnName("FIRST_LOGIN");
            entity.Property(e => e.FirstSet).HasColumnName("FIRST_SET");
            entity.Property(e => e.GameServerId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("game_server_id");
            entity.Property(e => e.GuildUid).HasColumnName("Guild_UID");
            entity.Property(e => e.HasClaimedActiveGift)
                .HasDefaultValue(false)
                .HasColumnName("has_claimed_active_gift");
            entity.Property(e => e.Id)
                .IsRequired()
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasDefaultValue("")
                .HasColumnName("ID");
            entity.Property(e => e.Idstate).HasColumnName("IDState");
            entity.Property(e => e.LastLeaveTime)
                .HasPrecision(0)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.LastLogonTime)
                .HasPrecision(0)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.MacAddress).HasMaxLength(50);
            entity.Property(e => e.Nick)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("")
                .HasColumnName("NICK");
            entity.Property(e => e.NomeCompleto)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Password)
                .IsRequired()
                .HasMaxLength(33)
                .IsUnicode(false)
                .HasDefaultValue("")
                .HasColumnName("PASSWORD");
            entity.Property(e => e.PasswordResetExpires)
                .HasColumnType("datetime")
                .HasColumnName("password_reset_expires");
            entity.Property(e => e.PasswordResetToken)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("password_reset_token");
            entity.Property(e => e.ProfileImage)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("profile_image");
            entity.Property(e => e.Question)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RegDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.ServerId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("ServerID");
            entity.Property(e => e.UserIp)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.UserName)
                .HasMaxLength(23)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)");
        });

        modelBuilder.Entity<Achievement>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK_achievements_index");

            entity.ToTable("achievements", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.Nome)
                .IsRequired()
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasDefaultValue("")
                .HasColumnName("nome");
            entity.Property(e => e.Option).HasColumnName("option");
            entity.Property(e => e.QuestTypeid).HasColumnName("quest_typeid");
            entity.Property(e => e.Tipo).HasColumnName("tipo");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
        });

        modelBuilder.Entity<AchievementQuest>(entity =>
        {
            entity.HasKey(e => e.Idx).HasName("PK_achievement_quest_IDX");

            entity.ToTable("achievement_quest", "pangya");

            entity.Property(e => e.Idx)
                .ValueGeneratedOnAdd()
                .HasColumnType("numeric(20, 0)")
                .HasColumnName("IDX");
            entity.Property(e => e.CountId).HasColumnName("Count_ID");
            entity.Property(e => e.DataSec).HasColumnName("Data_Sec");
            entity.Property(e => e.IdAchievement).HasColumnName("ID_ACHIEVEMENT");
            entity.Property(e => e.ObjetivoQuest).HasColumnName("Objetivo_Quest");
            entity.Property(e => e.TypeIdAchieve).HasColumnName("TypeID_ACHIEVE");
            entity.Property(e => e.Uid).HasColumnName("UID");
        });

        modelBuilder.Entity<AchievementTipo>(entity =>
        {
            entity.HasKey(e => e.IdAchievement).HasName("PK_achievement_tipo_ID_ACHIEVEMENT");

            entity.ToTable("achievement_tipo", "pangya");

            entity.Property(e => e.IdAchievement).HasColumnName("ID_ACHIEVEMENT");
            entity.Property(e => e.Nome)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.Tipo).HasColumnName("TIPO");
            entity.Property(e => e.TypeId).HasColumnName("TypeID");
            entity.Property(e => e.Uid).HasColumnName("UID");
        });

        modelBuilder.Entity<AuthkeyGame>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("authkey_game", "pangya");

            entity.Property(e => e.AuthKey)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasDefaultValue("");
            entity.Property(e => e.ServerId).HasColumnName("ServerID");
            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.Valid)
                .HasDefaultValue((short)1)
                .HasColumnName("valid");
        });

        modelBuilder.Entity<AuthkeyLogin>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("authkey_login", "pangya");

            entity.Property(e => e.AuthKey)
                .IsRequired()
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasDefaultValue("");
            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.Valid)
                .HasDefaultValue((short)1)
                .HasColumnName("valid");
        });

        modelBuilder.Entity<CharEquip>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("PK_char_equip_UID");

            entity.ToTable("char_equip", "pangya");

            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.Auxpart1).HasColumnName("auxpart1");
            entity.Property(e => e.Auxpart2).HasColumnName("auxpart2");
            entity.Property(e => e.Auxpart3).HasColumnName("auxpart3");
            entity.Property(e => e.Auxpart4).HasColumnName("auxpart4");
            entity.Property(e => e.Auxpart5).HasColumnName("auxpart5");
            entity.Property(e => e.DefaultHair).HasColumnName("default_hair");
            entity.Property(e => e.DefaultShirts).HasColumnName("default_shirts");
            entity.Property(e => e.IsValid).HasColumnName("isValid");
            entity.Property(e => e.Part1).HasColumnName("part1");
            entity.Property(e => e.Part10).HasColumnName("part10");
            entity.Property(e => e.Part11).HasColumnName("part11");
            entity.Property(e => e.Part12).HasColumnName("part12");
            entity.Property(e => e.Part13).HasColumnName("part13");
            entity.Property(e => e.Part14).HasColumnName("part14");
            entity.Property(e => e.Part15).HasColumnName("part15");
            entity.Property(e => e.Part16).HasColumnName("part16");
            entity.Property(e => e.Part17).HasColumnName("part17");
            entity.Property(e => e.Part18).HasColumnName("part18");
            entity.Property(e => e.Part19).HasColumnName("part19");
            entity.Property(e => e.Part2).HasColumnName("part2");
            entity.Property(e => e.Part20).HasColumnName("part20");
            entity.Property(e => e.Part21).HasColumnName("part21");
            entity.Property(e => e.Part22).HasColumnName("part22");
            entity.Property(e => e.Part23).HasColumnName("part23");
            entity.Property(e => e.Part24).HasColumnName("part24");
            entity.Property(e => e.Part3).HasColumnName("part3");
            entity.Property(e => e.Part4).HasColumnName("part4");
            entity.Property(e => e.Part5).HasColumnName("part5");
            entity.Property(e => e.Part6).HasColumnName("part6");
            entity.Property(e => e.Part7).HasColumnName("part7");
            entity.Property(e => e.Part8).HasColumnName("part8");
            entity.Property(e => e.Part9).HasColumnName("part9");
            entity.Property(e => e.Pcl0).HasColumnName("PCL0");
            entity.Property(e => e.Pcl1).HasColumnName("PCL1");
            entity.Property(e => e.Pcl2).HasColumnName("PCL2");
            entity.Property(e => e.Pcl3).HasColumnName("PCL3");
            entity.Property(e => e.Pcl4).HasColumnName("PCL4");
            entity.Property(e => e.Pucharge).HasColumnName("pucharge");
        });

        modelBuilder.Entity<ContasBetum>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK_contas_beta_index");

            entity.ToTable("contas_beta", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.Birthday).HasColumnType("datetime");
            entity.Property(e => e.Codigo)
                .HasMaxLength(13)
                .IsFixedLength()
                .HasColumnName("codigo");
            entity.Property(e => e.DateReg)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("date_reg");
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.EmailChangeKey)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("email_change_key");
            entity.Property(e => e.FinishReg).HasColumnName("finish_reg");
            entity.Property(e => e.IpRegister)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("")
                .IsFixedLength()
                .HasColumnName("ip_register");
            entity.Property(e => e.KeyUniq)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("key_uniq");
            entity.Property(e => e.LoginId)
                .IsRequired()
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("LoginID");
            entity.Property(e => e.NewEmailPending)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("new_email_pending");
            entity.Property(e => e.NomeCompleto)
                .IsRequired()
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.Pergunta)
                .IsRequired()
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.ProfileImage)
                .HasMaxLength(300)
                .IsUnicode(false)
                .HasColumnName("profile_image");
            entity.Property(e => e.RecoveryExpires)
                .HasColumnType("datetime")
                .HasColumnName("recovery_expires");
            entity.Property(e => e.ReferrerCode)
                .HasMaxLength(25)
                .IsFixedLength()
                .HasColumnName("referrer_code");
            entity.Property(e => e.Resposta).HasMaxLength(120);
            entity.Property(e => e.Senha)
                .IsRequired()
                .HasMaxLength(33)
                .IsUnicode(false);
            entity.Property(e => e.StatusReferal)
                .HasMaxLength(10)
                .IsFixedLength()
                .HasColumnName("status_referal");
            entity.Property(e => e.Uid).HasColumnName("uid");
        });

        modelBuilder.Entity<CountItem>(entity =>
        {
            entity.HasKey(e => e.CountId).HasName("PK_count_item_Count_ID");

            entity.ToTable("count_item", "pangya");

            entity.Property(e => e.CountId).HasColumnName("Count_ID");
            entity.Property(e => e.CountNumItem).HasColumnName("Count_Num_Item");
            entity.Property(e => e.DataSec).HasColumnName("Data_Sec");
            entity.Property(e => e.IdAchievement).HasColumnName("ID_ACHIEVEMENT");
            entity.Property(e => e.Nome)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.Tipo).HasColumnName("TIPO");
            entity.Property(e => e.TypeId).HasColumnName("TypeID");
            entity.Property(e => e.Uid).HasColumnName("UID");
        });

        modelBuilder.Entity<CounterItem>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK_counter_items_index");

            entity.ToTable("counter_items", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.Nome)
                .IsRequired()
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasDefaultValue("")
                .HasColumnName("nome");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
        });

        modelBuilder.Entity<IndicationStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__indicati__3213E83FC9F82001");

            entity.ToTable("indication_status", "pangya");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CurrentLevel).HasColumnName("current_level");
            entity.Property(e => e.IndicatedUid).HasColumnName("indicated_uid");
            entity.Property(e => e.LevelRequired).HasColumnName("level_required");
            entity.Property(e => e.ReferrerUid).HasColumnName("referrer_uid");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("status");
        });

        modelBuilder.Entity<ManiaCookie>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__mania_co__3214EC2761576DEC");

            entity.ToTable("mania_cookies", "pangya");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CpDescription)
                .HasMaxLength(50)
                .HasColumnName("CP_Description");
            entity.Property(e => e.CpPrice)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("CP_Price");
            entity.Property(e => e.CpValue).HasColumnName("CP_Value");
        });

        modelBuilder.Entity<Pangya1stAnniversary>(entity =>
        {
            entity.HasKey(e => e.EventDone).HasName("PK_pangya_1st_aniversary");

            entity.ToTable("pangya_1st_anniversary", "pangya");

            entity.Property(e => e.EventDone).HasColumnName("EVENT_DONE");
            entity.Property(e => e.AllPlayerApt).HasColumnName("ALL_PLAYER_APT");
            entity.Property(e => e.AllPlayerWin).HasColumnName("ALL_PLAYER_WIN");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("REG_DATE");
        });

        modelBuilder.Entity<Pangya1stAnniversaryPlayerWinCp>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK_pangya_1st_aniversary_player_win_cp");

            entity.ToTable("pangya_1st_anniversary_player_win_cp", "pangya");

            entity.HasIndex(e => e.Uid, "IX_pangya_1st_aniversary_player_win_cp").IsUnique();

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.CookiePoint).HasColumnName("COOKIE_POINT");
            entity.Property(e => e.LoginDays).HasColumnName("LOGIN_DAYS");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("REG_DATE");
            entity.Property(e => e.Uid).HasColumnName("UID");
        });

        modelBuilder.Entity<PangyaAchievement>(entity =>
        {
            entity.HasKey(e => e.IdAchievement);

            entity.ToTable("pangya_achievement", "pangya");

            entity.HasIndex(e => e.IdAchievement, "IX_pangya_achievement").IsUnique();

            entity.Property(e => e.IdAchievement).HasColumnName("ID_ACHIEVEMENT");
            entity.Property(e => e.Active)
                .HasDefaultValue(1)
                .HasColumnName("active");
            entity.Property(e => e.Nome).HasMaxLength(100);
            entity.Property(e => e.Status)
                .HasDefaultValue(1)
                .HasComment("1 em agurado, 2 excluido, 3 ativo, 4 concluido")
                .HasColumnName("status");
            entity.Property(e => e.TypeId).HasColumnName("TypeID");
            entity.Property(e => e.Uid).HasColumnName("UID");
        });

        modelBuilder.Entity<PangyaApproachMission>(entity =>
        {
            entity.HasKey(e => e.Numero).HasName("PK_pangya_approach_missions_numero");

            entity.ToTable("pangya_approach_missions", "pangya");

            entity.Property(e => e.Numero)
                .ValueGeneratedNever()
                .HasColumnName("numero");
            entity.Property(e => e.Active)
                .HasDefaultValue((short)1)
                .HasColumnName("active");
            entity.Property(e => e.Box)
                .HasDefaultValue(1)
                .HasColumnName("box");
            entity.Property(e => e.Flag).HasColumnName("flag");
            entity.Property(e => e.RewardTipo)
                .HasDefaultValue(1)
                .HasColumnName("reward_tipo");
            entity.Property(e => e.Tipo)
                .HasDefaultValue(1)
                .HasColumnName("tipo");
        });

        modelBuilder.Entity<PangyaAssistente>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("PK_pangya_assistente_UID");

            entity.ToTable("pangya_assistente", "pangya");

            entity.Property(e => e.Uid)
                .HasDefaultValue(1)
                .HasColumnName("UID");
        });

        modelBuilder.Entity<PangyaAttendanceReward>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("PK_pangya_attendance_reward_UID");

            entity.ToTable("pangya_attendance_reward", "pangya");

            entity.Property(e => e.Uid)
                .ValueGeneratedNever()
                .HasColumnName("UID");
            entity.Property(e => e.Counter).HasColumnName("counter");
            entity.Property(e => e.ItemQntdAfter).HasColumnName("item_qntd_after");
            entity.Property(e => e.ItemQntdNow).HasColumnName("item_qntd_now");
            entity.Property(e => e.ItemTypeidAfter).HasColumnName("item_typeid_after");
            entity.Property(e => e.ItemTypeidNow).HasColumnName("item_typeid_now");
            entity.Property(e => e.LastLogin)
                .HasPrecision(0)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("last_login");
        });

        modelBuilder.Entity<PangyaAttendanceTableItemReward>(entity =>
        {
            entity.HasKey(e => e.Idx).HasName("PK_pangya_attendance_table_item_reward_idx");

            entity.ToTable("pangya_attendance_table_item_reward", "pangya");

            entity.Property(e => e.Idx).HasColumnName("idx");
            entity.Property(e => e.Nome)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("nome");
            entity.Property(e => e.Quantidade).HasColumnName("quantidade");
            entity.Property(e => e.Tipo).HasColumnName("tipo");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
        });

        modelBuilder.Entity<PangyaAuthKey>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_auth_key", "pangya");

            entity.HasIndex(e => e.Index, "IX_pangya.auth_key").IsUnique();

            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.Key)
                .HasMaxLength(16)
                .IsFixedLength()
                .HasColumnName("key");
            entity.Property(e => e.ServerUid).HasColumnName("Server_UID");
            entity.Property(e => e.Valid)
                .HasDefaultValue((byte)1)
                .HasColumnName("valid");
        });

        modelBuilder.Entity<PangyaBotGmEventReward>(entity =>
        {
            entity.HasKey(e => e.Index);

            entity.ToTable("pangya_bot_gm_event_reward", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.Qntd).HasColumnName("qntd");
            entity.Property(e => e.QntdTime).HasColumnName("qntd_time");
            entity.Property(e => e.Rate).HasColumnName("rate");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("reg_date");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
            entity.Property(e => e.Valid)
                .HasDefaultValue((byte)1)
                .HasColumnName("valid");
        });

        modelBuilder.Entity<PangyaBotGmEventTime>(entity =>
        {
            entity.HasKey(e => e.Index);

            entity.ToTable("pangya_bot_gm_event_time", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.ChannelId).HasColumnName("channel_id");
            entity.Property(e => e.FimTime).HasColumnName("fim_time");
            entity.Property(e => e.InicioTime).HasColumnName("inicio_time");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("reg_date");
            entity.Property(e => e.Valid).HasColumnName("valid");
        });

        modelBuilder.Entity<PangyaCaddieInformation>(entity =>
        {
            entity.HasKey(e => e.ItemId).HasName("PK_pangya_caddie_information_item_id");

            entity.ToTable("pangya_caddie_information", "pangya");

            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.CLevel).HasColumnName("cLevel");
            entity.Property(e => e.CheckEnd).HasDefaultValue((short)1);
            entity.Property(e => e.EndDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.GiftFlag).HasColumnName("gift_flag");
            entity.Property(e => e.PartsEndDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("parts_EndDate");
            entity.Property(e => e.PartsTypeid).HasColumnName("parts_typeid");
            entity.Property(e => e.RegDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.RentFlag).HasDefaultValue((short)1);
            entity.Property(e => e.Typeid).HasColumnName("typeid");
            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.Valid).HasDefaultValue((short)1);
        });

        modelBuilder.Entity<PangyaCard>(entity =>
        {
            entity.HasKey(e => e.CardItemid).HasName("PK_pangya_card_card_itemid");

            entity.ToTable("pangya_card", "pangya");

            entity.Property(e => e.CardItemid).HasColumnName("card_itemid");
            entity.Property(e => e.CardType)
                .HasDefaultValue((short)1)
                .HasColumnName("card_type");
            entity.Property(e => e.CardTypeid).HasColumnName("card_typeid");
            entity.Property(e => e.EfeitoQntd).HasColumnName("Efeito_Qntd");
            entity.Property(e => e.EndDt)
                .HasPrecision(0)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("END_DT");
            entity.Property(e => e.GetDt)
                .HasPrecision(0)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("GET_DT");
            entity.Property(e => e.Qntd)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("QNTD");
            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.UseDt)
                .HasPrecision(0)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("USE_DT");
            entity.Property(e => e.UseYn)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)")
                .IsFixedLength()
                .HasColumnName("USE_YN");
        });

        modelBuilder.Entity<PangyaCardEquip>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_card_equip", "pangya");

            entity.HasIndex(e => e.Index, "IX_pangya_card_equip").IsUnique();

            entity.Property(e => e.CardTypeid).HasColumnName("card_typeid");
            entity.Property(e => e.Date)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("date");
            entity.Property(e => e.EfeitoQntd).HasColumnName("Efeito_Qntd");
            entity.Property(e => e.EndDt)
                .HasPrecision(0)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("END_DT");
            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.PartsId).HasColumnName("parts_id");
            entity.Property(e => e.PartsTypeid).HasColumnName("parts_typeid");
            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.UseDt)
                .HasPrecision(0)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("USE_DT");
            entity.Property(e => e.UseYn)
                .HasDefaultValue((short)1)
                .HasColumnName("USE_YN");
        });

        modelBuilder.Entity<PangyaChangeEmailLog>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_change_email_log", "pangya");

            entity.HasIndex(e => e.Index, "IX_pangya_change_email_log").IsUnique();

            entity.Property(e => e.ChangeTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("change_time");
            entity.Property(e => e.EmailNew)
                .IsRequired()
                .HasMaxLength(100)
                .HasDefaultValue("")
                .HasColumnName("email_new");
            entity.Property(e => e.EmailOld)
                .IsRequired()
                .HasMaxLength(100)
                .HasDefaultValue("")
                .HasColumnName("email_old");
            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.Uid).HasColumnName("uid");
        });

        modelBuilder.Entity<PangyaChangeNicknameLog>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_change_nickname_log", "pangya");

            entity.HasIndex(e => e.Index, "IX_pangya_change_nickname_log").IsUnique();

            entity.HasIndex(e => e.Index, "IX_pangya_change_nickname_log_1").IsUnique();

            entity.Property(e => e.ChangeTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("change_time");
            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.Nickname)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("")
                .HasColumnName("nickname");
            entity.Property(e => e.Uid).HasColumnName("uid");
        });

        modelBuilder.Entity<PangyaChangePwdLog>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_change_pwd_log", "pangya");

            entity.HasIndex(e => e.Uid, "IX_pangya_change_pwd_log").IsUnique();

            entity.HasIndex(e => e.Uid, "IX_pangya_change_pwd_log_1").IsUnique();

            entity.Property(e => e.ChangeDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("change_date");
            entity.Property(e => e.Count)
                .HasDefaultValue(1)
                .HasColumnName("count");
            entity.Property(e => e.LastChange)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("last_change");
            entity.Property(e => e.Uid).HasColumnName("uid");
        });

        modelBuilder.Entity<PangyaCharacterInformation>(entity =>
        {
            entity.HasKey(e => e.ItemId).HasName("PK_pangya.pangya_character_information");

            entity.ToTable("pangya_character_information", "pangya");

            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Auxparts1).HasColumnName("auxparts_1");
            entity.Property(e => e.Auxparts2).HasColumnName("auxparts_2");
            entity.Property(e => e.Auxparts3).HasColumnName("auxparts_3");
            entity.Property(e => e.Auxparts4).HasColumnName("auxparts_4");
            entity.Property(e => e.Auxparts5).HasColumnName("auxparts_5");
            entity.Property(e => e.CutIn1).HasColumnName("CutIn_1");
            entity.Property(e => e.CutIn2).HasColumnName("CutIn_2");
            entity.Property(e => e.CutIn3).HasColumnName("CutIn_3");
            entity.Property(e => e.CutIn4).HasColumnName("CutIn_4");
            entity.Property(e => e.DefaultHair).HasColumnName("default_hair");
            entity.Property(e => e.DefaultShirts).HasColumnName("default_shirts");
            entity.Property(e => e.GiftFlag).HasColumnName("gift_flag");
            entity.Property(e => e.Parts1).HasColumnName("parts_1");
            entity.Property(e => e.Parts10).HasColumnName("parts_10");
            entity.Property(e => e.Parts11).HasColumnName("parts_11");
            entity.Property(e => e.Parts12).HasColumnName("parts_12");
            entity.Property(e => e.Parts13).HasColumnName("parts_13");
            entity.Property(e => e.Parts14).HasColumnName("parts_14");
            entity.Property(e => e.Parts15).HasColumnName("parts_15");
            entity.Property(e => e.Parts16).HasColumnName("parts_16");
            entity.Property(e => e.Parts17).HasColumnName("parts_17");
            entity.Property(e => e.Parts18).HasColumnName("parts_18");
            entity.Property(e => e.Parts19).HasColumnName("parts_19");
            entity.Property(e => e.Parts2).HasColumnName("parts_2");
            entity.Property(e => e.Parts20).HasColumnName("parts_20");
            entity.Property(e => e.Parts21).HasColumnName("parts_21");
            entity.Property(e => e.Parts22).HasColumnName("parts_22");
            entity.Property(e => e.Parts23).HasColumnName("parts_23");
            entity.Property(e => e.Parts24).HasColumnName("parts_24");
            entity.Property(e => e.Parts3).HasColumnName("parts_3");
            entity.Property(e => e.Parts4).HasColumnName("parts_4");
            entity.Property(e => e.Parts5).HasColumnName("parts_5");
            entity.Property(e => e.Parts6).HasColumnName("parts_6");
            entity.Property(e => e.Parts7).HasColumnName("parts_7");
            entity.Property(e => e.Parts8).HasColumnName("parts_8");
            entity.Property(e => e.Parts9).HasColumnName("parts_9");
            entity.Property(e => e.Pcl0).HasColumnName("PCL0");
            entity.Property(e => e.Pcl1).HasColumnName("PCL1");
            entity.Property(e => e.Pcl2).HasColumnName("PCL2");
            entity.Property(e => e.Pcl3).HasColumnName("PCL3");
            entity.Property(e => e.Pcl4).HasColumnName("PCL4");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
            entity.Property(e => e.Uid).HasColumnName("UID");
        });

        modelBuilder.Entity<PangyaCharacterPartPadrao>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK_pangya_character_part_padrao_index");

            entity.ToTable("pangya_character_part_padrao", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.CharTypeid).HasColumnName("char_typeid");
            entity.Property(e => e.Parts1).HasColumnName("parts_1");
            entity.Property(e => e.Parts10).HasColumnName("parts_10");
            entity.Property(e => e.Parts11).HasColumnName("parts_11");
            entity.Property(e => e.Parts12).HasColumnName("parts_12");
            entity.Property(e => e.Parts13).HasColumnName("parts_13");
            entity.Property(e => e.Parts14).HasColumnName("parts_14");
            entity.Property(e => e.Parts15).HasColumnName("parts_15");
            entity.Property(e => e.Parts16).HasColumnName("parts_16");
            entity.Property(e => e.Parts17).HasColumnName("parts_17");
            entity.Property(e => e.Parts18).HasColumnName("parts_18");
            entity.Property(e => e.Parts19).HasColumnName("parts_19");
            entity.Property(e => e.Parts2).HasColumnName("parts_2");
            entity.Property(e => e.Parts20).HasColumnName("parts_20");
            entity.Property(e => e.Parts21).HasColumnName("parts_21");
            entity.Property(e => e.Parts22).HasColumnName("parts_22");
            entity.Property(e => e.Parts23).HasColumnName("parts_23");
            entity.Property(e => e.Parts24).HasColumnName("parts_24");
            entity.Property(e => e.Parts3).HasColumnName("parts_3");
            entity.Property(e => e.Parts4).HasColumnName("parts_4");
            entity.Property(e => e.Parts5).HasColumnName("parts_5");
            entity.Property(e => e.Parts6).HasColumnName("parts_6");
            entity.Property(e => e.Parts7).HasColumnName("parts_7");
            entity.Property(e => e.Parts8).HasColumnName("parts_8");
            entity.Property(e => e.Parts9).HasColumnName("parts_9");
        });

        modelBuilder.Entity<PangyaClubsetEnchant>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_clubset_enchant", "pangya");

            entity.Property(e => e.C0).HasColumnName("c0");
            entity.Property(e => e.C1).HasColumnName("c1");
            entity.Property(e => e.C2).HasColumnName("c2");
            entity.Property(e => e.C3).HasColumnName("c3");
            entity.Property(e => e.C4).HasColumnName("c4");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Pang).HasColumnName("pang");
            entity.Property(e => e.Uid).HasColumnName("uid");
        });

        modelBuilder.Entity<PangyaCoinCubeInfo>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_coin_cube_info", "pangya");

            entity.Property(e => e.Active)
                .HasDefaultValue((byte)1)
                .HasColumnName("active");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.UpdateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("update_date");
        });

        modelBuilder.Entity<PangyaCoinCubeLocation>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK_pangya_coin_cube_copy1_copy1");

            entity.ToTable("pangya_coin_cube_location", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.Course).HasColumnName("course");
            entity.Property(e => e.Hole).HasColumnName("hole");
            entity.Property(e => e.Rate)
                .HasDefaultValue(1L)
                .HasColumnName("rate");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("reg_date");
            entity.Property(e => e.Tipo).HasColumnName("tipo");
            entity.Property(e => e.TipoLocation).HasColumnName("tipo_location");
            entity.Property(e => e.X)
                .HasDefaultValueSql("((0.0))")
                .HasColumnName("x");
            entity.Property(e => e.Y)
                .HasDefaultValueSql("((0.0))")
                .HasColumnName("y");
            entity.Property(e => e.Z)
                .HasDefaultValueSql("((0.0))")
                .HasColumnName("z");
        });

        modelBuilder.Entity<PangyaCometRefill>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_comet_refill", "pangya");

            entity.HasIndex(e => e.Index, "IX_pangya_comet_refill").IsUnique();

            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.Max).HasColumnName("max");
            entity.Property(e => e.Min).HasColumnName("min");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
        });

        modelBuilder.Entity<PangyaCommand>(entity =>
        {
            entity.HasKey(e => e.Idx).HasName("PK_pangya_command_idx");

            entity.ToTable("pangya_command", "pangya");

            entity.Property(e => e.Idx).HasColumnName("idx");
            entity.Property(e => e.Arg1).HasColumnName("arg1");
            entity.Property(e => e.Arg2).HasColumnName("arg2");
            entity.Property(e => e.Arg3).HasColumnName("arg3");
            entity.Property(e => e.Arg4).HasColumnName("arg4");
            entity.Property(e => e.Arg5).HasColumnName("arg5");
            entity.Property(e => e.CommandId).HasColumnName("command_id");
            entity.Property(e => e.Flag).HasColumnName("flag");
            entity.Property(e => e.RegDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("regDate");
            entity.Property(e => e.ReserveDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("reserveDate");
            entity.Property(e => e.Target).HasColumnName("target");
            entity.Property(e => e.Valid)
                .HasDefaultValue((short)1)
                .HasColumnName("valid");
        });

        modelBuilder.Entity<PangyaCommandGmLog>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_command_gm_log", "pangya");

            entity.HasIndex(e => e.Index, "IX_pangya_command_gm_log").IsUnique();

            entity.Property(e => e.Capability).HasColumnName("capability");
            entity.Property(e => e.CommandType).HasColumnName("command_type");
            entity.Property(e => e.GmNick)
                .IsRequired()
                .HasMaxLength(120)
                .IsUnicode(false)
                .HasColumnName("gm_nick");
            entity.Property(e => e.GmUid).HasColumnName("gm_uid");
            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.ItemQntd).HasColumnName("item_qntd");
            entity.Property(e => e.ItemTypeid).HasColumnName("item_typeid");
            entity.Property(e => e.NickGift)
                .IsRequired()
                .HasMaxLength(120)
                .IsUnicode(false)
                .HasColumnName("nick_gift");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("reg_date");
            entity.Property(e => e.UidGift).HasColumnName("uid_gift");
        });

        modelBuilder.Entity<PangyaConfig>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_config", "pangya");

            entity.HasIndex(e => e.Uid, "IX_pangya_config").IsUnique();

            entity.Property(e => e.BotGmevent).HasColumnName("BotGMEvent");
            entity.Property(e => e.ChuvaRate).HasDefaultValue((short)100);
            entity.Property(e => e.ClubMasteryRate).HasDefaultValue((short)100);
            entity.Property(e => e.ExpRate).HasDefaultValue((short)100);
            entity.Property(e => e.MemorialShopRate).HasDefaultValue((short)100);
            entity.Property(e => e.PangRate).HasDefaultValue((short)100);
            entity.Property(e => e.PapelShopCookieItemRate).HasDefaultValue((short)100);
            entity.Property(e => e.PapelShopRareItemRate).HasDefaultValue((short)100);
            entity.Property(e => e.ScratchyPorPointRate).HasDefaultValue((short)100);
            entity.Property(e => e.TreasureRate).HasDefaultValue((short)100);
            entity.Property(e => e.Uid).HasColumnName("UID");
        });

        modelBuilder.Entity<PangyaCookiePointItemLog>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_cookie_point_item_log", "pangya");

            entity.HasIndex(e => e.Index, "IX_pangya_cookie_point_item_log").IsUnique();

            entity.Property(e => e.CpIdLog)
                .HasDefaultValue(0L)
                .HasColumnName("cp_id_log");
            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.Price)
                .HasDefaultValue(0L)
                .HasColumnName("price");
            entity.Property(e => e.Qnty)
                .HasDefaultValue(0)
                .HasColumnName("qnty");
            entity.Property(e => e.Typeid)
                .HasDefaultValue(0)
                .HasColumnName("typeid");
        });

        modelBuilder.Entity<PangyaCookiePointLog>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_cookie_point_log", "pangya");

            entity.HasIndex(e => e.Id, "IX_pangya_cookie_point_log").IsUnique();

            entity.Property(e => e.Cookie)
                .HasDefaultValue(0L)
                .HasColumnName("cookie");
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.ItemQnty)
                .HasDefaultValue(0)
                .HasColumnName("item_qnty");
            entity.Property(e => e.MailId)
                .HasDefaultValue(-1)
                .HasColumnName("mail_id");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("reg_date");
            entity.Property(e => e.Type)
                .HasDefaultValue((byte)0)
                .HasColumnName("type");
            entity.Property(e => e.Uid)
                .HasDefaultValue(0)
                .HasColumnName("uid");
        });

        modelBuilder.Entity<PangyaCounterItem>(entity =>
        {
            entity.HasKey(e => e.CountId).HasName("PK_pangya.counter_item");

            entity.ToTable("pangya_counter_item", "pangya");

            entity.HasIndex(e => e.CountId, "IX_pangya.counter_item").IsUnique();

            entity.Property(e => e.CountId).HasColumnName("Count_ID");
            entity.Property(e => e.Active)
                .HasDefaultValue(1)
                .HasColumnName("active");
            entity.Property(e => e.CountNumItem).HasColumnName("Count_Num_Item");
            entity.Property(e => e.TypeId).HasColumnName("TypeID");
            entity.Property(e => e.Uid).HasColumnName("UID");
        });

        modelBuilder.Entity<PangyaCouponDesconto>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_coupon_desconto", "pangya");

            entity.Property(e => e.Nome)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Typeid).HasColumnName("typeid");
            entity.Property(e => e.Valor).HasColumnName("valor");
        });

        modelBuilder.Entity<PangyaCourseCubeCoinTemporadum>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK_pangya_course_cube_coin_temporada_index");

            entity.ToTable("pangya_course_cube_coin_temporada", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.Active)
                .HasDefaultValue((short)1)
                .HasColumnName("active");
            entity.Property(e => e.Course).HasColumnName("course");
        });

        modelBuilder.Entity<PangyaCourseRewardTreasure>(entity =>
        {
            entity.HasKey(e => e.Course).HasName("PK_pangya_course_reward_treasure_COURSE");

            entity.ToTable("pangya_course_reward_treasure", "pangya");

            entity.Property(e => e.Course)
                .ValueGeneratedNever()
                .HasColumnName("COURSE");
            entity.Property(e => e.Pangreward).HasColumnName("PANGREWARD");
        });

        modelBuilder.Entity<PangyaCubeCoinLocation>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK_pangya_cube_coin_location_index");

            entity.ToTable("pangya_cube_coin_location", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.Config2).HasColumnName("config2");
            entity.Property(e => e.Course).HasColumnName("course");
            entity.Property(e => e.Hole).HasColumnName("hole");
            entity.Property(e => e.Tipo).HasColumnName("tipo");
            entity.Property(e => e.X).HasColumnName("x");
            entity.Property(e => e.Y).HasColumnName("y");
            entity.Property(e => e.Z).HasColumnName("z");
        });

        modelBuilder.Entity<PangyaDailyQuest>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_daily_quest", "pangya");

            entity.Property(e => e.AchieveQuest1).HasColumnName("achieve_quest_1");
            entity.Property(e => e.AchieveQuest2).HasColumnName("achieve_quest_2");
            entity.Property(e => e.AchieveQuest3).HasColumnName("achieve_quest_3");
            entity.Property(e => e.RegDate).HasColumnName("Reg_Date");
        });

        modelBuilder.Entity<PangyaDailyQuestPlayer>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("PK_pangya_daily_quest_player_uid");

            entity.ToTable("pangya_daily_quest_player", "pangya");

            entity.Property(e => e.Uid)
                .ValueGeneratedNever()
                .HasColumnName("uid");
            entity.Property(e => e.LastQuestAccept)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("last_quest_accept");
            entity.Property(e => e.TodayQuest)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("today_quest");
        });

        modelBuilder.Entity<PangyaDolfiniLocker>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("PK_pangya_dolfini_locker_uid");

            entity.ToTable("pangya_dolfini_locker", "pangya");

            entity.Property(e => e.Uid)
                .ValueGeneratedNever()
                .HasColumnName("uid");
            entity.Property(e => e.Locker).HasColumnName("locker");
            entity.Property(e => e.Pang).HasColumnName("pang");
            entity.Property(e => e.Senha)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("senha");
        });

        modelBuilder.Entity<PangyaDolfiniLockerItem>(entity =>
        {
            entity.HasKey(e => e.Idx).HasName("PK_pangya_dolfini_locker_item_idx");

            entity.ToTable("pangya_dolfini_locker_item", "pangya");

            entity.Property(e => e.Idx)
                .ValueGeneratedOnAdd()
                .HasColumnType("numeric(20, 0)")
                .HasColumnName("idx");
            entity.Property(e => e.Flag)
                .HasDefaultValue((short)1)
                .HasColumnName("flag");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Uid).HasColumnName("uid");
        });

        modelBuilder.Entity<PangyaDonationEpin>(entity =>
        {
            entity.HasKey(e => e.Index);

            entity.ToTable("pangya_donation_epin", "pangya");

            entity.HasIndex(e => e.Epin, "IX_pangya_donation_epin").IsUnique();

            entity.HasIndex(e => e.DonationId, "IX_pangya_donation_epin_1").IsUnique();

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.DonationId).HasColumnName("donation_id");
            entity.Property(e => e.Epin)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("epin");
            entity.Property(e => e.Qntd).HasColumnName("qntd");
            entity.Property(e => e.RetriveUid).HasColumnName("retrive_uid");
            entity.Property(e => e.Uid).HasColumnName("uid");
            entity.Property(e => e.Valid)
                .HasDefaultValue((byte)1)
                .HasColumnName("valid");
        });

        modelBuilder.Entity<PangyaDonationItemLog>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_donation_item_log", "pangya");

            entity.HasIndex(e => e.Index, "IX_pangya_donation_item_log").IsUnique();

            entity.HasIndex(e => e.Index, "IX_pangya_donation_item_log_1").IsUnique();

            entity.Property(e => e.DonationId).HasColumnName("donation_id");
            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.ItemQntd).HasColumnName("item_qntd");
            entity.Property(e => e.ItemTypeid).HasColumnName("item_typeid");
        });

        modelBuilder.Entity<PangyaDonationLog>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_donation_log", "pangya");

            entity.HasIndex(e => e.Index, "IX_pangya_donation_log").IsUnique();

            entity.HasIndex(e => e.Index, "IX_pangya_donation_log_1").IsUnique();

            entity.Property(e => e.AdmUid)
                .HasComment("quem registrou a do doação para o usuário")
                .HasColumnName("ADM_uid");
            entity.Property(e => e.Cash).HasColumnName("cash");
            entity.Property(e => e.CookiePoint).HasColumnName("cookie_point");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.Obs)
                .HasMaxLength(500)
                .HasColumnName("obs");
            entity.Property(e => e.Plataforma)
                .HasComment("0 nenhum, 1 Paypal, 2 PagSeguro")
                .HasColumnName("plataforma");
            entity.Property(e => e.RedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("red_date");
            entity.Property(e => e.Uid).HasColumnName("uid");
        });

        modelBuilder.Entity<PangyaDonationNew>(entity =>
        {
            entity.HasKey(e => e.Index);

            entity.ToTable("pangya_donation_new", "pangya");

            entity.HasIndex(e => e.Code, "IX_pangya_donation_new").IsUnique();

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("email");
            entity.Property(e => e.EpinId)
                .HasDefaultValue(-1L)
                .HasColumnName("epin_id");
            entity.Property(e => e.Escrow).HasColumnName("escrow");
            entity.Property(e => e.GrossAmount).HasColumnName("gross_amount");
            entity.Property(e => e.NetAmount).HasColumnName("net_amount");
            entity.Property(e => e.Plataforma).HasColumnName("plataforma");
            entity.Property(e => e.Reference)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("reference");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("reg_date");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.Uid)
                .HasDefaultValue(-1)
                .HasColumnName("uid");
            entity.Property(e => e.Update).HasColumnName("update");
        });

        modelBuilder.Entity<PangyaEventItensSite>(entity =>
        {
            entity.HasKey(e => e.ItemId).HasName("PK__pangya_e__ADFD89A0F35AC350");

            entity.ToTable("pangya_event_itens_site", "pangya");

            entity.HasIndex(e => e.EventoId, "IDX_EVENTO_ID");

            entity.Property(e => e.ItemId).HasColumnName("ITEM_ID");
            entity.Property(e => e.EventoId).HasColumnName("EVENTO_ID");
            entity.Property(e => e.NomeItem)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("NOME_ITEM");
            entity.Property(e => e.QntItem).HasColumnName("QNT_ITEM");
            entity.Property(e => e.QntJogada).HasColumnName("QNT_JOGADA");
            entity.Property(e => e.Typeid).HasColumnName("TYPEID");

            entity.HasOne(d => d.Evento).WithMany(p => p.PangyaEventItensSites)
                .HasForeignKey(d => d.EventoId)
                .HasConstraintName("FK_EVENTO");
        });

        modelBuilder.Entity<PangyaEventSite>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__pangya_e__3214EC27E5986E28");

            entity.ToTable("pangya_event_site", "pangya");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.DataFim)
                .HasColumnType("datetime")
                .HasColumnName("DATA_FIM");
            entity.Property(e => e.DataInicial)
                .HasColumnType("datetime")
                .HasColumnName("DATA_INICIAL");
            entity.Property(e => e.DataRegistro)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("DATA_REGISTRO");
            entity.Property(e => e.Itens).HasColumnName("ITENS");
            entity.Property(e => e.NomeEvento)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("NOME_EVENTO");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("STATUS");
            entity.Property(e => e.TipoEvento)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("TIPO_EVENTO");
        });

        modelBuilder.Entity<PangyaExceptionLog>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_exception_log", "pangya");

            entity.Property(e => e.CreateDate).HasColumnType("datetime");
            entity.Property(e => e.ExceptionId)
                .ValueGeneratedOnAdd()
                .HasColumnName("ExceptionID");
            entity.Property(e => e.ExceptionMessage)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.Server)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<PangyaFastPassEvent>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_fast_pass_event", "pangya");

            entity.Property(e => e.EndDate).HasColumnName("END_DATE");
            entity.Property(e => e.HolesCounter).HasColumnName("HOLES_COUNTER");
            entity.Property(e => e.HolesInit).HasColumnName("HOLES_INIT");
            entity.Property(e => e.RegDate).HasColumnName("REG_DATE");
            entity.Property(e => e.Uid).HasColumnName("UID");
        });

        modelBuilder.Entity<PangyaFriendList>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_friend_list", "pangya");

            entity.Property(e => e.Apelido)
                .IsRequired()
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasDefaultValue("Friend")
                .HasColumnName("apelido");
            entity.Property(e => e.Flag1)
                .HasDefaultValue((short)-1)
                .HasColumnName("flag1");
            entity.Property(e => e.Flag5).HasColumnName("flag5");
            entity.Property(e => e.StateFlag).HasColumnName("state_flag");
            entity.Property(e => e.Uid).HasColumnName("uid");
            entity.Property(e => e.UidFriend).HasColumnName("uid_friend");
            entity.Property(e => e.Unknown1)
                .HasDefaultValue(-1)
                .HasColumnName("unknown1");
            entity.Property(e => e.Unknown2).HasColumnName("unknown2");
            entity.Property(e => e.Unknown3)
                .HasDefaultValue(-1)
                .HasColumnName("unknown3");
            entity.Property(e => e.Unknown4).HasColumnName("unknown4");
            entity.Property(e => e.Unknown5).HasColumnName("unknown5");
            entity.Property(e => e.Unknown6).HasColumnName("unknown6");
        });

        modelBuilder.Entity<PangyaGacha>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK_pangya_gacha_index");

            entity.ToTable("pangya_gacha", "pangya");

            entity.Property(e => e.Index)
                .ValueGeneratedNever()
                .HasColumnName("index");
            entity.Property(e => e.Charno)
                .HasDefaultValue(1)
                .HasColumnName("charno");
            entity.Property(e => e.Coin).HasColumnName("coin");
            entity.Property(e => e.Numero).HasColumnName("numero");
            entity.Property(e => e.Rate)
                .HasDefaultValue(100)
                .HasColumnName("rate");
            entity.Property(e => e.Shop)
                .HasDefaultValue(1)
                .HasColumnName("shop");
            entity.Property(e => e.Townno)
                .HasDefaultValue(1)
                .HasColumnName("townno");
        });

        modelBuilder.Entity<PangyaGachaCoin>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK_pangya_gacha_coin_index");

            entity.ToTable("pangya_gacha_coin", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.Preco).HasColumnName("preco");
            entity.Property(e => e.Qntd).HasColumnName("qntd");
            entity.Property(e => e.Tipo).HasColumnName("tipo");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
        });

        modelBuilder.Entity<PangyaGachaItem>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK_pangya_gacha_items_index");

            entity.ToTable("pangya_gacha_items", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.GachaNum).HasColumnName("gacha_num");
            entity.Property(e => e.Nome)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("nome");
            entity.Property(e => e.Premio).HasColumnName("premio");
            entity.Property(e => e.Probabilidade).HasColumnName("probabilidade");
            entity.Property(e => e.Qntd).HasColumnName("qntd");
            entity.Property(e => e.Secret).HasColumnName("secret");
            entity.Property(e => e.Tipo).HasColumnName("tipo");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
        });

        modelBuilder.Entity<PangyaGachaJpAllItemList>(entity =>
        {
            entity.HasKey(e => e.Index);

            entity.ToTable("pangya_gacha_jp_all_item_list", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.CharType).HasColumnName("char_type");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("reg_date");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
        });

        modelBuilder.Entity<PangyaGachaJpItemList>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK_pangya.pangya_gacha_jp_item_list");

            entity.ToTable("pangya_gacha_jp_item_list", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.Active)
                .HasDefaultValue((byte)1)
                .HasColumnName("active");
            entity.Property(e => e.GachaNum).HasColumnName("gacha_num");
            entity.Property(e => e.Qnty1).HasColumnName("qnty_1");
            entity.Property(e => e.Qnty2).HasColumnName("qnty_2");
            entity.Property(e => e.RarityType).HasColumnName("rarity_type");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("reg_date");
            entity.Property(e => e.Typeid1).HasColumnName("typeid_1");
            entity.Property(e => e.Typeid2).HasColumnName("typeid_2");
        });

        modelBuilder.Entity<PangyaGachaJpPlayerWin>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK_pangya.pangya_gacha_jp_player_win");

            entity.ToTable("pangya_gacha_jp_player_win", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.GachaNum).HasColumnName("gacha_num");
            entity.Property(e => e.Qnty).HasColumnName("qnty");
            entity.Property(e => e.RarityType).HasColumnName("rarity_type");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("reg_date");
            entity.Property(e => e.SendMail).HasColumnName("send_mail");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
            entity.Property(e => e.Uid).HasColumnName("uid");
            entity.Property(e => e.Valid).HasColumnName("valid");
        });

        modelBuilder.Entity<PangyaGachaJpRate>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_gacha_jp_rate", "pangya");

            entity.Property(e => e.GachaNum).HasColumnName("gacha_num");
            entity.Property(e => e.RateNormal)
                .HasDefaultValue(100)
                .HasColumnName("rate_normal");
            entity.Property(e => e.RateRare)
                .HasDefaultValue(100)
                .HasColumnName("rate_rare");
        });

        modelBuilder.Entity<PangyaGachaUserKey>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("PK_pangya_gacha_user_key_uid");

            entity.ToTable("pangya_gacha_user_key", "pangya");

            entity.Property(e => e.Uid)
                .ValueGeneratedNever()
                .HasColumnName("uid");
            entity.Property(e => e.AttFlag).HasColumnName("att_flag");
            entity.Property(e => e.CoinCountEntrou).HasColumnName("coin_count_entrou");
            entity.Property(e => e.DateKeyGeneration)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("date_key_generation");
            entity.Property(e => e.Key)
                .IsRequired()
                .HasMaxLength(22)
                .IsUnicode(false)
                .HasColumnName("key");
        });

        modelBuilder.Entity<PangyaGachaUserWon>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK_pangya_gacha_user_won_index");

            entity.ToTable("pangya_gacha_user_won", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.GachaNum).HasColumnName("gacha_num");
            entity.Property(e => e.GetDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("get_date");
            entity.Property(e => e.ItemName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("item_name");
            entity.Property(e => e.ItemTypeid).HasColumnName("item_typeid");
            entity.Property(e => e.Uid).HasColumnName("uid");
        });

        modelBuilder.Entity<PangyaGiftTable>(entity =>
        {
            entity.HasKey(e => e.MsgId).HasName("PK_pangya_gift_table_Msg_ID");

            entity.ToTable("pangya_gift_table", "pangya");

            entity.Property(e => e.MsgId).HasColumnName("Msg_ID");
            entity.Property(e => e.ContadorVista).HasColumnName("Contador_Vista");
            entity.Property(e => e.Enddate)
                .HasPrecision(0)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("enddate");
            entity.Property(e => e.Fromid)
                .IsRequired()
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("fromid");
            entity.Property(e => e.Giftdate)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("giftdate");
            entity.Property(e => e.LidaYn).HasColumnName("Lida_YN");
            entity.Property(e => e.Message)
                .IsRequired()
                .HasMaxLength(500)
                .HasDefaultValue("")
                .HasColumnName("message");
            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.Valid)
                .HasDefaultValue((short)1)
                .HasColumnName("valid");
        });

        modelBuilder.Entity<PangyaGmGiftWebLog>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_gm_gift_web_log", "pangya");

            entity.HasIndex(e => e.Index, "IX_pangya_gm_gift_web_log").IsUnique();

            entity.HasIndex(e => e.Index, "IX_pangya_gm_gift_web_log_1").IsUnique();

            entity.Property(e => e.GmUid).HasColumnName("GM_UID");
            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.MsgId).HasColumnName("MSG_ID");
            entity.Property(e => e.PlayerUid).HasColumnName("PLAYER_UID");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("REG_DATE");
        });

        modelBuilder.Entity<PangyaGoldenTimeInfo>(entity =>
        {
            entity.HasKey(e => e.Index);

            entity.ToTable("pangya_golden_time_info", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.Begin).HasColumnName("begin");
            entity.Property(e => e.End).HasColumnName("end");
            entity.Property(e => e.IsEnd).HasColumnName("is_end");
            entity.Property(e => e.Rate)
                .HasDefaultValue(1)
                .HasColumnName("rate");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("reg_date");
            entity.Property(e => e.Type).HasColumnName("type");
        });

        modelBuilder.Entity<PangyaGoldenTimeItem>(entity =>
        {
            entity.HasKey(e => e.Index);

            entity.ToTable("pangya_golden_time_item", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.GoldenTimeId).HasColumnName("golden_time_id");
            entity.Property(e => e.Qntd).HasColumnName("qntd");
            entity.Property(e => e.QntdTime).HasColumnName("qntd_time");
            entity.Property(e => e.Rate)
                .HasDefaultValue(100)
                .HasColumnName("rate");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("reg_date");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
        });

        modelBuilder.Entity<PangyaGoldenTimeRound>(entity =>
        {
            entity.HasKey(e => e.Index);

            entity.ToTable("pangya_golden_time_round", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.GoldenTimeId).HasColumnName("golden_time_id");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("reg_date");
            entity.Property(e => e.Time).HasColumnName("time");
        });

        modelBuilder.Entity<PangyaGrandZodiacPonto>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("PK_pangya_grand_zodiac_pontos_uid");

            entity.ToTable("pangya_grand_zodiac_pontos", "pangya");

            entity.Property(e => e.Uid)
                .ValueGeneratedNever()
                .HasColumnName("uid");
            entity.Property(e => e.Pontos).HasColumnName("pontos");
        });

        modelBuilder.Entity<PangyaGrandZodiacTime>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK_pangya_grand_zodiac_times_index");

            entity.ToTable("pangya_grand_zodiac_times", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.FimTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("fim_time");
            entity.Property(e => e.InicioTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("inicio_time");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.Valid)
                .HasDefaultValue((short)1)
                .HasColumnName("valid");
        });

        modelBuilder.Entity<PangyaGrandprixClear>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK_pangya_grandprix_clear_index");

            entity.ToTable("pangya_grandprix_clear", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.Flag).HasColumnName("flag");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
            entity.Property(e => e.Uid).HasColumnName("uid");
        });

        modelBuilder.Entity<PangyaGrandprixEventConfig>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK_pangya_grandprix_event_config_index");

            entity.ToTable("pangya_grandprix_event_config", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.Active).HasDefaultValue((short)1);
            entity.Property(e => e.Flag).HasColumnName("flag");
        });

        modelBuilder.Entity<PangyaGuild>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_guild", "pangya");

            entity.HasIndex(e => e.GuildUid, "IX_pangya_guild").IsUnique();

            entity.Property(e => e.GuildAcceptDate).HasColumnName("GUILD_ACCEPT_DATE");
            entity.Property(e => e.GuildClosureDate).HasColumnName("GUILD_CLOSURE_DATE");
            entity.Property(e => e.GuildConditionLevel).HasColumnName("GUILD_CONDITION_LEVEL");
            entity.Property(e => e.GuildDraw).HasColumnName("GUILD_DRAW");
            entity.Property(e => e.GuildFlag).HasColumnName("GUILD_FLAG");
            entity.Property(e => e.GuildId)
                .IsRequired()
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasDefaultValue("")
                .HasColumnName("GUILD_ID");
            entity.Property(e => e.GuildInfo)
                .IsRequired()
                .HasMaxLength(110)
                .IsUnicode(false)
                .HasDefaultValue("")
                .HasColumnName("GUILD_INFO");
            entity.Property(e => e.GuildIntroImg)
                .HasMaxLength(50)
                .HasColumnName("GUILD_INTRO_IMG");
            entity.Property(e => e.GuildLeader).HasColumnName("GUILD_LEADER");
            entity.Property(e => e.GuildLose).HasColumnName("GUILD_LOSE");
            entity.Property(e => e.GuildMarkImg)
                .IsRequired()
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasDefaultValue("guildmark")
                .HasColumnName("GUILD_MARK_IMG");
            entity.Property(e => e.GuildMarkImgIdx).HasColumnName("GUILD_MARK_IMG_IDX");
            entity.Property(e => e.GuildName)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("GUILD_NAME");
            entity.Property(e => e.GuildNewMarkIdx).HasColumnName("GUILD_NEW_MARK_IDX");
            entity.Property(e => e.GuildNotice)
                .IsRequired()
                .HasMaxLength(110)
                .IsUnicode(false)
                .HasDefaultValue("")
                .HasColumnName("GUILD_NOTICE");
            entity.Property(e => e.GuildPang).HasColumnName("GUILD_PANG");
            entity.Property(e => e.GuildPermitionJoin)
                .HasDefaultValue((byte)1)
                .HasColumnName("GUILD_PERMITION_JOIN");
            entity.Property(e => e.GuildPoint).HasColumnName("GUILD_POINT");
            entity.Property(e => e.GuildRegDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("GUILD_REG_DATE");
            entity.Property(e => e.GuildState).HasColumnName("GUILD_STATE");
            entity.Property(e => e.GuildSubMaster).HasColumnName("GUILD_SUB_MASTER");
            entity.Property(e => e.GuildUid)
                .ValueGeneratedOnAdd()
                .HasColumnName("GUILD_UID");
            entity.Property(e => e.GuildWin).HasColumnName("GUILD_WIN");
        });

        modelBuilder.Entity<PangyaGuildAtividadePlayer>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_guild_atividade_player", "pangya");

            entity.HasIndex(e => e.Idx, "IX_pangya_guild_atividade_player").IsUnique();

            entity.Property(e => e.Flag).HasColumnName("FLAG");
            entity.Property(e => e.GuildUid).HasColumnName("GUILD_UID");
            entity.Property(e => e.Idx)
                .ValueGeneratedOnAdd()
                .HasColumnName("IDX");
            entity.Property(e => e.RegDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("REG_DATE");
            entity.Property(e => e.Uid).HasColumnName("UID");
        });

        modelBuilder.Entity<PangyaGuildBb>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_guild_bbs", "pangya");

            entity.HasIndex(e => e.Seq, "IX_pangya_guild_bbs").IsUnique();

            entity.HasIndex(e => e.Seq, "IX_pangya_guild_bbs_1").IsUnique();

            entity.HasIndex(e => e.OwnerUid, "IX_pangya_guild_bbs_2");

            entity.Property(e => e.OwnerUid).HasColumnName("OWNER_UID");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("REG_DATE");
            entity.Property(e => e.Seq)
                .ValueGeneratedOnAdd()
                .HasColumnName("SEQ");
            entity.Property(e => e.State)
                .HasDefaultValue((byte)1)
                .HasColumnName("STATE");
            entity.Property(e => e.Text)
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnName("TEXT");
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("TITLE");
            entity.Property(e => e.Type).HasColumnName("TYPE");
            entity.Property(e => e.Views).HasColumnName("VIEWS");
        });

        modelBuilder.Entity<PangyaGuildBbsRe>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_guild_bbs_res", "pangya");

            entity.HasIndex(e => e.Seq, "IX_pangya_guild_bbs_res").IsUnique();

            entity.HasIndex(e => e.Seq, "IX_pangya_guild_bbs_res_1").IsUnique();

            entity.HasIndex(e => e.BbsSeq, "IX_pangya_guild_bbs_res_2");

            entity.HasIndex(e => e.OwnerUid, "IX_pangya_guild_bbs_res_3");

            entity.Property(e => e.BbsSeq).HasColumnName("BBS_SEQ");
            entity.Property(e => e.OwnerUid).HasColumnName("OWNER_UID");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("REG_DATE");
            entity.Property(e => e.Seq)
                .ValueGeneratedOnAdd()
                .HasColumnName("SEQ");
            entity.Property(e => e.State)
                .HasDefaultValue((byte)1)
                .HasColumnName("STATE");
            entity.Property(e => e.Text)
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnName("TEXT");
        });

        modelBuilder.Entity<PangyaGuildIntroImgLog>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_guild_intro_img_log", "pangya");

            entity.HasIndex(e => e.Index, "IX_pangya_guild_intro_img_log").IsUnique();

            entity.HasIndex(e => e.Index, "IX_pangya_guild_intro_img_log_1").IsUnique();

            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.IntroImg)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("intro_img");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("reg_date");
        });

        modelBuilder.Entity<PangyaGuildMarkLog>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_guild_mark_log", "pangya");

            entity.HasIndex(e => e.Index, "IX_pangya.pangya_guild_mark_log").IsUnique();

            entity.HasIndex(e => e.Index, "IX_pangya.pangya_guild_mark_log_1").IsUnique();

            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.MarkIdx).HasColumnName("mark_idx");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("reg_date");
        });

        modelBuilder.Entity<PangyaGuildMatch>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_guild_match", "pangya");

            entity.HasIndex(e => e.Index, "IX_pangya_guild_match").IsUnique();

            entity.HasIndex(e => e.Index, "IX_pangya_guild_match_1").IsUnique();

            entity.HasIndex(e => e.Guild1Uid, "IX_pangya_guild_match_2");

            entity.HasIndex(e => e.Guild2Uid, "IX_pangya_guild_match_3");

            entity.Property(e => e.Guild1Pang).HasColumnName("guild_1_pang");
            entity.Property(e => e.Guild1Point).HasColumnName("guild_1_point");
            entity.Property(e => e.Guild1Uid).HasColumnName("guild_1_uid");
            entity.Property(e => e.Guild2Pang).HasColumnName("guild_2_pang");
            entity.Property(e => e.Guild2Point).HasColumnName("guild_2_point");
            entity.Property(e => e.Guild2Uid).HasColumnName("guild_2_uid");
            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("reg_date");
        });

        modelBuilder.Entity<PangyaGuildMember>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_guild_member", "pangya");

            entity.Property(e => e.GuildPang).HasColumnName("GUILD_PANG");
            entity.Property(e => e.GuildPoint).HasColumnName("GUILD_POINT");
            entity.Property(e => e.GuildUid).HasColumnName("GUILD_UID");
            entity.Property(e => e.MemberFlag).HasColumnName("MEMBER_FLAG");
            entity.Property(e => e.MemberMsg)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("MEMBER_MSG");
            entity.Property(e => e.MemberStateFlag)
                .HasDefaultValue(3)
                .HasColumnName("MEMBER_STATE_FLAG");
            entity.Property(e => e.MemberUid).HasColumnName("MEMBER_UID");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("REG_DATE");
        });

        modelBuilder.Entity<PangyaGuildNotice>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_guild_notice", "pangya");

            entity.HasIndex(e => e.Seq, "IX_pangya_guild_notice").IsUnique();

            entity.HasIndex(e => e.Seq, "IX_pangya_guild_notice_1").IsUnique();

            entity.HasIndex(e => e.GuildUid, "IX_pangya_guild_notice_2");

            entity.HasIndex(e => e.OwnerUid, "IX_pangya_guild_notice_3");

            entity.Property(e => e.GuildUid).HasColumnName("GUILD_UID");
            entity.Property(e => e.OwnerUid).HasColumnName("OWNER_UID");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("REG_DATE");
            entity.Property(e => e.Seq)
                .ValueGeneratedOnAdd()
                .HasColumnName("SEQ");
            entity.Property(e => e.State)
                .HasDefaultValue((byte)1)
                .HasColumnName("STATE");
            entity.Property(e => e.Text)
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnName("TEXT");
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("TITLE");
            entity.Property(e => e.Views).HasColumnName("VIEWS");
        });

        modelBuilder.Entity<PangyaGuildPrivateBb>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_guild_private_bbs", "pangya");

            entity.HasIndex(e => e.Seq, "IX_pangya_guild_private_bbs").IsUnique();

            entity.HasIndex(e => e.Seq, "IX_pangya_guild_private_bbs_1").IsUnique();

            entity.HasIndex(e => e.GuildUid, "IX_pangya_guild_private_bbs_2");

            entity.HasIndex(e => e.OwnerUid, "IX_pangya_guild_private_bbs_3");

            entity.Property(e => e.GuildUid).HasColumnName("GUILD_UID");
            entity.Property(e => e.OwnerUid).HasColumnName("OWNER_UID");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("REG_DATE");
            entity.Property(e => e.Seq)
                .ValueGeneratedOnAdd()
                .HasColumnName("SEQ");
            entity.Property(e => e.State)
                .HasDefaultValue((byte)1)
                .HasColumnName("STATE");
            entity.Property(e => e.Text)
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnName("TEXT");
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("TITLE");
            entity.Property(e => e.Views).HasColumnName("VIEWS");
        });

        modelBuilder.Entity<PangyaGuildPrivateBbsRe>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_guild_private_bbs_res", "pangya");

            entity.HasIndex(e => e.Seq, "IX_pangya_guild_private_bbs_res").IsUnique();

            entity.HasIndex(e => e.Seq, "IX_pangya_guild_private_bbs_res_1").IsUnique();

            entity.HasIndex(e => e.GuildBbsSeq, "IX_pangya_guild_private_bbs_res_2");

            entity.HasIndex(e => e.OwnerUid, "IX_pangya_guild_private_bbs_res_3");

            entity.Property(e => e.GuildBbsSeq).HasColumnName("GUILD_BBS_SEQ");
            entity.Property(e => e.OwnerUid).HasColumnName("OWNER_UID");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("REG_DATE");
            entity.Property(e => e.Seq)
                .ValueGeneratedOnAdd()
                .HasColumnName("SEQ");
            entity.Property(e => e.State)
                .HasDefaultValue((byte)1)
                .HasColumnName("STATE");
            entity.Property(e => e.Text)
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnName("TEXT");
        });

        modelBuilder.Entity<PangyaGuildRanking>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK_pangya.pangya_guild_ranking");

            entity.ToTable("pangya_guild_ranking", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.GuildUid).HasColumnName("GUILD_UID");
            entity.Property(e => e.LastRank).HasColumnName("LAST_RANK");
            entity.Property(e => e.Rank)
                .HasDefaultValue(1)
                .HasColumnName("RANK");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("REG_DATE");
        });

        modelBuilder.Entity<PangyaGuildUpdateActivity>(entity =>
        {
            entity.HasKey(e => e.Index);

            entity.ToTable("pangya_guild_update_activity", "pangya");

            entity.HasIndex(e => e.OwnerUpdate, "IX_pangya_guild_update_activity");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.GuildUid).HasColumnName("GUILD_UID");
            entity.Property(e => e.OwnerUpdate).HasColumnName("OWNER_UPDATE");
            entity.Property(e => e.PlayerUid).HasColumnName("PLAYER_UID");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("REG_DATE");
            entity.Property(e => e.State).HasColumnName("STATE");
            entity.Property(e => e.TypeUpdate).HasColumnName("TYPE_UPDATE");
        });

        modelBuilder.Entity<PangyaGzEvent2016121600RareWin>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK_pangya_gz_event_2016121600_rare_win_index");

            entity.ToTable("pangya_gz_event_2016121600_rare_win", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.ItemTypeid)
                .HasDefaultValue(0)
                .HasColumnName("item_typeid");
            entity.Property(e => e.Uid).HasColumnName("uid");
            entity.Property(e => e.WinDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("win_date");
        });

        modelBuilder.Entity<PangyaHioEvent>(entity =>
        {
            entity.HasKey(e => e.Index);

            entity.ToTable("pangya_hio_event", "pangya");

            entity.Property(e => e.Index).HasColumnName("INDEX");
            entity.Property(e => e.FinishDate).HasColumnName("FINISH_DATE");
            entity.Property(e => e.ProcessHios).HasColumnName("PROCESS_HIOS");
            entity.Property(e => e.StartHios).HasColumnName("START_HIOS");
            entity.Property(e => e.Status).HasColumnName("STATUS");
            entity.Property(e => e.Uid).HasColumnName("UID");
        });

        modelBuilder.Entity<PangyaHioEventItem>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_hio_event_items", "pangya");

            entity.Property(e => e.EndEvent).HasColumnName("END_EVENT");
            entity.Property(e => e.EventDescription)
                .HasMaxLength(255)
                .HasColumnName("EVENT_DESCRIPTION");
            entity.Property(e => e.HioCount).HasColumnName("HIO_COUNT");
            entity.Property(e => e.Idx)
                .ValueGeneratedOnAdd()
                .HasColumnName("IDX");
            entity.Property(e => e.ItemName)
                .HasMaxLength(50)
                .HasColumnName("ITEM_NAME");
            entity.Property(e => e.ItemQntd).HasColumnName("ITEM_QNTD");
            entity.Property(e => e.ItemQntdTime).HasColumnName("ITEM_QNTD_TIME");
            entity.Property(e => e.ItemTypeid).HasColumnName("ITEM_TYPEID");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("REG_DATE");
        });

        modelBuilder.Entity<PangyaHioEventLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__pangya_h__3213E83F99883BEE");

            entity.ToTable("pangya_hio_event_log", "pangya");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.HioCount).HasColumnName("hio_count");
            entity.Property(e => e.ItemTypeid).HasColumnName("item_typeid");
            entity.Property(e => e.ReceivedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("received_at");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.Uid).HasColumnName("uid");
        });

        modelBuilder.Entity<PangyaHoleEvent>(entity =>
        {
            entity.HasKey(e => e.Index);

            entity.ToTable("pangya_hole_event", "pangya");

            entity.Property(e => e.Index).HasColumnName("INDEX");
            entity.Property(e => e.FinishDate).HasColumnName("FINISH_DATE");
            entity.Property(e => e.ProcessHoles).HasColumnName("PROCESS_HOLES");
            entity.Property(e => e.StartHoles).HasColumnName("START_HOLES");
            entity.Property(e => e.Status).HasColumnName("STATUS");
            entity.Property(e => e.Uid).HasColumnName("UID");
        });

        modelBuilder.Entity<PangyaHoleEventConfig>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_hole_event_config", "pangya");

            entity.Property(e => e.EndEvent).HasColumnName("END_EVENT");
            entity.Property(e => e.EventId).HasColumnName("EVENT_ID");
            entity.Property(e => e.StartEvent).HasColumnName("START_EVENT");
        });

        modelBuilder.Entity<PangyaHoleEventItem>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_hole_event_items", "pangya");

            entity.Property(e => e.EventDescription)
                .HasMaxLength(255)
                .HasColumnName("EVENT_DESCRIPTION");
            entity.Property(e => e.EventId).HasColumnName("EVENT_ID");
            entity.Property(e => e.HoleCount).HasColumnName("HOLE_COUNT");
            entity.Property(e => e.ItemName)
                .HasMaxLength(50)
                .HasColumnName("ITEM_NAME");
            entity.Property(e => e.ItemQntd).HasColumnName("ITEM_QNTD");
            entity.Property(e => e.ItemQntdTime).HasColumnName("ITEM_QNTD_TIME");
            entity.Property(e => e.ItemTypeid).HasColumnName("ITEM_TYPEID");
        });

        modelBuilder.Entity<PangyaIpTable>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK_pangya_ip_table_index");

            entity.ToTable("pangya_ip_table", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.Date)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("date");
            entity.Property(e => e.Ip)
                .IsRequired()
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasColumnName("ip");
            entity.Property(e => e.Mask)
                .IsRequired()
                .HasMaxLength(18)
                .IsUnicode(false)
                .HasDefaultValue("255.255.255.255")
                .HasColumnName("mask");
        });

        modelBuilder.Entity<PangyaItemBuff>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_item_buff", "pangya");

            entity.HasIndex(e => e.Index, "IX_pangya_item_buff").IsUnique();

            entity.Property(e => e.EndDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("end_date");
            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.Percent).HasColumnName("percent");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("reg_date");
            entity.Property(e => e.Tipo)
                .HasDefaultValue((short)2)
                .HasColumnName("tipo");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
            entity.Property(e => e.Uid).HasColumnName("uid");
            entity.Property(e => e.UseYn)
                .HasDefaultValue((byte)1)
                .HasColumnName("use_yn");
        });

        modelBuilder.Entity<PangyaItemBuyShopLog>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_item_buy_shop_log", "pangya");

            entity.HasIndex(e => e.Index, "IX_pangya_item_buy_shop_log").IsUnique();

            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.ItemCookie).HasColumnName("item_cookie");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.ItemPang).HasColumnName("item_pang");
            entity.Property(e => e.ItemQntd).HasColumnName("item_qntd");
            entity.Property(e => e.ItemTime).HasColumnName("item_time");
            entity.Property(e => e.ItemType).HasColumnName("item_type");
            entity.Property(e => e.ItemTypeid).HasColumnName("item_typeid");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("reg_date");
            entity.Property(e => e.Uid).HasColumnName("uid");
        });

        modelBuilder.Entity<PangyaItemMail>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_item_mail", "pangya");

            entity.Property(e => e.FlagGift).HasColumnName("Flag_Gift");
            entity.Property(e => e.GetDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("GET_DATE");
            entity.Property(e => e.GmId)
                .HasDefaultValue(-1)
                .HasColumnName("GM_ID");
            entity.Property(e => e.ItemId)
                .HasDefaultValue(-1)
                .HasColumnName("item_id");
            entity.Property(e => e.ItemTypeid).HasColumnName("item_typeid");
            entity.Property(e => e.MsgId).HasColumnName("Msg_ID");
            entity.Property(e => e.QuantidadeDia).HasColumnName("Quantidade_Dia");
            entity.Property(e => e.QuantidadeItem).HasColumnName("Quantidade_item");
            entity.Property(e => e.UccImgMark)
                .IsRequired()
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasDefaultValue("0")
                .HasColumnName("UCC_IMG_MARK");
            entity.Property(e => e.Valid)
                .HasDefaultValue((short)1)
                .HasColumnName("valid");
        });

        modelBuilder.Entity<PangyaItemTypelist>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_item_typelist", "pangya");

            entity.Property(e => e.CharId)
                .HasDefaultValue(0)
                .HasColumnName("CHAR_ID");
            entity.Property(e => e.CharSerialno)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("0")
                .HasColumnName("CHAR_SERIALNO");
            entity.Property(e => e.Com0)
                .HasDefaultValueSql("('0')")
                .HasColumnName("COM0");
            entity.Property(e => e.Com1)
                .HasDefaultValueSql("('0')")
                .HasColumnName("COM1");
            entity.Property(e => e.Com2)
                .HasDefaultValueSql("('0')")
                .HasColumnName("COM2");
            entity.Property(e => e.Com3)
                .HasDefaultValueSql("('0')")
                .HasColumnName("COM3");
            entity.Property(e => e.Com4)
                .HasDefaultValueSql("('0')")
                .HasColumnName("COM4");
            entity.Property(e => e.Desc)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .HasDefaultValue("NO HAVE DESC")
                .HasColumnName("DESC");
            entity.Property(e => e.Icon)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("icon_x.png")
                .HasColumnName("ICON");
            entity.Property(e => e.IffType)
                .HasDefaultValueSql("('0')")
                .HasColumnName("IFF_TYPE");
            entity.Property(e => e.IsSalable)
                .HasDefaultValueSql("('0')")
                .HasColumnName("IS_SALABLE");
            entity.Property(e => e.Iscash).HasColumnName("ISCASH");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasDefaultValue("NAME ITEM")
                .HasColumnName("NAME");
            entity.Property(e => e.NameItem)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("NAME_ITEM");
            entity.Property(e => e.Price)
                .HasDefaultValueSql("('0')")
                .HasColumnName("PRICE");
            entity.Property(e => e.Tname)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("NO HAVE TNAME")
                .HasColumnName("TNAME");
            entity.Property(e => e.Type)
                .HasDefaultValueSql("('0')")
                .HasColumnName("TYPE");
            entity.Property(e => e.Typeid).HasColumnName("TYPEID");
        });

        modelBuilder.Entity<PangyaItemWarehouse>(entity =>
        {
            entity.HasKey(e => e.ItemId).HasName("PK_pangya_item_warehouse_item_id");

            entity.ToTable("pangya_item_warehouse", "pangya");

            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Applytime)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ClubSetWorkShopC0).HasColumnName("ClubSet_WorkShop_C0");
            entity.Property(e => e.ClubSetWorkShopC1).HasColumnName("ClubSet_WorkShop_C1");
            entity.Property(e => e.ClubSetWorkShopC2).HasColumnName("ClubSet_WorkShop_C2");
            entity.Property(e => e.ClubSetWorkShopC3).HasColumnName("ClubSet_WorkShop_C3");
            entity.Property(e => e.ClubSetWorkShopC4).HasColumnName("ClubSet_WorkShop_C4");
            entity.Property(e => e.ClubSetWorkShopFlag).HasColumnName("ClubSet_WorkShop_Flag");
            entity.Property(e => e.EndDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Flag).HasColumnName("flag");
            entity.Property(e => e.GiftFlag).HasColumnName("Gift_flag");
            entity.Property(e => e.ItemType).HasDefaultValue((short)2);
            entity.Property(e => e.MasteryGasto).HasColumnName("Mastery_Gasto");
            entity.Property(e => e.MasteryPts).HasColumnName("Mastery_Pts");
            entity.Property(e => e.RecoveryPts).HasColumnName("Recovery_Pts");
            entity.Property(e => e.Regdate)
                .HasPrecision(0)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("regdate");
            entity.Property(e => e.TotalMasteryPts).HasColumnName("Total_Mastery_Pts");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.Valid)
                .HasDefaultValue((short)1)
                .HasColumnName("valid");
        });

        modelBuilder.Entity<PangyaLastPlayersUser>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_last_players_user", "pangya");

            entity.Property(e => e.Id0)
                .HasMaxLength(22)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("ID_0");
            entity.Property(e => e.Id1)
                .HasMaxLength(22)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("ID_1");
            entity.Property(e => e.Id2)
                .HasMaxLength(22)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("ID_2");
            entity.Property(e => e.Id3)
                .HasMaxLength(22)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("ID_3");
            entity.Property(e => e.Id4)
                .HasMaxLength(22)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("ID_4");
            entity.Property(e => e.Nick0)
                .HasMaxLength(22)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("NICK_0");
            entity.Property(e => e.Nick1)
                .HasMaxLength(22)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("NICK_1");
            entity.Property(e => e.Nick2)
                .HasMaxLength(22)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("NICK_2");
            entity.Property(e => e.Nick3)
                .HasMaxLength(22)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("NICK_3");
            entity.Property(e => e.Nick4)
                .HasMaxLength(22)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("NICK_4");
            entity.Property(e => e.Sex0)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("SEX_0");
            entity.Property(e => e.Sex1)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("SEX_1");
            entity.Property(e => e.Sex2)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("SEX_2");
            entity.Property(e => e.Sex3)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("SEX_3");
            entity.Property(e => e.Sex4)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("SEX_4");
            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.Uid0)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("UID_0");
            entity.Property(e => e.Uid1)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("UID_1");
            entity.Property(e => e.Uid2)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("UID_2");
            entity.Property(e => e.Uid3)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("UID_3");
            entity.Property(e => e.Uid4)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("UID_4");
        });

        modelBuilder.Entity<PangyaLastUpClubset>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_last_up_clubset", "pangya");

            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.ItemUsado).HasColumnName("item_usado");
            entity.Property(e => e.Uid).HasColumnName("UID");
        });

        modelBuilder.Entity<PangyaLoginReward>(entity =>
        {
            entity.HasKey(e => e.Index);

            entity.ToTable("pangya_login_reward", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.DaysToGift)
                .HasDefaultValue(1)
                .HasColumnName("days_to_gift");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.IsEnd).HasColumnName("is_end");
            entity.Property(e => e.ItemQntd).HasColumnName("item_qntd");
            entity.Property(e => e.ItemQntdTime).HasColumnName("item_qntd_time");
            entity.Property(e => e.ItemTypeid).HasColumnName("item_typeid");
            entity.Property(e => e.NTimesGift).HasColumnName("n_times_gift");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("reg_date");
            entity.Property(e => e.Type).HasColumnName("type");
        });

        modelBuilder.Entity<PangyaLoginRewardPlayer>(entity =>
        {
            entity.HasKey(e => e.Index);

            entity.ToTable("pangya_login_reward_player", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.CountDays)
                .HasDefaultValue(1)
                .HasColumnName("count_days");
            entity.Property(e => e.CountSeq).HasColumnName("count_seq");
            entity.Property(e => e.IsClear).HasColumnName("is_clear");
            entity.Property(e => e.LoginRewardId).HasColumnName("login_reward_id");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("reg_date");
            entity.Property(e => e.Uid).HasColumnName("uid");
            entity.Property(e => e.UpdateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("update_date");
        });

        modelBuilder.Entity<PangyaLuciaAttendance>(entity =>
        {
            entity.HasKey(e => e.Uid);

            entity.ToTable("pangya_lucia_attendance", "pangya");

            entity.Property(e => e.Uid)
                .ValueGeneratedNever()
                .HasColumnName("UID");
            entity.Property(e => e.BlockEndDate).HasColumnName("block_end_date");
            entity.Property(e => e.BlockType).HasColumnName("block_type");
            entity.Property(e => e.CountDay).HasColumnName("count_day");
            entity.Property(e => e.LastDayAttendance).HasColumnName("last_day_attendance");
            entity.Property(e => e.LastDayGetItem).HasColumnName("last_day_get_item");
            entity.Property(e => e.TryHackingCount).HasColumnName("try_hacking_count");
        });

        modelBuilder.Entity<PangyaLuciaAttendanceRewardLog>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK_pangya.pangya_lucia_attendance_log");

            entity.ToTable("pangya_lucia_attendance_reward_log", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.Error)
                .HasMaxLength(50)
                .HasColumnName("ERROR");
            entity.Property(e => e.MsgId).HasColumnName("MSG_ID");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("REG_DATE");
            entity.Property(e => e.Uid).HasColumnName("UID");
        });

        modelBuilder.Entity<PangyaMacTable>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_mac_table", "pangya");

            entity.HasIndex(e => e.Index, "IX_pangya_mac_table").IsUnique();

            entity.HasIndex(e => e.Index, "IX_pangya_mac_table_1").IsUnique();

            entity.Property(e => e.Date)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("date");
            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.Mac)
                .IsRequired()
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("mac");
        });

        modelBuilder.Entity<PangyaManiadonationLog>(entity =>
        {
            entity.HasKey(e => new { e.AdmUid, e.Uid }).HasName("PK__pangya_m__02CC300AD4C936EC");

            entity.ToTable("pangya_maniadonation_log", "pangya");

            entity.Property(e => e.AdmUid).HasColumnName("ADM_UID");
            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.Cash)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("cash");
            entity.Property(e => e.CookiePoint).HasColumnName("cookie_point");
            entity.Property(e => e.ItemQntd1).HasColumnName("ITEM_QNTD_1");
            entity.Property(e => e.ItemQntd2).HasColumnName("ITEM_QNTD_2");
            entity.Property(e => e.ItemQntd3).HasColumnName("ITEM_QNTD_3");
            entity.Property(e => e.ItemQntd4).HasColumnName("ITEM_QNTD_4");
            entity.Property(e => e.ItemQntd5).HasColumnName("ITEM_QNTD_5");
            entity.Property(e => e.ItemTypeid1).HasColumnName("ITEM_TYPEID_1");
            entity.Property(e => e.ItemTypeid2).HasColumnName("ITEM_TYPEID_2");
            entity.Property(e => e.ItemTypeid3).HasColumnName("ITEM_TYPEID_3");
            entity.Property(e => e.ItemTypeid4).HasColumnName("ITEM_TYPEID_4");
            entity.Property(e => e.ItemTypeid5).HasColumnName("ITEM_TYPEID_5");
            entity.Property(e => e.Pangs).HasColumnName("pangs");
        });

        modelBuilder.Entity<PangyaMascotInfo>(entity =>
        {
            entity.HasKey(e => e.ItemId).HasName("PK_pangya_mascot_info_item_id");

            entity.ToTable("pangya_mascot_info", "pangya");

            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.EndDate).HasPrecision(0);
            entity.Property(e => e.MExp).HasColumnName("mExp");
            entity.Property(e => e.MLevel).HasColumnName("mLevel");
            entity.Property(e => e.Message)
                .IsRequired()
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("Pangya GZ");
            entity.Property(e => e.RegDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.Valid).HasDefaultValue((short)1);
        });

        modelBuilder.Entity<PangyaMsgUser>(entity =>
        {
            entity.HasKey(e => e.MsgIdx).HasName("PK_pangya_msg_user_msg_idx");

            entity.ToTable("pangya_msg_user", "pangya");

            entity.Property(e => e.MsgIdx).HasColumnName("msg_idx");
            entity.Property(e => e.Msg)
                .IsRequired()
                .HasMaxLength(500)
                .HasDefaultValue("hello")
                .HasColumnName("msg");
            entity.Property(e => e.RegDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("reg_date");
            entity.Property(e => e.Uid).HasColumnName("uid");
            entity.Property(e => e.UidFrom).HasColumnName("uid_from");
            entity.Property(e => e.Valid)
                .HasDefaultValue((short)1)
                .HasColumnName("valid");
        });

        modelBuilder.Entity<PangyaMyroom>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("PK_pangya_myroom_uid");

            entity.ToTable("pangya_myroom", "pangya");

            entity.Property(e => e.Uid)
                .ValueGeneratedNever()
                .HasColumnName("uid");
            entity.Property(e => e.PublicLock).HasColumnName("public_lock");
            entity.Property(e => e.Senha)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("senha");
            entity.Property(e => e.State)
                .HasDefaultValue((short)1)
                .HasColumnName("state");
        });

        modelBuilder.Entity<PangyaNewBox>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_new_box", "pangya");

            entity.HasIndex(e => e.Id, "IX_pangya_new_box").IsUnique();

            entity.Property(e => e.Active)
                .HasDefaultValue((byte)1)
                .HasColumnName("active");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Message)
                .IsRequired()
                .HasMaxLength(80)
                .HasDefaultValue("OUUUU VOCÊ GANHOU UM ITEM<GZ>")
                .HasColumnName("message");
            entity.Property(e => e.Nome)
                .HasMaxLength(100)
                .HasColumnName("nome");
            entity.Property(e => e.Numero)
                .HasDefaultValue(1)
                .HasColumnName("numero");
            entity.Property(e => e.OpenedTypeid).HasColumnName("opened_typeid");
            entity.Property(e => e.Tipo)
                .HasComment("0 SEND ITEM TO MAIL, 1 SEND ITEM TO MY ROOM")
                .HasColumnName("tipo");
            entity.Property(e => e.TipoOpen)
                .HasComment("0 SEND ITEM TO MAIL, 1 SEND ITEM TO MY ROOM")
                .HasColumnName("tipo_open");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
        });

        modelBuilder.Entity<PangyaNewBoxItem>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_new_box_item", "pangya");

            entity.Property(e => e.Active)
                .HasDefaultValue((byte)1)
                .HasColumnName("active");
            entity.Property(e => e.BoxId).HasColumnName("box_id");
            entity.Property(e => e.Duplicar).HasColumnName("duplicar");
            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.Nome)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("nome");
            entity.Property(e => e.Numero)
                .HasDefaultValue(-1)
                .HasColumnName("numero");
            entity.Property(e => e.Probabilidade)
                .HasDefaultValue(100)
                .HasColumnName("probabilidade");
            entity.Property(e => e.Qntd)
                .HasDefaultValue(1)
                .HasColumnName("qntd");
            entity.Property(e => e.Raridade)
                .HasComment("0 NORMAL, 1 RARE, 2 SUPER RARE")
                .HasColumnName("raridade");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
        });

        modelBuilder.Entity<PangyaNewBoxRareWinLog>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_new_box_rare_win_log", "pangya");

            entity.HasIndex(e => e.Index, "IX_pangya_new_box_rare_win_log").IsUnique();

            entity.Property(e => e.BoxTypeid).HasColumnName("box_typeid");
            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.ItemTypeid).HasColumnName("item_typeid");
            entity.Property(e => e.Qntd).HasColumnName("qntd");
            entity.Property(e => e.Raridade).HasColumnName("raridade");
            entity.Property(e => e.Uid).HasColumnName("uid");
            entity.Property(e => e.WinDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("win_date");
        });

        modelBuilder.Entity<PangyaNewCard>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_new_cards", "pangya");

            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Pack).HasColumnName("pack");
            entity.Property(e => e.Probabilidade).HasColumnName("probabilidade");
            entity.Property(e => e.Tipo).HasColumnName("tipo");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
        });

        modelBuilder.Entity<PangyaNewCardPack>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_new_card_pack", "pangya");

            entity.HasIndex(e => e.Index, "UQ__pangya_n__1D0A3349DE4D54D1").IsUnique();

            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Quantidade)
                .HasDefaultValue((short)1)
                .HasColumnName("quantidade");
            entity.Property(e => e.RateN)
                .HasDefaultValue((short)100)
                .HasColumnName("rate_N");
            entity.Property(e => e.RateR)
                .HasDefaultValue((short)100)
                .HasColumnName("rate_R");
            entity.Property(e => e.RateSc)
                .HasDefaultValue((short)100)
                .HasColumnName("rate_SC");
            entity.Property(e => e.RateSr)
                .HasDefaultValue((short)100)
                .HasColumnName("rate_SR");
            entity.Property(e => e.Tipo).HasColumnName("tipo");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
        });

        modelBuilder.Entity<PangyaNewCourseDrop>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_new_course_drop", "pangya");

            entity.HasIndex(e => e.Index, "IX_pangya_new_course_drop_1").IsUnique();

            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.RateGrandPrixTicket)
                .HasDefaultValue(100)
                .HasColumnName("rate_grand_prix_ticket");
            entity.Property(e => e.RateManaArtefact)
                .HasDefaultValue(100)
                .HasColumnName("rate_mana_artefact");
            entity.Property(e => e.RateSscTicket)
                .HasDefaultValue(100)
                .HasColumnName("rate_SSC_ticket");
        });

        modelBuilder.Entity<PangyaNewCourseDropItem>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_new_course_drop_item", "pangya");

            entity.HasIndex(e => e.Index, "IX_pangya_new_course_drop").IsUnique();

            entity.Property(e => e.Active)
                .HasDefaultValue((byte)1)
                .HasColumnName("active");
            entity.Property(e => e.Course).HasColumnName("course");
            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.Probabilidade18h).HasColumnName("probabilidade_18H");
            entity.Property(e => e.Probabilidade3h).HasColumnName("probabilidade_3H");
            entity.Property(e => e.Probabilidade6h).HasColumnName("probabilidade_6H");
            entity.Property(e => e.Probabilidade9h).HasColumnName("probabilidade_9H");
            entity.Property(e => e.Quantidade)
                .HasDefaultValue(1)
                .HasColumnName("quantidade");
            entity.Property(e => e.Tipo).HasColumnName("tipo");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
        });

        modelBuilder.Entity<PangyaNewMemorialCoin>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_new_memorial_coin", "pangya");

            entity.Property(e => e.Nome)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("nome");
            entity.Property(e => e.Probabilidade).HasColumnName("probabilidade");
            entity.Property(e => e.Tipo).HasColumnName("tipo");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
        });

        modelBuilder.Entity<PangyaNewMemorialLevel>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_new_memorial_level", "pangya");

            entity.HasIndex(e => e.Level, "IX_pangya_new_memorial_level").IsUnique();

            entity.Property(e => e.GachaEnd).HasColumnName("gacha_end");
            entity.Property(e => e.GachaStart).HasColumnName("gacha_start");
            entity.Property(e => e.Level).HasColumnName("level");
        });

        modelBuilder.Entity<PangyaNewMemorialLuckySet>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_new_memorial_lucky_set", "pangya");

            entity.HasIndex(e => e.Index, "IX_pangya_new_memorial_lucky_set").IsUnique();

            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.Nome)
                .HasMaxLength(100)
                .HasColumnName("nome");
            entity.Property(e => e.Qntd).HasColumnName("qntd");
            entity.Property(e => e.SetId).HasColumnName("set_id");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
        });

        modelBuilder.Entity<PangyaNewMemorialNormalItem>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_new_memorial_normal_item", "pangya");

            entity.HasIndex(e => e.Index, "IX_pangya_new_memorial_normal_item").IsUnique();

            entity.Property(e => e.Active)
                .HasDefaultValue((byte)1)
                .HasColumnName("active");
            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.Nome)
                .HasMaxLength(100)
                .HasColumnName("nome");
            entity.Property(e => e.Qntd).HasColumnName("qntd");
            entity.Property(e => e.Tipo).HasColumnName("tipo");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
        });

        modelBuilder.Entity<PangyaNewMemorialRareItem>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_new_memorial_rare_item", "pangya");

            entity.Property(e => e.CoinTypeid).HasColumnName("coin_typeid");
            entity.Property(e => e.ItemActive).HasColumnName("item_active");
            entity.Property(e => e.ItemGachaNumber).HasColumnName("item_gacha_number");
            entity.Property(e => e.ItemNome)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("item_nome");
            entity.Property(e => e.ItemProbabilidade).HasColumnName("item_probabilidade");
            entity.Property(e => e.ItemTipo).HasColumnName("item_tipo");
            entity.Property(e => e.ItemTypeid).HasColumnName("item_typeid");
        });

        modelBuilder.Entity<PangyaNewMemorialRareWinLog>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_new_memorial_rare_win_log", "pangya");

            entity.HasIndex(e => e.Index, "IX_pangya_new_memorial_rare_win_log").IsUnique();

            entity.Property(e => e.CoinTypeid).HasColumnName("coin_typeid");
            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.ItemProbabilidade).HasColumnName("item_probabilidade");
            entity.Property(e => e.ItemQntd).HasColumnName("item_qntd");
            entity.Property(e => e.ItemRaridade).HasColumnName("item_raridade");
            entity.Property(e => e.ItemTypeid).HasColumnName("item_typeid");
            entity.Property(e => e.MemorialNr).HasColumnName("memorial_nr");
            entity.Property(e => e.Uid).HasColumnName("uid");
            entity.Property(e => e.WinDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("win_date");
        });

        modelBuilder.Entity<PangyaNewPremiumUser>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_new_premium_user", "pangya");

            entity.HasIndex(e => e.Index, "IX_pangya_new_premium_user").IsUnique();

            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.LastUpdate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("last_update");
            entity.Property(e => e.LimitCnt)
                .HasDefaultValue((short)1)
                .HasColumnName("limit_cnt");
            entity.Property(e => e.Uid).HasColumnName("uid");
        });

        modelBuilder.Entity<PangyaNewPremiumUserItem>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_new_premium_user_item", "pangya");

            entity.HasIndex(e => e.Index, "IX_pangya_new_premium_user_item").IsUnique();

            entity.Property(e => e.Active)
                .HasDefaultValue((byte)1)
                .HasColumnName("active");
            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.Nome).HasMaxLength(100);
            entity.Property(e => e.Qtd).HasColumnName("qtd");
            entity.Property(e => e.Tipo).HasColumnName("tipo");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
        });

        modelBuilder.Entity<PangyaNewPremiumUserLog>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_new_premium_user_log", "pangya");

            entity.HasIndex(e => e.Index, "IX_pangya_new_premium_user_log").IsUnique();

            entity.Property(e => e.Date)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("date");
            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.Uid).HasColumnName("uid");
        });

        modelBuilder.Entity<PangyaNoticeList>(entity =>
        {
            entity.HasKey(e => e.NoticeId).HasName("PK_pangya_notice_list_notice_id");

            entity.ToTable("pangya_notice_list", "pangya");

            entity.Property(e => e.NoticeId).HasColumnName("notice_id");
            entity.Property(e => e.Message)
                .HasMaxLength(1024)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("message");
            entity.Property(e => e.RefreshTime).HasColumnName("refreshTime");
            entity.Property(e => e.ReplayCount)
                .HasDefaultValue(1)
                .HasColumnName("replayCount");
        });

        modelBuilder.Entity<PangyaPapelShopConfig>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_papel_shop_config", "pangya");

            entity.Property(e => e.LimittedYn)
                .HasDefaultValue((byte)1)
                .HasColumnName("Limitted_YN");
            entity.Property(e => e.Numero).HasDefaultValue(1);
            entity.Property(e => e.PriceBig)
                .HasDefaultValue(10000L)
                .HasColumnName("Price_Big");
            entity.Property(e => e.PriceNormal)
                .HasDefaultValue(900L)
                .HasColumnName("Price_Normal");
            entity.Property(e => e.UpdateDate).HasColumnName("Update_Date");
        });

        modelBuilder.Entity<PangyaPapelShopCoupon>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_papel_shop_coupon", "pangya");

            entity.HasIndex(e => e.Index, "IX_pangya_papel_shop_coupon").IsUnique();

            entity.Property(e => e.Active)
                .HasDefaultValue((byte)1)
                .HasColumnName("active");
            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
        });

        modelBuilder.Entity<PangyaPapelShopInfo>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_papel_shop_info", "pangya");

            entity.HasIndex(e => e.Index, "IX_pangya_papel_shop_info").IsUnique();

            entity.Property(e => e.CurrentCnt).HasColumnName("current_cnt");
            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.LastUpdate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("last_update");
            entity.Property(e => e.LimitCnt)
                .HasDefaultValue((short)50)
                .HasColumnName("limit_cnt");
            entity.Property(e => e.RemainCnt).HasColumnName("remain_cnt");
            entity.Property(e => e.Uid).HasColumnName("uid");
        });

        modelBuilder.Entity<PangyaPapelShopItem>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_papel_shop_item", "pangya");

            entity.HasIndex(e => e.Index, "IX_pangya_papel_shop_item").IsUnique();

            entity.Property(e => e.Active)
                .HasDefaultValue((byte)1)
                .HasColumnName("active");
            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.Nome).HasMaxLength(100);
            entity.Property(e => e.Numero)
                .HasDefaultValue(-1)
                .HasColumnName("numero");
            entity.Property(e => e.Probabilidade).HasColumnName("probabilidade");
            entity.Property(e => e.Tipo).HasColumnName("tipo");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
        });

        modelBuilder.Entity<PangyaPapelShopRareWinLog>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_papel_shop_rare_win_log", "pangya");

            entity.HasIndex(e => e.Index, "IX_pangya_papel_shop_rare_win").IsUnique();

            entity.Property(e => e.BallColor).HasColumnName("ball_color");
            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.Probabilidade).HasColumnName("probabilidade");
            entity.Property(e => e.Qntd).HasColumnName("qntd");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("reg_date");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
            entity.Property(e => e.Uid).HasColumnName("uid");
        });

        modelBuilder.Entity<PangyaPartsList>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK_pangya_parts_list_index");

            entity.ToTable("pangya_parts_list", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.EquipFlag).HasColumnName("equip_flag");
            entity.Property(e => e.Tipo).HasColumnName("tipo");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
        });

        modelBuilder.Entity<PangyaPersonalShopConfig>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK__pangya_p__9A5B62289F36E46A");

            entity.ToTable("pangya_personal_shop_config", "pangya");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("reg_date");
        });

        modelBuilder.Entity<PangyaPersonalShopLog>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_personal_shop_log", "pangya");

            entity.HasIndex(e => e.Index, "IX_pangya_personal_shop_log").IsUnique();

            entity.HasIndex(e => e.Index, "IX_pangya_personal_shop_log_1").IsUnique();

            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.ItemIdBuy).HasColumnName("item_id_buy");
            entity.Property(e => e.ItemIdSell).HasColumnName("item_id_sell");
            entity.Property(e => e.ItemPang).HasColumnName("item_pang");
            entity.Property(e => e.ItemQntd).HasColumnName("item_qntd");
            entity.Property(e => e.ItemTypeid).HasColumnName("item_typeid");
            entity.Property(e => e.PlayerBuyUid).HasColumnName("player_buy_uid");
            entity.Property(e => e.PlayerSellUid).HasColumnName("player_sell_uid");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("reg_date");
            entity.Property(e => e.TotalPang).HasColumnName("total_pang");
        });

        modelBuilder.Entity<PangyaPlayerBirthDayLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__pangya_p__3214EC27880F6C54");

            entity.ToTable("pangya_player_birth_day_log", "pangya");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Login)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("LOGIN");
            entity.Property(e => e.UserId).HasColumnName("UserID");
        });

        modelBuilder.Entity<PangyaPlayerIp>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK_pangya_player_ip_index");

            entity.ToTable("pangya_player_ip", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.BlockBeta)
                .HasDefaultValue((byte)1)
                .HasColumnName("block_beta");
            entity.Property(e => e.ChangeCount).HasColumnName("change_count");
            entity.Property(e => e.ChangeDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("change_date");
            entity.Property(e => e.FlagDay)
                .HasDefaultValue((short)1)
                .HasColumnName("flag_day");
            entity.Property(e => e.Ip)
                .IsRequired()
                .HasMaxLength(18)
                .IsUnicode(false)
                .HasDefaultValue("000.000.000.000")
                .HasColumnName("ip");
            entity.Property(e => e.Uid).HasColumnName("uid");
        });

        modelBuilder.Entity<PangyaPlayerLocation>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_player_location", "pangya");

            entity.HasIndex(e => e.Uid, "IX_pangya_player_location").IsUnique();

            entity.Property(e => e.Channel)
                .HasDefaultValue((short)-1)
                .HasColumnName("channel");
            entity.Property(e => e.Lobby)
                .HasDefaultValue((short)-1)
                .HasColumnName("lobby");
            entity.Property(e => e.Place).HasColumnName("place");
            entity.Property(e => e.Room)
                .HasDefaultValue((short)-1)
                .HasColumnName("room");
            entity.Property(e => e.Uid).HasColumnName("UID");
        });

        modelBuilder.Entity<PangyaPointEvent>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("PK_pangya_point_event_uid");

            entity.ToTable("pangya_point_event", "pangya");

            entity.Property(e => e.Uid)
                .ValueGeneratedNever()
                .HasColumnName("uid");
            entity.Property(e => e.LastDay)
                .HasColumnType("datetime")
                .HasColumnName("last_day");
            entity.Property(e => e.LimitBuy).HasColumnName("limit_buy");
            entity.Property(e => e.Points).HasColumnName("points");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("reg_date");
        });

        modelBuilder.Entity<PangyaPointEventItem>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_point_event_items", "pangya");

            entity.Property(e => e.Actived)
                .HasDefaultValueSql("('0')")
                .HasColumnName("ACTIVED");
            entity.Property(e => e.CharType)
                .HasDefaultValueSql("('0')")
                .HasColumnName("CHAR_TYPE");
            entity.Property(e => e.Icon)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("icon_x")
                .HasColumnName("ICON");
            entity.Property(e => e.IffType)
                .HasDefaultValueSql("('0')")
                .HasColumnName("IFF_TYPE");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(120)
                .IsUnicode(false)
                .HasDefaultValue("NAME ITEM")
                .HasColumnName("NAME");
            entity.Property(e => e.Price)
                .HasDefaultValueSql("('0')")
                .HasColumnName("PRICE");
            entity.Property(e => e.Typeid).HasColumnName("TYPEID");
        });

        modelBuilder.Entity<PangyaPremioindicacaoLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__pangya_p__2D26E7AE20251924");

            entity.ToTable("pangya_premioindicacao_log", "pangya");

            entity.Property(e => e.LogId).HasColumnName("Log_ID");
            entity.Property(e => e.AdmUid).HasColumnName("ADM_UID");
            entity.Property(e => e.Cash).HasColumnName("cash");
            entity.Property(e => e.CookiePoint).HasColumnName("cookie_point");
            entity.Property(e => e.ItemQntd1).HasColumnName("ITEM_QNTD_1");
            entity.Property(e => e.ItemQntd2).HasColumnName("ITEM_QNTD_2");
            entity.Property(e => e.ItemQntd3).HasColumnName("ITEM_QNTD_3");
            entity.Property(e => e.ItemQntd4).HasColumnName("ITEM_QNTD_4");
            entity.Property(e => e.ItemQntd5).HasColumnName("ITEM_QNTD_5");
            entity.Property(e => e.ItemTypeid1).HasColumnName("ITEM_TYPEID_1");
            entity.Property(e => e.ItemTypeid2).HasColumnName("ITEM_TYPEID_2");
            entity.Property(e => e.ItemTypeid3).HasColumnName("ITEM_TYPEID_3");
            entity.Property(e => e.ItemTypeid4).HasColumnName("ITEM_TYPEID_4");
            entity.Property(e => e.ItemTypeid5).HasColumnName("ITEM_TYPEID_5");
            entity.Property(e => e.LogDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("Log_Date");
            entity.Property(e => e.Pangs).HasColumnName("pangs");
            entity.Property(e => e.Uid).HasColumnName("UID");
        });

        modelBuilder.Entity<PangyaQuest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_pangya_quest_copy1");

            entity.ToTable("pangya_quest", "pangya");

            entity.HasIndex(e => e.Id, "UQ__pangya_q__3213E83E970124D3").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AchievementId).HasColumnName("achievement_id");
            entity.Property(e => e.CounterItemId).HasColumnName("counter_item_id");
            entity.Property(e => e.Date).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
            entity.Property(e => e.Uid).HasColumnName("uid");
        });

        modelBuilder.Entity<PangyaQuestClear>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK_pangya_quest_clear_index");

            entity.ToTable("pangya_quest_clear", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.Option)
                .HasDefaultValue((short)1)
                .HasColumnName("option");
            entity.Property(e => e.QuestId).HasColumnName("quest_id");
            entity.Property(e => e.Uid).HasColumnName("uid");
        });

        modelBuilder.Entity<PangyaRankAnte>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK_pangya_rank_antes_index");

            entity.ToTable("pangya_rank_antes", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.Position).HasColumnName("position");
            entity.Property(e => e.RegDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("reg_date");
            entity.Property(e => e.TipoRank).HasColumnName("tipo_rank");
            entity.Property(e => e.TipoRankSeq).HasColumnName("tipo_rank_seq");
            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.Valor).HasColumnName("valor");
        });

        modelBuilder.Entity<PangyaRankAtual>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK_pangya_rank_atual_index");

            entity.ToTable("pangya_rank_atual", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.Position).HasColumnName("position");
            entity.Property(e => e.RegDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("reg_date");
            entity.Property(e => e.TipoRank).HasColumnName("tipo_rank");
            entity.Property(e => e.TipoRankSeq).HasColumnName("tipo_rank_seq");
            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.Valor).HasColumnName("valor");
        });

        modelBuilder.Entity<PangyaRankAtualCharacter>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("PK_pangya_rank_atual_character_uid");

            entity.ToTable("pangya_rank_atual_character", "pangya");

            entity.Property(e => e.Uid)
                .ValueGeneratedNever()
                .HasColumnName("uid");
            entity.Property(e => e.Auxparts1).HasColumnName("AUXPARTS_1");
            entity.Property(e => e.Auxparts2).HasColumnName("AUXPARTS_2");
            entity.Property(e => e.Auxparts3).HasColumnName("AUXPARTS_3");
            entity.Property(e => e.Auxparts4).HasColumnName("AUXPARTS_4");
            entity.Property(e => e.Auxparts5).HasColumnName("AUXPARTS_5");
            entity.Property(e => e.CardCaddie1).HasColumnName("CARD_CADDIE_1");
            entity.Property(e => e.CardCaddie2).HasColumnName("CARD_CADDIE_2");
            entity.Property(e => e.CardCaddie3).HasColumnName("CARD_CADDIE_3");
            entity.Property(e => e.CardCaddie4).HasColumnName("CARD_CADDIE_4");
            entity.Property(e => e.CardCharacter1).HasColumnName("CARD_CHARACTER_1");
            entity.Property(e => e.CardCharacter2).HasColumnName("CARD_CHARACTER_2");
            entity.Property(e => e.CardCharacter3).HasColumnName("CARD_CHARACTER_3");
            entity.Property(e => e.CardCharacter4).HasColumnName("CARD_CHARACTER_4");
            entity.Property(e => e.CardNpc1).HasColumnName("CARD_NPC_1");
            entity.Property(e => e.CardNpc2).HasColumnName("CARD_NPC_2");
            entity.Property(e => e.CardNpc3).HasColumnName("CARD_NPC_3");
            entity.Property(e => e.CardNpc4).HasColumnName("CARD_NPC_4");
            entity.Property(e => e.CutIn1).HasColumnName("CutIn_1");
            entity.Property(e => e.CutIn2).HasColumnName("CutIn_2");
            entity.Property(e => e.CutIn3).HasColumnName("CutIn_3");
            entity.Property(e => e.CutIn4).HasColumnName("CutIn_4");
            entity.Property(e => e.DefaultHair).HasColumnName("default_hair");
            entity.Property(e => e.DefaultShirts).HasColumnName("default_shirts");
            entity.Property(e => e.GiftFlag).HasColumnName("gift_flag");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.ItemidParts1).HasColumnName("itemid_parts_1");
            entity.Property(e => e.ItemidParts10).HasColumnName("itemid_parts_10");
            entity.Property(e => e.ItemidParts11).HasColumnName("itemid_parts_11");
            entity.Property(e => e.ItemidParts12).HasColumnName("itemid_parts_12");
            entity.Property(e => e.ItemidParts13).HasColumnName("itemid_parts_13");
            entity.Property(e => e.ItemidParts14).HasColumnName("itemid_parts_14");
            entity.Property(e => e.ItemidParts15).HasColumnName("itemid_parts_15");
            entity.Property(e => e.ItemidParts16).HasColumnName("itemid_parts_16");
            entity.Property(e => e.ItemidParts17).HasColumnName("itemid_parts_17");
            entity.Property(e => e.ItemidParts18).HasColumnName("itemid_parts_18");
            entity.Property(e => e.ItemidParts19).HasColumnName("itemid_parts_19");
            entity.Property(e => e.ItemidParts2).HasColumnName("itemid_parts_2");
            entity.Property(e => e.ItemidParts20).HasColumnName("itemid_parts_20");
            entity.Property(e => e.ItemidParts21).HasColumnName("itemid_parts_21");
            entity.Property(e => e.ItemidParts22).HasColumnName("itemid_parts_22");
            entity.Property(e => e.ItemidParts23).HasColumnName("itemid_parts_23");
            entity.Property(e => e.ItemidParts24).HasColumnName("itemid_parts_24");
            entity.Property(e => e.ItemidParts3).HasColumnName("itemid_parts_3");
            entity.Property(e => e.ItemidParts4).HasColumnName("itemid_parts_4");
            entity.Property(e => e.ItemidParts5).HasColumnName("itemid_parts_5");
            entity.Property(e => e.ItemidParts6).HasColumnName("itemid_parts_6");
            entity.Property(e => e.ItemidParts7).HasColumnName("itemid_parts_7");
            entity.Property(e => e.ItemidParts8).HasColumnName("itemid_parts_8");
            entity.Property(e => e.ItemidParts9).HasColumnName("itemid_parts_9");
            entity.Property(e => e.Mastery).HasColumnName("mastery");
            entity.Property(e => e.Parts1).HasColumnName("parts_1");
            entity.Property(e => e.Parts10).HasColumnName("parts_10");
            entity.Property(e => e.Parts11).HasColumnName("parts_11");
            entity.Property(e => e.Parts12).HasColumnName("parts_12");
            entity.Property(e => e.Parts13).HasColumnName("parts_13");
            entity.Property(e => e.Parts14).HasColumnName("parts_14");
            entity.Property(e => e.Parts15).HasColumnName("parts_15");
            entity.Property(e => e.Parts16).HasColumnName("parts_16");
            entity.Property(e => e.Parts17).HasColumnName("parts_17");
            entity.Property(e => e.Parts18).HasColumnName("parts_18");
            entity.Property(e => e.Parts19).HasColumnName("parts_19");
            entity.Property(e => e.Parts2).HasColumnName("parts_2");
            entity.Property(e => e.Parts20).HasColumnName("parts_20");
            entity.Property(e => e.Parts21).HasColumnName("parts_21");
            entity.Property(e => e.Parts22).HasColumnName("parts_22");
            entity.Property(e => e.Parts23).HasColumnName("parts_23");
            entity.Property(e => e.Parts24).HasColumnName("parts_24");
            entity.Property(e => e.Parts3).HasColumnName("parts_3");
            entity.Property(e => e.Parts4).HasColumnName("parts_4");
            entity.Property(e => e.Parts5).HasColumnName("parts_5");
            entity.Property(e => e.Parts6).HasColumnName("parts_6");
            entity.Property(e => e.Parts7).HasColumnName("parts_7");
            entity.Property(e => e.Parts8).HasColumnName("parts_8");
            entity.Property(e => e.Parts9).HasColumnName("parts_9");
            entity.Property(e => e.Pcl0).HasColumnName("PCL0");
            entity.Property(e => e.Pcl1).HasColumnName("PCL1");
            entity.Property(e => e.Pcl2).HasColumnName("PCL2");
            entity.Property(e => e.Pcl3).HasColumnName("PCL3");
            entity.Property(e => e.Pcl4).HasColumnName("PCL4");
            entity.Property(e => e.Purchase).HasColumnName("purchase");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
        });

        modelBuilder.Entity<PangyaRankConfig>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK_pangya_rank_config_index");

            entity.ToTable("pangya_rank_config", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.RefreshTimeH).HasColumnName("refresh_time_H");
            entity.Property(e => e.RegDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("reg_date");
        });

        modelBuilder.Entity<PangyaRecord>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_record", "pangya");

            entity.Property(e => e.Assist).HasColumnName("assist");
            entity.Property(e => e.BestPang).HasColumnName("best_pang");
            entity.Property(e => e.BestScore)
                .HasDefaultValue((short)127)
                .HasColumnName("best_score");
            entity.Property(e => e.CharacterTypeid).HasColumnName("character_typeid");
            entity.Property(e => e.Course).HasColumnName("course");
            entity.Property(e => e.EventScore).HasColumnName("event_score");
            entity.Property(e => e.Fairway).HasColumnName("fairway");
            entity.Property(e => e.Hole).HasColumnName("hole");
            entity.Property(e => e.Holein).HasColumnName("holein");
            entity.Property(e => e.Putt).HasColumnName("putt");
            entity.Property(e => e.Puttin).HasColumnName("puttin");
            entity.Property(e => e.Tacada).HasColumnName("tacada");
            entity.Property(e => e.Tipo).HasColumnName("tipo");
            entity.Property(e => e.TotalScore).HasColumnName("total_score");
            entity.Property(e => e.Uid).HasColumnName("UID");
        });

        modelBuilder.Entity<PangyaRescuePwdLog>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_rescue_pwd_log", "pangya");

            entity.HasIndex(e => e.Uid, "IX_pangya_rescue_pwd_log");

            entity.HasIndex(e => e.Index, "IX_pangya_rescue_pwd_log_2").IsUnique();

            entity.HasIndex(e => e.Index, "IX_pangya_rescue_pwd_log_3").IsUnique();

            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.KeyUniq)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("key_uniq");
            entity.Property(e => e.SendDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("send_date");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.Tipo)
                .HasDefaultValue((byte)1)
                .HasColumnName("tipo");
            entity.Property(e => e.Uid).HasColumnName("uid");
        });

        modelBuilder.Entity<PangyaRewardSsc>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_reward_ssc", "pangya");

            entity.Property(e => e.Probabilidade).HasColumnName("probabilidade");
            entity.Property(e => e.Tipo).HasColumnName("tipo");
            entity.Property(e => e.Valor).HasColumnName("valor");
        });

        modelBuilder.Entity<PangyaRoomLog>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_room_log", "pangya");

            entity.Property(e => e.AlbaHit).HasColumnName("Alba_Hit");
            entity.Property(e => e.BirdieHit).HasColumnName("Birdie_Hit");
            entity.Property(e => e.BogeyHit).HasColumnName("Bogey_Hit");
            entity.Property(e => e.Data)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DoubleBogeyHit).HasColumnName("DoubleBogey_Hit");
            entity.Property(e => e.EagleHit)
                .HasDefaultValueSql("('0')")
                .HasColumnName("Eagle_Hit");
            entity.Property(e => e.GmEvent)
                .HasDefaultValueSql("('0')")
                .HasColumnName("GM_EVENT");
            entity.Property(e => e.HioHit).HasColumnName("Hio_Hit");
            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("index");
            entity.Property(e => e.MasterUid).HasColumnName("Master_UID");
            entity.Property(e => e.MaxPlayers)
                .HasDefaultValueSql("('0')")
                .HasColumnName("Max_Players");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.NumberPlayers)
                .HasDefaultValueSql("('0')")
                .HasColumnName("Number_Players");
            entity.Property(e => e.ParHit).HasColumnName("Par_Hit");
            entity.Property(e => e.RoomId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Score).HasColumnType("numeric(18, 0)");
            entity.Property(e => e.TripleBogeyHit).HasColumnName("TripleBogey_Hit");
            entity.Property(e => e.Uid).HasColumnName("UID");
        });

        modelBuilder.Entity<PangyaServerList>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_server_list", "pangya");

            entity.HasIndex(e => e.Uid, "IX_pangya_server_list").IsUnique();

            entity.Property(e => e.ClientVersion)
                .IsRequired()
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Ip)
                .IsRequired()
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("IP");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("JaCk2 Server");
            entity.Property(e => e.PcbangUser).HasColumnName("PCBangUser");
            entity.Property(e => e.Property).HasColumnName("property");
            entity.Property(e => e.ServerVersion)
                .IsRequired()
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasDefaultValue("");
            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.UpdateTime).HasPrecision(0);
        });

        modelBuilder.Entity<PangyaShopGift>(entity =>
        {
            entity.HasKey(e => e.GiftId).HasName("PK__pangya_s__C1A26301B9694CA3");

            entity.ToTable("pangya_shop_gift", "pangya");

            entity.Property(e => e.GiftId).HasColumnName("gift_id");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.GiftName)
                .IsRequired()
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("gift_name");
            entity.Property(e => e.ItemName)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("item name")
                .HasColumnName("item_name");
            entity.Property(e => e.ItemPeriod)
                .HasDefaultValue(30)
                .HasColumnName("item_period");
            entity.Property(e => e.ItemQntd).HasColumnName("item_qntd");
            entity.Property(e => e.ItemQntdTime).HasColumnName("item_qntd_time");
            entity.Property(e => e.ItemTypeid).HasColumnName("item_typeid");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("reg_date");
            entity.Property(e => e.RequiredPrice).HasColumnName("required_price");
        });

        modelBuilder.Entity<PangyaShopGiftLog>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK__pangya_s__1D0A3348E3DDBF0B");

            entity.ToTable("pangya_shop_gift_log", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.GiftId).HasColumnName("gift_id");
            entity.Property(e => e.ItemQntd).HasColumnName("item_qntd");
            entity.Property(e => e.ItemTypeid).HasColumnName("item_typeid");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("reg_date");
            entity.Property(e => e.Uid).HasColumnName("uid");
        });

        modelBuilder.Entity<PangyaShutdownList>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_pangya_shutdown_list_id");

            entity.ToTable("pangya_shutdown_list", "pangya");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DateShutdown)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("date_shutdown");
            entity.Property(e => e.RefreshTime).HasColumnName("refreshTime");
            entity.Property(e => e.ReplayCount)
                .HasDefaultValue(1)
                .HasColumnName("replayCount");
        });

        modelBuilder.Entity<PangyaTickerList>(entity =>
        {
            entity.HasKey(e => e.TickerId).HasName("PK_pangya_ticker_list_ticker_id");

            entity.ToTable("pangya_ticker_list", "pangya");

            entity.Property(e => e.TickerId).HasColumnName("ticker_id");
            entity.Property(e => e.Message)
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnName("message");
            entity.Property(e => e.Nick)
                .IsRequired()
                .HasMaxLength(22)
                .IsUnicode(false)
                .HasColumnName("nick");
            entity.Property(e => e.RefreshTime).HasColumnName("refreshTime");
            entity.Property(e => e.ReplayCount)
                .HasDefaultValue(1)
                .HasColumnName("replayCount");
        });

        modelBuilder.Entity<PangyaTicketReport>(entity =>
        {
            entity.HasKey(e => e.Idx).HasName("PK_pangya_ticket_report_idx");

            entity.ToTable("pangya_ticket_report", "pangya");

            entity.Property(e => e.Idx).HasColumnName("idx");
            entity.Property(e => e.Flag).HasColumnName("flag");
            entity.Property(e => e.RegDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("reg_date");
            entity.Property(e => e.Tipo)
                .HasDefaultValue(4)
                .HasColumnName("tipo");
            entity.Property(e => e.TrofelTypeid)
                .HasDefaultValue(738197504)
                .HasColumnName("trofel_typeid");
        });

        modelBuilder.Entity<PangyaTicketReportDado>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_ticket_report_dados", "pangya");

            entity.Property(e => e.FinishDate).HasColumnName("finish_date");
            entity.Property(e => e.FlagItemPang).HasColumnName("flag_item_pang");
            entity.Property(e => e.FlagPremiumUser).HasColumnName("flag_premium_user");
            entity.Property(e => e.PlayerBonusPang).HasColumnName("player_bonus_pang");
            entity.Property(e => e.PlayerExp).HasColumnName("player_exp");
            entity.Property(e => e.PlayerMascotTypeid).HasColumnName("player_mascot_typeid");
            entity.Property(e => e.PlayerMedalha).HasColumnName("player_medalha");
            entity.Property(e => e.PlayerPang).HasColumnName("player_pang");
            entity.Property(e => e.PlayerScore).HasColumnName("player_score");
            entity.Property(e => e.PlayerState).HasColumnName("player_state");
            entity.Property(e => e.PlayerTrofel).HasColumnName("player_trofel");
            entity.Property(e => e.PlayerUid).HasColumnName("player_uid");
            entity.Property(e => e.ReportId).HasColumnName("report_id");
        });

        modelBuilder.Entity<PangyaTikiPoint>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_tiki_points", "pangya");

            entity.Property(e => e.ModDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("MOD_DATE");
            entity.Property(e => e.RegDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("REG_DATE");
            entity.Property(e => e.TikiPoints).HasColumnName("Tiki_Points");
            entity.Property(e => e.Uid).HasColumnName("UID");
        });

        modelBuilder.Entity<PangyaTikiPointsItem>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_tiki_points_items", "pangya");

            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("INDEX");
            entity.Property(e => e.ItemActive).HasColumnName("ITEM_ACTIVE");
            entity.Property(e => e.ItemFlag).HasColumnName("ITEM_FLAG");
            entity.Property(e => e.ItemName)
                .HasMaxLength(50)
                .HasColumnName("ITEM_NAME");
            entity.Property(e => e.ItemQntd).HasColumnName("ITEM_QNTD");
            entity.Property(e => e.ItemTypeid).HasColumnName("ITEM_TYPEID");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("REG_DATE");
            entity.Property(e => e.ReqPoints).HasColumnName("REQ_POINTS");
        });

        modelBuilder.Entity<PangyaTransformeClubsetTemp>(entity =>
        {
            entity.HasKey(e => e.TransIndex).HasName("PK_pangya_transforme_clubset_temp_trans_index");

            entity.ToTable("pangya_transforme_clubset_temp", "pangya");

            entity.Property(e => e.TransIndex).HasColumnName("trans_index");
            entity.Property(e => e.CardQntd).HasColumnName("CARD_QNTD");
            entity.Property(e => e.CardTypeid).HasColumnName("CARD_TYPEID");
            entity.Property(e => e.Flag).HasColumnName("FLAG");
            entity.Property(e => e.Mastery).HasColumnName("MASTERY");
            entity.Property(e => e.State).HasColumnName("STATE");
            entity.Property(e => e.State2).HasColumnName("STATE2");
            entity.Property(e => e.TaqueiraId).HasColumnName("TAQUEIRA_ID");
            entity.Property(e => e.TaqueiraTransTypeid).HasColumnName("TAQUEIRA_TRANS_TYPEID");
            entity.Property(e => e.Uid).HasColumnName("UID");
        });

        modelBuilder.Entity<PangyaTreasureHunterEventItem>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_treasure_hunter_event_item", "pangya");

            entity.Property(e => e.Flag).HasColumnName("flag");
            entity.Property(e => e.Index).ValueGeneratedOnAdd();
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Probabilidade).HasColumnName("probabilidade");
            entity.Property(e => e.Quantidade).HasColumnName("quantidade");
            entity.Property(e => e.Tipo).HasColumnName("tipo");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
        });

        modelBuilder.Entity<PangyaTreasureItem>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_treasure_item", "pangya");

            entity.Property(e => e.Flag).HasColumnName("flag");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.Probabilidade).HasColumnName("probabilidade");
            entity.Property(e => e.Quantidade).HasColumnName("quantidade");
            entity.Property(e => e.Tipo).HasColumnName("tipo");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
        });

        modelBuilder.Entity<PangyaTrofelEspecial>(entity =>
        {
            entity.HasKey(e => e.ItemId).HasName("PK_pangya_trofel_especial_item_id");

            entity.ToTable("pangya_trofel_especial", "pangya");

            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Qntd).HasColumnName("qntd");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
            entity.Property(e => e.Uid).HasColumnName("UID");
        });

        modelBuilder.Entity<PangyaTrofelGrandprix>(entity =>
        {
            entity.HasKey(e => e.ItemId).HasName("PK_pangya_trofel_grandprix_item_id");

            entity.ToTable("pangya_trofel_grandprix", "pangya");

            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Qntd).HasColumnName("qntd");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
            entity.Property(e => e.Uid).HasColumnName("UID");
        });

        modelBuilder.Entity<PangyaUserEquip>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("PK_pangya_user_equip_UID");

            entity.ToTable("pangya_user_equip", "pangya");

            entity.Property(e => e.Uid)
                .ValueGeneratedNever()
                .HasColumnName("UID");
            entity.Property(e => e.BallType).HasColumnName("ball_type");
            entity.Property(e => e.CaddieId).HasColumnName("caddie_id");
            entity.Property(e => e.CharacterId).HasColumnName("character_id");
            entity.Property(e => e.ClubId).HasColumnName("club_id");
            entity.Property(e => e.ItemSlot1).HasColumnName("item_slot_1");
            entity.Property(e => e.ItemSlot10).HasColumnName("item_slot_10");
            entity.Property(e => e.ItemSlot2).HasColumnName("item_slot_2");
            entity.Property(e => e.ItemSlot3).HasColumnName("item_slot_3");
            entity.Property(e => e.ItemSlot4).HasColumnName("item_slot_4");
            entity.Property(e => e.ItemSlot5).HasColumnName("item_slot_5");
            entity.Property(e => e.ItemSlot6).HasColumnName("item_slot_6");
            entity.Property(e => e.ItemSlot7).HasColumnName("item_slot_7");
            entity.Property(e => e.ItemSlot8).HasColumnName("item_slot_8");
            entity.Property(e => e.ItemSlot9).HasColumnName("item_slot_9");
            entity.Property(e => e.MascotId).HasColumnName("mascot_id");
            entity.Property(e => e.Poster1).HasColumnName("poster_1");
            entity.Property(e => e.Poster2).HasColumnName("poster_2");
            entity.Property(e => e.Skin1).HasColumnName("Skin_1");
            entity.Property(e => e.Skin2).HasColumnName("Skin_2");
            entity.Property(e => e.Skin3).HasColumnName("Skin_3");
            entity.Property(e => e.Skin4).HasColumnName("Skin_4");
            entity.Property(e => e.Skin5).HasColumnName("Skin_5");
            entity.Property(e => e.Skin6).HasColumnName("Skin_6");
        });

        modelBuilder.Entity<PangyaUserMacro>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_user_macro", "pangya");

            entity.Property(e => e.Macro1)
                .HasMaxLength(500)
                .HasDefaultValue("Pangya!");
            entity.Property(e => e.Macro10)
                .HasMaxLength(500)
                .HasDefaultValue("Pangya!");
            entity.Property(e => e.Macro2)
                .HasMaxLength(500)
                .HasDefaultValue("Pangya!");
            entity.Property(e => e.Macro3)
                .HasMaxLength(500)
                .HasDefaultValue("Pangya!");
            entity.Property(e => e.Macro4)
                .HasMaxLength(500)
                .HasDefaultValue("Pangya!");
            entity.Property(e => e.Macro5)
                .HasMaxLength(500)
                .HasDefaultValue("Pangya!");
            entity.Property(e => e.Macro6)
                .HasMaxLength(500)
                .HasDefaultValue("Pangya!");
            entity.Property(e => e.Macro7)
                .HasMaxLength(500)
                .HasDefaultValue("Pangya!");
            entity.Property(e => e.Macro8)
                .HasMaxLength(500)
                .HasDefaultValue("Pangya!");
            entity.Property(e => e.Macro9)
                .HasMaxLength(500)
                .HasDefaultValue("Pangya!");
            entity.Property(e => e.Uid).HasColumnName("UID");
        });

        modelBuilder.Entity<PangyaUsersEditorIff>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("PK__pangya_u__C5B19602111F3A37");

            entity.ToTable("pangya_users_editor_iff", "pangya");

            entity.HasIndex(e => e.Username, "UQ__pangya_u__536C85E418755C13").IsUnique();

            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.EndDate)
                .HasDefaultValueSql("(NULL)")
                .HasColumnType("datetime");
            entity.Property(e => e.Hwid)
                .HasMaxLength(128)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("HWID");
            entity.Property(e => e.LastAcess).HasColumnType("datetime");
            entity.Property(e => e.MacAdress)
                .HasMaxLength(20)
                .HasDefaultValueSql("(NULL)");
            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(e => e.StartDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Time).HasDefaultValue(120);
            entity.Property(e => e.Tipo).HasDefaultValue(1);
            entity.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(50);
        });

        modelBuilder.Entity<PangyaWeblinkCookiesKey>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK_pangya_weblink_cookies_key_index");

            entity.ToTable("pangya_weblink_cookies_key", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.Key)
                .IsRequired()
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasDefaultValue("123456")
                .HasColumnName("key");
            entity.Property(e => e.Uid).HasColumnName("uid");
            entity.Property(e => e.Valid)
                .HasDefaultValue((short)1)
                .HasColumnName("valid");
        });

        modelBuilder.Entity<PangyaWeblinkKey>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK_pangya_weblink_key_index");

            entity.ToTable("pangya_weblink_key", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.Key)
                .IsRequired()
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasDefaultValue("123456")
                .HasColumnName("key");
            entity.Property(e => e.Uid).HasColumnName("uid");
            entity.Property(e => e.Valid)
                .HasDefaultValue((short)1)
                .HasColumnName("valid");
        });

        modelBuilder.Entity<PangyaWorldTourConfig>(entity =>
        {
            entity.HasKey(e => e.EventId).HasName("PK_WorldTourConfig");

            entity.ToTable("pangya_world_tour_config", "pangya");

            entity.Property(e => e.EventId).HasColumnName("EventID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100)
                .HasDefaultValue("World Tour");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysdatetime())");
        });

        modelBuilder.Entity<PangyaWorldTourEvent>(entity =>
        {
            entity.HasKey(e => e.Index)
                .IsClustered(false)
                .HasFillFactor(90);

            entity.ToTable("pangya_world_tour_event", "pangya");

            entity.Property(e => e.FinishData)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("Finish_Data");
            entity.Property(e => e.Uid).HasColumnName("UID");
        });

        modelBuilder.Entity<PangyaWorldTourEventItem>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("pangya_world_tour_event_items", "pangya");

            entity.Property(e => e.EndEvent).HasColumnName("END_EVENT");
            entity.Property(e => e.EventDescription)
                .HasMaxLength(255)
                .HasColumnName("EVENT_DESCRIPTION");
            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .HasColumnName("INDEX");
            entity.Property(e => e.ItemName)
                .HasMaxLength(50)
                .HasColumnName("ITEM_NAME");
            entity.Property(e => e.ItemQntd).HasColumnName("ITEM_QNTD");
            entity.Property(e => e.ItemQntdTime).HasColumnName("ITEM_QNTD_TIME");
            entity.Property(e => e.ItemTypeid).HasColumnName("ITEM_TYPEID");
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("REG_DATE");
            entity.Property(e => e.TourEvent)
                .HasMaxLength(50)
                .HasColumnName("TOUR_EVENT");
        });

        modelBuilder.Entity<PangyaWorldTourEventLog>(entity =>
        {
            entity.HasKey(e => e.Index)
                .IsClustered(false)
                .HasFillFactor(90);

            entity.ToTable("pangya_world_tour_event_log", "pangya", tb => tb.HasComment("envia o presente sim ou nao"));

            entity.Property(e => e.FinishData)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("Finish_Data");
            entity.Property(e => e.Uid).HasColumnName("UID");
        });

        modelBuilder.Entity<QuestItem>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK_quest_items_index");

            entity.ToTable("quest_items", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.Nome)
                .IsRequired()
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasDefaultValue("")
                .HasColumnName("nome");
            entity.Property(e => e.StuffTypeid).HasColumnName("stuff_typeid");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
        });

        modelBuilder.Entity<QuestStuff>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK_quest_stuffs_index");

            entity.ToTable("quest_stuffs", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.CounterQntd).HasColumnName("counter_qntd");
            entity.Property(e => e.CounterTypeid).HasColumnName("counter_typeid");
            entity.Property(e => e.Nome)
                .IsRequired()
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasDefaultValue("")
                .HasColumnName("nome");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
        });

        modelBuilder.Entity<ScratchyItem>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("scratchy_item", "pangya");

            entity.Property(e => e.Active).HasDefaultValue((short)1);
            entity.Property(e => e.Flag).HasColumnName("flag");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Numero).HasDefaultValue(-1);
            entity.Property(e => e.TypeId).HasColumnName("TypeID");
        });

        modelBuilder.Entity<ScratchyRareWin>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("scratchy_rare_win", "pangya");

            entity.Property(e => e.RegDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("REG_DATE");
            entity.Property(e => e.TypeId).HasColumnName("TypeID");
            entity.Property(e => e.Uid).HasColumnName("UID");
        });

        modelBuilder.Entity<ScratchyRate>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("scratchy_rate", "pangya");

            entity.Property(e => e.Nome)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("nome");
            entity.Property(e => e.Probabilidade).HasColumnName("probabilidade");
            entity.Property(e => e.Tipo).HasColumnName("tipo");
        });

        modelBuilder.Entity<ShopProduct>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__shop_pro__3214EC27CC8904E0");

            entity.ToTable("shop_products", "pangya");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Amount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Category)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(255)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ShopProductItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__shop_pro__3214EC27B82E4F15");

            entity.ToTable("shop_product_items", "pangya");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ItemId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("ItemID");
            entity.Property(e => e.ShopProductId).HasColumnName("ShopProductID");

            entity.HasOne(d => d.ShopProduct).WithMany(p => p.ShopProductItems)
                .HasForeignKey(d => d.ShopProductId)
                .HasConstraintName("fk_shop_product_items_shop_product_id");
        });

        modelBuilder.Entity<ShopPurchase>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__shop_pur__3214EC27ECEF865D");

            entity.ToTable("shop_purchases", "pangya");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.AccountId)
                .IsRequired()
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("AccountID");
            entity.Property(e => e.Amount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.PaymentLink)
                .HasMaxLength(500)
                .HasColumnName("payment_link");
            entity.Property(e => e.ShopProductId).HasColumnName("ShopProductID");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Account).WithMany(p => p.ShopPurchases)
                .HasPrincipalKey(p => p.Id)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("fk_shop_purchases_account_id");

            entity.HasOne(d => d.ShopProduct).WithMany(p => p.ShopPurchases)
                .HasForeignKey(d => d.ShopProductId)
                .HasConstraintName("fk_shop_purchases_shop_product_id");
        });

        modelBuilder.Entity<TdCharEquipS4>(entity =>
        {
            entity.HasKey(e => e.Seq).HasName("PK_td_char_equip_s4_SEQ");

            entity.ToTable("td_char_equip_s4", "pangya");

            entity.Property(e => e.Seq).HasColumnName("SEQ");
            entity.Property(e => e.CharItemid).HasColumnName("CHAR_ITEMID");
            entity.Property(e => e.EquipNum).HasColumnName("EQUIP_NUM");
            entity.Property(e => e.EquipType).HasColumnName("EQUIP_TYPE");
            entity.Property(e => e.InDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("IN_DATE");
            entity.Property(e => e.Itemid).HasColumnName("ITEMID");
            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.UseYn)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue("Y")
                .HasColumnName("USE_YN");
        });

        modelBuilder.Entity<TdRoomDatum>(entity =>
        {
            entity.HasKey(e => e.MyroomId).HasName("PK_td_room_data_MYROOM_ID");

            entity.ToTable("td_room_data", "pangya");

            entity.Property(e => e.MyroomId).HasColumnName("MYROOM_ID");
            entity.Property(e => e.DisplayYn)
                .IsRequired()
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue("N")
                .HasColumnName("DISPLAY_YN");
            entity.Property(e => e.ModDt)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("MOD_DT");
            entity.Property(e => e.ModSeq).HasColumnName("MOD_SEQ");
            entity.Property(e => e.PosR).HasColumnName("POS_R");
            entity.Property(e => e.PosX).HasColumnName("POS_X");
            entity.Property(e => e.PosY).HasColumnName("POS_Y");
            entity.Property(e => e.PosZ).HasColumnName("POS_Z");
            entity.Property(e => e.RoomNo).HasColumnName("ROOM_NO");
            entity.Property(e => e.Typeid).HasColumnName("TYPEID");
            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.UseYn)
                .IsRequired()
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue("Y")
                .HasColumnName("USE_YN");
            entity.Property(e => e.Valid)
                .HasDefaultValue((byte)1)
                .HasColumnName("valid");
        });

        modelBuilder.Entity<TempCounterTypeidInit>(entity =>
        {
            entity.HasKey(e => e.Index).HasName("PK_temp_counter_typeid_init_index");

            entity.ToTable("temp_counter_typeid_init", "pangya");

            entity.Property(e => e.Index).HasColumnName("index");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
            entity.Property(e => e.Uid).HasColumnName("uid");
        });

        modelBuilder.Entity<TempTmp>(entity =>
        {
            entity.HasKey(e => e.Idx).HasName("PK_temp_tmp_idx");

            entity.ToTable("temp_tmp", "pangya");

            entity.Property(e => e.Idx).HasColumnName("idx");
            entity.Property(e => e.C1).HasColumnName("c1");
            entity.Property(e => e.C2).HasColumnName("c2");
            entity.Property(e => e.C3).HasColumnName("c3");
            entity.Property(e => e.C4).HasColumnName("c4");
            entity.Property(e => e.C5).HasColumnName("c5");
            entity.Property(e => e.Cookie).HasColumnName("cookie");
            entity.Property(e => e.CouponId).HasColumnName("coupon_id");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.ItemTempo).HasColumnName("item_tempo");
            entity.Property(e => e.ItemTipo).HasColumnName("item_tipo");
            entity.Property(e => e.Pang).HasColumnName("pang");
            entity.Property(e => e.Qntd).HasColumnName("qntd");
            entity.Property(e => e.R)
                .HasColumnType("numeric(10, 0)")
                .HasColumnName("r");
            entity.Property(e => e.Time).HasColumnName("time");
            entity.Property(e => e.Tipo).HasColumnName("tipo");
            entity.Property(e => e.Typeid).HasColumnName("typeid");
            entity.Property(e => e.Uccidx)
                .IsRequired()
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("UCCIDX");
            entity.Property(e => e.Uid).HasColumnName("uid");
            entity.Property(e => e.X)
                .HasColumnType("numeric(10, 0)")
                .HasColumnName("x");
            entity.Property(e => e.Y)
                .HasColumnType("numeric(10, 0)")
                .HasColumnName("y");
            entity.Property(e => e.Z)
                .HasColumnType("numeric(10, 0)")
                .HasColumnName("z");
        });

        modelBuilder.Entity<TempTypeid>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("temp_typeid", "pangya");

            entity.Property(e => e.ItemId).HasColumnName("ITEM_ID");
            entity.Property(e => e.QntdDia).HasColumnName("QNTD_DIA");
            entity.Property(e => e.Quantidade).HasColumnName("QUANTIDADE");
            entity.Property(e => e.Typeid).HasColumnName("TYPEID");
        });

        modelBuilder.Entity<TrofelStat>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("PK_trofel_stat_UID");

            entity.ToTable("trofel_stat", "pangya");

            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.Ama1B).HasColumnName("AMA_1_B");
            entity.Property(e => e.Ama1G).HasColumnName("AMA_1_G");
            entity.Property(e => e.Ama1S).HasColumnName("AMA_1_S");
            entity.Property(e => e.Ama2B).HasColumnName("AMA_2_B");
            entity.Property(e => e.Ama2G).HasColumnName("AMA_2_G");
            entity.Property(e => e.Ama2S).HasColumnName("AMA_2_S");
            entity.Property(e => e.Ama3B).HasColumnName("AMA_3_B");
            entity.Property(e => e.Ama3G).HasColumnName("AMA_3_G");
            entity.Property(e => e.Ama3S).HasColumnName("AMA_3_S");
            entity.Property(e => e.Ama4B).HasColumnName("AMA_4_B");
            entity.Property(e => e.Ama4G).HasColumnName("AMA_4_G");
            entity.Property(e => e.Ama4S).HasColumnName("AMA_4_S");
            entity.Property(e => e.Ama5B).HasColumnName("AMA_5_B");
            entity.Property(e => e.Ama5G).HasColumnName("AMA_5_G");
            entity.Property(e => e.Ama5S).HasColumnName("AMA_5_S");
            entity.Property(e => e.Ama6B).HasColumnName("AMA_6_B");
            entity.Property(e => e.Ama6G).HasColumnName("AMA_6_G");
            entity.Property(e => e.Ama6S).HasColumnName("AMA_6_S");
            entity.Property(e => e.Pro1B).HasColumnName("PRO_1_B");
            entity.Property(e => e.Pro1G).HasColumnName("PRO_1_G");
            entity.Property(e => e.Pro1S).HasColumnName("PRO_1_S");
            entity.Property(e => e.Pro2B).HasColumnName("PRO_2_B");
            entity.Property(e => e.Pro2G).HasColumnName("PRO_2_G");
            entity.Property(e => e.Pro2S).HasColumnName("PRO_2_S");
            entity.Property(e => e.Pro3B).HasColumnName("PRO_3_B");
            entity.Property(e => e.Pro3G).HasColumnName("PRO_3_G");
            entity.Property(e => e.Pro3S).HasColumnName("PRO_3_S");
            entity.Property(e => e.Pro4B).HasColumnName("PRO_4_B");
            entity.Property(e => e.Pro4G).HasColumnName("PRO_4_G");
            entity.Property(e => e.Pro4S).HasColumnName("PRO_4_S");
            entity.Property(e => e.Pro5B).HasColumnName("PRO_5_B");
            entity.Property(e => e.Pro5G).HasColumnName("PRO_5_G");
            entity.Property(e => e.Pro5S).HasColumnName("PRO_5_S");
            entity.Property(e => e.Pro6B).HasColumnName("PRO_6_B");
            entity.Property(e => e.Pro6G).HasColumnName("PRO_6_G");
            entity.Property(e => e.Pro6S).HasColumnName("PRO_6_S");
            entity.Property(e => e.Pro7B).HasColumnName("PRO_7_B");
            entity.Property(e => e.Pro7G).HasColumnName("PRO_7_G");
            entity.Property(e => e.Pro7S).HasColumnName("PRO_7_S");
        });

        modelBuilder.Entity<TuUcc>(entity =>
        {
            entity.HasKey(e => new { e.Uid, e.Typeid, e.Seq, e.ItemId }).HasName("PK_tu_ucc_UID");

            entity.ToTable("tu_ucc", "pangya");

            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.Typeid).HasColumnName("TYPEID");
            entity.Property(e => e.Seq)
                .HasDefaultValue(1)
                .HasColumnName("SEQ");
            entity.Property(e => e.ItemId)
                .HasColumnType("numeric(20, 0)")
                .HasColumnName("ITEM_ID");
            entity.Property(e => e.Copier)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("COPIER");
            entity.Property(e => e.CopierNick)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("COPIER_NICK");
            entity.Property(e => e.DrawDt)
                .HasPrecision(0)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("DRAW_DT");
            entity.Property(e => e.InDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("IN_DATE");
            entity.Property(e => e.Skey)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("SKEY");
            entity.Property(e => e.Status).HasColumnName("STATUS");
            entity.Property(e => e.Trade).HasColumnName("TRADE");
            entity.Property(e => e.UccName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValueSql("(NULL)")
                .HasColumnName("UCC_NAME");
            entity.Property(e => e.Uccidx)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasDefaultValue("")
                .HasColumnName("UCCIDX");
            entity.Property(e => e.UseYn)
                .IsRequired()
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue("N")
                .HasColumnName("USE_YN");
        });

        modelBuilder.Entity<Tutorial>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("PK_tutorial_UID");

            entity.ToTable("tutorial", "pangya");

            entity.Property(e => e.Uid).HasColumnName("UID");
        });

        modelBuilder.Entity<TypeList>(entity =>
        {
            entity.HasKey(e => e.TypeId).HasName("PK_type_list_TypeId");

            entity.ToTable("type_list", "pangya");

            entity.Property(e => e.Icon)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("0");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasDefaultValue("0");
            entity.Property(e => e.Type).HasColumnName("type");
        });

        modelBuilder.Entity<UserInfo>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("PK_user_info_UID");

            entity.ToTable("user_info", "pangya");

            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.AcertoPangya).HasColumnName("Acerto_pangya");
            entity.Property(e => e.AcertoPutt).HasColumnName("Acerto_Putt");
            entity.Property(e => e.BestChipinMedal).HasColumnName("best_chipin_medal");
            entity.Property(e => e.BestDriveMedal).HasColumnName("best_drive_medal");
            entity.Property(e => e.BestPuttinMedal).HasColumnName("best_puttin_medal");
            entity.Property(e => e.BestRecoveryMedal).HasColumnName("best_recovery_medal");
            entity.Property(e => e.ChipIn).HasColumnName("Chip-in");
            entity.Property(e => e.FastMedal).HasColumnName("fast_medal");
            entity.Property(e => e.Hio).HasColumnName("HIO");
            entity.Property(e => e.LadderPoint).HasDefaultValue(1000);
            entity.Property(e => e.Level).HasColumnName("level");
            entity.Property(e => e.LongPutt).HasColumnName("Long-putt");
            entity.Property(e => e.LuckyMedal).HasColumnName("lucky_medal");
            entity.Property(e => e.MaxDistancia).HasColumnName("Max_distancia");
            entity.Property(e => e.MaxPang1).HasColumnName("maxPang1");
            entity.Property(e => e.MaxPang2).HasColumnName("maxPang2");
            entity.Property(e => e.MaxPang3).HasColumnName("maxPang3");
            entity.Property(e => e.MaxPang4).HasColumnName("maxPang4");
            entity.Property(e => e.MediaScore).HasColumnName("Media_score");
            entity.Property(e => e.OB).HasColumnName("O.B");
            entity.Property(e => e.SkinRunHole).HasDefaultValue(-1);
            entity.Property(e => e.TempoTacadas).HasColumnName("Tempo tacadas");
            entity.Property(e => e.TodosCombos).HasColumnName("Todos_combos");
            entity.Property(e => e.TotalDistancia).HasColumnName("Total_distancia");
            entity.Property(e => e.TotalPangWinGame).HasColumnName("total_pang_win_game");
            entity.Property(e => e._16bitNaosei).HasColumnName("16bit_naosei");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
