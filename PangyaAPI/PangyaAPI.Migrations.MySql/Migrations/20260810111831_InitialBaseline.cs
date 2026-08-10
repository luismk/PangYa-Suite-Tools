using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace PangyaAPI.Migrations.MySql.Migrations
{
    /// <inheritdoc />
    public partial class InitialBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "account",
                schema: "pangya",
                columns: table => new
                {
                    UID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ID = table.Column<string>(type: "varchar(25)", unicode: false, maxLength: 25, nullable: false, defaultValue: ""),
                    PASSWORD = table.Column<string>(type: "varchar(33)", unicode: false, maxLength: 33, nullable: false, defaultValue: ""),
                    IDState = table.Column<long>(type: "bigint", nullable: false),
                    LastLogonTime = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(NULL)"),
                    BlockTime = table.Column<int>(type: "int", nullable: false),
                    Logon = table.Column<short>(type: "smallint", nullable: false),
                    FIRST_LOGIN = table.Column<short>(type: "smallint", nullable: false),
                    RegDate = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(NULL)"),
                    NICK = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, defaultValue: ""),
                    FIRST_SET = table.Column<short>(type: "smallint", nullable: false),
                    Guild_UID = table.Column<int>(type: "int", nullable: false),
                    Sex = table.Column<short>(type: "smallint", nullable: false),
                    doTutorial = table.Column<short>(type: "smallint", nullable: false),
                    NomeCompleto = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    BirthDay = table.Column<DateTime>(type: "datetime", nullable: true),
                    UserName = table.Column<string>(type: "varchar(23)", unicode: false, maxLength: 23, nullable: true, defaultValueSql: "(NULL)"),
                    UserIp = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true, defaultValueSql: "(NULL)"),
                    ServerID = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true, defaultValueSql: "(NULL)"),
                    game_server_id = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true, defaultValueSql: "(NULL)"),
                    LastLeaveTime = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(NULL)"),
                    LogonCount = table.Column<long>(type: "bigint", nullable: false),
                    BlockRegDate = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(NULL)"),
                    School = table.Column<int>(type: "int", nullable: false),
                    capability = table.Column<int>(type: "int", nullable: false),
                    Event = table.Column<short>(type: "smallint", nullable: false),
                    MannerFlag = table.Column<short>(type: "smallint", nullable: false),
                    Event1 = table.Column<short>(type: "smallint", nullable: false),
                    Event2 = table.Column<int>(type: "int", nullable: false),
                    domainid = table.Column<int>(type: "int", nullable: false),
                    ChannelFlag = table.Column<short>(type: "smallint", nullable: false),
                    change_nick = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(NULL)"),
                    Question = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Answer = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    MacAddress = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    donation_private = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    has_claimed_active_gift = table.Column<bool>(type: "tinyint(1)", nullable: true, defaultValue: false),
                    claimed_returner_bonus = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    profile_image = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    password_reset_token = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    password_reset_expires = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_UID", x => x.UID);
                    table.UniqueConstraint("AK_account_ID", x => x.ID);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "achievement_quest",
                schema: "pangya",
                columns: table => new
                {
                    IDX = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UID = table.Column<int>(type: "int", nullable: false),
                    ID_ACHIEVEMENT = table.Column<int>(type: "int", nullable: false),
                    TypeID_ACHIEVE = table.Column<int>(type: "int", nullable: false),
                    Count_ID = table.Column<int>(type: "int", nullable: false),
                    Data_Sec = table.Column<int>(type: "int", nullable: false),
                    Objetivo_Quest = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_achievement_quest_IDX", x => x.IDX);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "achievement_tipo",
                schema: "pangya",
                columns: table => new
                {
                    ID_ACHIEVEMENT = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UID = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true, defaultValueSql: "(NULL)"),
                    TypeID = table.Column<int>(type: "int", nullable: false),
                    TIPO = table.Column<short>(type: "smallint", nullable: false),
                    Option = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_achievement_tipo_ID_ACHIEVEMENT", x => x.ID_ACHIEVEMENT);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "achievements",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    nome = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false, defaultValue: ""),
                    tipo = table.Column<short>(type: "smallint", nullable: false),
                    option = table.Column<short>(type: "smallint", nullable: false),
                    quest_typeid = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_achievements_index", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "authkey_game",
                schema: "pangya",
                columns: table => new
                {
                    UID = table.Column<int>(type: "int", nullable: false),
                    AuthKey = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: true, defaultValue: ""),
                    ServerID = table.Column<int>(type: "int", nullable: false),
                    valid = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "authkey_login",
                schema: "pangya",
                columns: table => new
                {
                    UID = table.Column<int>(type: "int", nullable: false),
                    AuthKey = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false, defaultValue: ""),
                    valid = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "char_equip",
                schema: "pangya",
                columns: table => new
                {
                    UID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Character = table.Column<int>(type: "int", nullable: false),
                    part1 = table.Column<int>(type: "int", nullable: false),
                    part2 = table.Column<int>(type: "int", nullable: false),
                    part3 = table.Column<int>(type: "int", nullable: false),
                    part4 = table.Column<int>(type: "int", nullable: false),
                    part5 = table.Column<int>(type: "int", nullable: false),
                    part6 = table.Column<int>(type: "int", nullable: false),
                    part7 = table.Column<int>(type: "int", nullable: false),
                    part8 = table.Column<int>(type: "int", nullable: false),
                    part9 = table.Column<int>(type: "int", nullable: false),
                    part10 = table.Column<int>(type: "int", nullable: false),
                    part11 = table.Column<int>(type: "int", nullable: false),
                    part12 = table.Column<int>(type: "int", nullable: false),
                    part13 = table.Column<int>(type: "int", nullable: false),
                    part14 = table.Column<int>(type: "int", nullable: false),
                    part15 = table.Column<int>(type: "int", nullable: false),
                    part16 = table.Column<int>(type: "int", nullable: false),
                    part17 = table.Column<int>(type: "int", nullable: false),
                    part18 = table.Column<int>(type: "int", nullable: false),
                    part19 = table.Column<int>(type: "int", nullable: false),
                    part20 = table.Column<int>(type: "int", nullable: false),
                    part21 = table.Column<int>(type: "int", nullable: false),
                    part22 = table.Column<int>(type: "int", nullable: false),
                    part23 = table.Column<int>(type: "int", nullable: false),
                    part24 = table.Column<int>(type: "int", nullable: false),
                    auxpart1 = table.Column<int>(type: "int", nullable: false),
                    auxpart2 = table.Column<int>(type: "int", nullable: false),
                    auxpart3 = table.Column<int>(type: "int", nullable: false),
                    auxpart4 = table.Column<int>(type: "int", nullable: false),
                    auxpart5 = table.Column<int>(type: "int", nullable: false),
                    PCL0 = table.Column<short>(type: "smallint", nullable: false),
                    PCL1 = table.Column<short>(type: "smallint", nullable: false),
                    PCL2 = table.Column<short>(type: "smallint", nullable: false),
                    PCL3 = table.Column<short>(type: "smallint", nullable: false),
                    PCL4 = table.Column<short>(type: "smallint", nullable: false),
                    pucharge = table.Column<short>(type: "smallint", nullable: false),
                    isValid = table.Column<short>(type: "smallint", nullable: false),
                    default_hair = table.Column<int>(type: "int", nullable: false),
                    default_shirts = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_char_equip_UID", x => x.UID);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "contas_beta",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    uid = table.Column<int>(type: "int", nullable: false),
                    NomeCompleto = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, defaultValue: ""),
                    Birthday = table.Column<DateTime>(type: "datetime", nullable: true),
                    Email = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, defaultValue: ""),
                    Sexo = table.Column<short>(type: "smallint", nullable: false),
                    Pergunta = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, defaultValue: ""),
                    Resposta = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true),
                    LoginID = table.Column<string>(type: "varchar(25)", unicode: false, maxLength: 25, nullable: false),
                    Senha = table.Column<string>(type: "varchar(33)", unicode: false, maxLength: 33, nullable: false),
                    key_uniq = table.Column<Guid>(type: "char(36)", nullable: false, defaultValueSql: "(newid())"),
                    finish_reg = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    date_reg = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(getdate())"),
                    ip_register = table.Column<string>(type: "char(20)", fixedLength: true, maxLength: 20, nullable: false, defaultValue: ""),
                    codigo = table.Column<string>(type: "char(13)", fixedLength: true, maxLength: 13, nullable: true),
                    referrer_code = table.Column<string>(type: "char(25)", fixedLength: true, maxLength: 25, nullable: true),
                    status_referal = table.Column<string>(type: "char(10)", fixedLength: true, maxLength: 10, nullable: true),
                    profile_image = table.Column<string>(type: "varchar(300)", unicode: false, maxLength: 300, nullable: true),
                    new_email_pending = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    email_change_key = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    recovery_expires = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contas_beta_index", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "count_item",
                schema: "pangya",
                columns: table => new
                {
                    Count_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UID = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true, defaultValueSql: "(NULL)"),
                    ID_ACHIEVEMENT = table.Column<int>(type: "int", nullable: false),
                    TypeID = table.Column<int>(type: "int", nullable: false),
                    Count_Num_Item = table.Column<long>(type: "bigint", nullable: false),
                    Data_Sec = table.Column<int>(type: "int", nullable: false),
                    TIPO = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_count_item_Count_ID", x => x.Count_ID);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "counter_items",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    nome = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_counter_items_index", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "indication_status",
                schema: "pangya",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    indicated_uid = table.Column<int>(type: "int", nullable: false),
                    referrer_uid = table.Column<int>(type: "int", nullable: false),
                    level_required = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    current_level = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__indicati__3213E83FC9F82001", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mania_cookies",
                schema: "pangya",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    CP_Description = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    CP_Value = table.Column<int>(type: "int", nullable: true),
                    CP_Price = table.Column<decimal>(type: "decimal(10,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__mania_co__3214EC2761576DEC", x => x.ID);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_1st_anniversary",
                schema: "pangya",
                columns: table => new
                {
                    EVENT_DONE = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    ALL_PLAYER_APT = table.Column<long>(type: "bigint", nullable: false),
                    ALL_PLAYER_WIN = table.Column<long>(type: "bigint", nullable: false),
                    REG_DATE = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_1st_aniversary", x => x.EVENT_DONE);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_1st_anniversary_player_win_cp",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UID = table.Column<int>(type: "int", nullable: false),
                    LOGIN_DAYS = table.Column<int>(type: "int", nullable: false),
                    COOKIE_POINT = table.Column<long>(type: "bigint", nullable: false),
                    REG_DATE = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_1st_aniversary_player_win_cp", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_achievement",
                schema: "pangya",
                columns: table => new
                {
                    ID_ACHIEVEMENT = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UID = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    TypeID = table.Column<int>(type: "int", nullable: false),
                    active = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    status = table.Column<int>(type: "int", nullable: false, defaultValue: 1, comment: "1 em agurado, 2 excluido, 3 ativo, 4 concluido")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_achievement", x => x.ID_ACHIEVEMENT);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_approach_missions",
                schema: "pangya",
                columns: table => new
                {
                    numero = table.Column<long>(type: "bigint", nullable: false),
                    tipo = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    reward_tipo = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    box = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    flag = table.Column<int>(type: "int", nullable: false),
                    active = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_approach_missions_numero", x => x.numero);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_assistente",
                schema: "pangya",
                columns: table => new
                {
                    UID = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Assist = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_assistente_UID", x => x.UID);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_attendance_reward",
                schema: "pangya",
                columns: table => new
                {
                    UID = table.Column<int>(type: "int", nullable: false),
                    counter = table.Column<int>(type: "int", nullable: false),
                    item_typeid_now = table.Column<int>(type: "int", nullable: false),
                    item_qntd_now = table.Column<int>(type: "int", nullable: false),
                    item_typeid_after = table.Column<int>(type: "int", nullable: false),
                    item_qntd_after = table.Column<int>(type: "int", nullable: false),
                    last_login = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(NULL)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_attendance_reward_UID", x => x.UID);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_attendance_table_item_reward",
                schema: "pangya",
                columns: table => new
                {
                    idx = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    nome = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true, defaultValueSql: "(NULL)"),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    quantidade = table.Column<int>(type: "int", nullable: false),
                    tipo = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_attendance_table_item_reward_idx", x => x.idx);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_auth_key",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Server_UID = table.Column<int>(type: "int", nullable: true),
                    key = table.Column<string>(type: "char(16)", fixedLength: true, maxLength: 16, nullable: true),
                    valid = table.Column<byte>(type: "tinyint unsigned", nullable: true, defaultValue: (byte)1)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_bot_gm_event_reward",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    qntd = table.Column<int>(type: "int", nullable: false),
                    qntd_time = table.Column<int>(type: "int", nullable: false),
                    rate = table.Column<int>(type: "int", nullable: false),
                    valid = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)1),
                    reg_date = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_bot_gm_event_reward", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_bot_gm_event_time",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    inicio_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    fim_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    channel_id = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    valid = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    reg_date = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_bot_gm_event_time", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_caddie_information",
                schema: "pangya",
                columns: table => new
                {
                    item_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UID = table.Column<int>(type: "int", nullable: false),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    parts_typeid = table.Column<int>(type: "int", nullable: false),
                    gift_flag = table.Column<short>(type: "smallint", nullable: false),
                    cLevel = table.Column<short>(type: "smallint", nullable: false),
                    Exp = table.Column<int>(type: "int", nullable: false),
                    RegDate = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    Period = table.Column<short>(type: "smallint", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(NULL)"),
                    RentFlag = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    Purchase = table.Column<short>(type: "smallint", nullable: false),
                    parts_EndDate = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(NULL)"),
                    CheckEnd = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    Valid = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_caddie_information_item_id", x => x.item_id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_card",
                schema: "pangya",
                columns: table => new
                {
                    card_itemid = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UID = table.Column<int>(type: "int", nullable: false),
                    card_typeid = table.Column<int>(type: "int", nullable: false),
                    QNTD = table.Column<int>(type: "int", nullable: true, defaultValueSql: "(NULL)"),
                    GET_DT = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(NULL)"),
                    USE_DT = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(NULL)"),
                    END_DT = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(NULL)"),
                    Slot = table.Column<int>(type: "int", nullable: false),
                    Efeito = table.Column<int>(type: "int", nullable: false),
                    Efeito_Qntd = table.Column<int>(type: "int", nullable: false),
                    card_type = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    USE_YN = table.Column<string>(type: "char(1)", unicode: false, fixedLength: true, maxLength: 1, nullable: true, defaultValueSql: "(NULL)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_card_card_itemid", x => x.card_itemid);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_card_equip",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UID = table.Column<int>(type: "int", nullable: false),
                    parts_id = table.Column<int>(type: "int", nullable: false),
                    parts_typeid = table.Column<int>(type: "int", nullable: false),
                    card_typeid = table.Column<int>(type: "int", nullable: false),
                    Efeito = table.Column<int>(type: "int", nullable: false),
                    Efeito_Qntd = table.Column<int>(type: "int", nullable: false),
                    Slot = table.Column<int>(type: "int", nullable: false),
                    USE_DT = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(NULL)"),
                    END_DT = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(NULL)"),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    USE_YN = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    date = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_change_email_log",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    uid = table.Column<int>(type: "int", nullable: false),
                    email_old = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, defaultValue: ""),
                    email_new = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, defaultValue: ""),
                    change_time = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_change_nickname_log",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    uid = table.Column<int>(type: "int", nullable: false),
                    nickname = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, defaultValue: ""),
                    change_time = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_change_pwd_log",
                schema: "pangya",
                columns: table => new
                {
                    uid = table.Column<int>(type: "int", nullable: false),
                    last_change = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())"),
                    change_date = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())"),
                    count = table.Column<int>(type: "int", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_character_information",
                schema: "pangya",
                columns: table => new
                {
                    item_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    UID = table.Column<int>(type: "int", nullable: false),
                    parts_1 = table.Column<int>(type: "int", nullable: false),
                    parts_2 = table.Column<int>(type: "int", nullable: false),
                    parts_3 = table.Column<int>(type: "int", nullable: false),
                    parts_4 = table.Column<int>(type: "int", nullable: false),
                    parts_5 = table.Column<int>(type: "int", nullable: false),
                    parts_6 = table.Column<int>(type: "int", nullable: false),
                    parts_7 = table.Column<int>(type: "int", nullable: false),
                    parts_8 = table.Column<int>(type: "int", nullable: false),
                    parts_9 = table.Column<int>(type: "int", nullable: false),
                    parts_10 = table.Column<int>(type: "int", nullable: false),
                    parts_11 = table.Column<int>(type: "int", nullable: false),
                    parts_12 = table.Column<int>(type: "int", nullable: false),
                    parts_13 = table.Column<int>(type: "int", nullable: false),
                    parts_14 = table.Column<int>(type: "int", nullable: false),
                    parts_15 = table.Column<int>(type: "int", nullable: false),
                    parts_16 = table.Column<int>(type: "int", nullable: false),
                    parts_17 = table.Column<int>(type: "int", nullable: false),
                    parts_18 = table.Column<int>(type: "int", nullable: false),
                    parts_19 = table.Column<int>(type: "int", nullable: false),
                    parts_20 = table.Column<int>(type: "int", nullable: false),
                    parts_21 = table.Column<int>(type: "int", nullable: false),
                    parts_22 = table.Column<int>(type: "int", nullable: false),
                    parts_23 = table.Column<int>(type: "int", nullable: false),
                    parts_24 = table.Column<int>(type: "int", nullable: false),
                    default_hair = table.Column<short>(type: "smallint", nullable: false),
                    default_shirts = table.Column<short>(type: "smallint", nullable: false),
                    gift_flag = table.Column<short>(type: "smallint", nullable: false),
                    PCL0 = table.Column<short>(type: "smallint", nullable: false),
                    PCL1 = table.Column<short>(type: "smallint", nullable: false),
                    PCL2 = table.Column<short>(type: "smallint", nullable: false),
                    PCL3 = table.Column<short>(type: "smallint", nullable: false),
                    PCL4 = table.Column<short>(type: "smallint", nullable: false),
                    Purchase = table.Column<short>(type: "smallint", nullable: false),
                    auxparts_1 = table.Column<int>(type: "int", nullable: false),
                    auxparts_2 = table.Column<int>(type: "int", nullable: false),
                    auxparts_3 = table.Column<int>(type: "int", nullable: false),
                    auxparts_4 = table.Column<int>(type: "int", nullable: false),
                    auxparts_5 = table.Column<int>(type: "int", nullable: false),
                    CutIn_1 = table.Column<int>(type: "int", nullable: false),
                    CutIn_2 = table.Column<int>(type: "int", nullable: false),
                    CutIn_3 = table.Column<int>(type: "int", nullable: false),
                    CutIn_4 = table.Column<int>(type: "int", nullable: false),
                    Mastery = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya.pangya_character_information", x => x.item_id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_character_part_padrao",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    char_typeid = table.Column<int>(type: "int", nullable: false),
                    parts_1 = table.Column<int>(type: "int", nullable: false),
                    parts_2 = table.Column<int>(type: "int", nullable: false),
                    parts_3 = table.Column<int>(type: "int", nullable: false),
                    parts_4 = table.Column<int>(type: "int", nullable: false),
                    parts_5 = table.Column<int>(type: "int", nullable: false),
                    parts_6 = table.Column<int>(type: "int", nullable: false),
                    parts_7 = table.Column<int>(type: "int", nullable: false),
                    parts_8 = table.Column<int>(type: "int", nullable: false),
                    parts_9 = table.Column<int>(type: "int", nullable: false),
                    parts_10 = table.Column<int>(type: "int", nullable: false),
                    parts_11 = table.Column<int>(type: "int", nullable: false),
                    parts_12 = table.Column<int>(type: "int", nullable: false),
                    parts_13 = table.Column<int>(type: "int", nullable: false),
                    parts_14 = table.Column<int>(type: "int", nullable: false),
                    parts_15 = table.Column<int>(type: "int", nullable: false),
                    parts_16 = table.Column<int>(type: "int", nullable: false),
                    parts_17 = table.Column<int>(type: "int", nullable: false),
                    parts_18 = table.Column<int>(type: "int", nullable: false),
                    parts_19 = table.Column<int>(type: "int", nullable: false),
                    parts_20 = table.Column<int>(type: "int", nullable: false),
                    parts_21 = table.Column<int>(type: "int", nullable: false),
                    parts_22 = table.Column<int>(type: "int", nullable: false),
                    parts_23 = table.Column<int>(type: "int", nullable: false),
                    parts_24 = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_character_part_padrao_index", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_clubset_enchant",
                schema: "pangya",
                columns: table => new
                {
                    uid = table.Column<int>(type: "int", nullable: false),
                    item_id = table.Column<int>(type: "int", nullable: false),
                    pang = table.Column<long>(type: "bigint", nullable: false),
                    c0 = table.Column<short>(type: "smallint", nullable: false),
                    c1 = table.Column<short>(type: "smallint", nullable: false),
                    c2 = table.Column<short>(type: "smallint", nullable: false),
                    c3 = table.Column<short>(type: "smallint", nullable: false),
                    c4 = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_coin_cube_info",
                schema: "pangya",
                columns: table => new
                {
                    course_id = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    active = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)1),
                    update_date = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_coin_cube_location",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    course = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    hole = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    tipo = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    tipo_location = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    rate = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    x = table.Column<double>(type: "double", nullable: false, defaultValueSql: "((0.0))"),
                    y = table.Column<double>(type: "double", nullable: false, defaultValueSql: "((0.0))"),
                    z = table.Column<double>(type: "double", nullable: false, defaultValueSql: "((0.0))"),
                    reg_date = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_coin_cube_copy1_copy1", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_comet_refill",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    min = table.Column<short>(type: "smallint", nullable: false),
                    max = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_command",
                schema: "pangya",
                columns: table => new
                {
                    idx = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    command_id = table.Column<int>(type: "int", nullable: false),
                    arg1 = table.Column<int>(type: "int", nullable: false),
                    arg2 = table.Column<int>(type: "int", nullable: false),
                    arg3 = table.Column<int>(type: "int", nullable: false),
                    arg4 = table.Column<int>(type: "int", nullable: false),
                    arg5 = table.Column<int>(type: "int", nullable: false),
                    target = table.Column<int>(type: "int", nullable: false),
                    regDate = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    reserveDate = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(NULL)"),
                    flag = table.Column<short>(type: "smallint", nullable: false),
                    valid = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_command_idx", x => x.idx);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_command_gm_log",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    command_type = table.Column<int>(type: "int", nullable: false),
                    gm_uid = table.Column<int>(type: "int", nullable: false),
                    gm_nick = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: false),
                    capability = table.Column<long>(type: "bigint", nullable: false),
                    nick_gift = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: false),
                    uid_gift = table.Column<int>(type: "int", nullable: false),
                    item_typeid = table.Column<int>(type: "int", nullable: false),
                    item_qntd = table.Column<int>(type: "int", nullable: false),
                    reg_date = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_config",
                schema: "pangya",
                columns: table => new
                {
                    UID = table.Column<int>(type: "int", nullable: false),
                    GrandZodiacEventTime = table.Column<short>(type: "smallint", nullable: false),
                    ScratchyPorPointRate = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)100),
                    PapelShopRareItemRate = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)100),
                    PapelShopCookieItemRate = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)100),
                    TreasureRate = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)100),
                    PangRate = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)100),
                    ExpRate = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)100),
                    ClubMasteryRate = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)100),
                    ChuvaRate = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)100),
                    MemorialShopRate = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)100),
                    AngelEvent = table.Column<short>(type: "smallint", nullable: false),
                    GrandPrixEvent = table.Column<short>(type: "smallint", nullable: false),
                    GoldenTimeEvent = table.Column<short>(type: "smallint", nullable: false),
                    LoginRewardEvent = table.Column<short>(type: "smallint", nullable: false),
                    BotGMEvent = table.Column<short>(type: "smallint", nullable: false),
                    SmartCalculator = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_cookie_point_item_log",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    cp_id_log = table.Column<long>(type: "bigint", nullable: true, defaultValue: 0L),
                    typeid = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    qnty = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    price = table.Column<long>(type: "bigint", nullable: true, defaultValue: 0L)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_cookie_point_log",
                schema: "pangya",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    uid = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    type = table.Column<byte>(type: "tinyint unsigned", nullable: true, defaultValue: (byte)0),
                    mail_id = table.Column<int>(type: "int", nullable: true, defaultValue: -1),
                    cookie = table.Column<long>(type: "bigint", nullable: true, defaultValue: 0L),
                    item_qnty = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    reg_date = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_counter_item",
                schema: "pangya",
                columns: table => new
                {
                    Count_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UID = table.Column<int>(type: "int", nullable: false),
                    TypeID = table.Column<int>(type: "int", nullable: false),
                    active = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Count_Num_Item = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya.counter_item", x => x.Count_ID);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_coupon_desconto",
                schema: "pangya",
                columns: table => new
                {
                    Nome = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    valor = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_course_cube_coin_temporada",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    course = table.Column<int>(type: "int", nullable: false),
                    active = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_course_cube_coin_temporada_index", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_course_reward_treasure",
                schema: "pangya",
                columns: table => new
                {
                    COURSE = table.Column<short>(type: "smallint", nullable: false),
                    PANGREWARD = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_course_reward_treasure_COURSE", x => x.COURSE);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_cube_coin_location",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    tipo = table.Column<int>(type: "int", nullable: false),
                    config2 = table.Column<int>(type: "int", nullable: false),
                    course = table.Column<short>(type: "smallint", nullable: false),
                    hole = table.Column<short>(type: "smallint", nullable: false),
                    x = table.Column<float>(type: "float", nullable: false),
                    y = table.Column<float>(type: "float", nullable: false),
                    z = table.Column<float>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_cube_coin_location_index", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_daily_quest",
                schema: "pangya",
                columns: table => new
                {
                    achieve_quest_1 = table.Column<int>(type: "int", nullable: false),
                    achieve_quest_2 = table.Column<int>(type: "int", nullable: false),
                    achieve_quest_3 = table.Column<int>(type: "int", nullable: false),
                    Reg_Date = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_daily_quest_player",
                schema: "pangya",
                columns: table => new
                {
                    uid = table.Column<long>(type: "bigint", nullable: false),
                    last_quest_accept = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(getdate())"),
                    today_quest = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_daily_quest_player_uid", x => x.uid);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_dolfini_locker",
                schema: "pangya",
                columns: table => new
                {
                    uid = table.Column<int>(type: "int", nullable: false),
                    senha = table.Column<string>(type: "varchar(5)", unicode: false, maxLength: 5, nullable: true, defaultValueSql: "(NULL)"),
                    pang = table.Column<long>(type: "bigint", nullable: false),
                    locker = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_dolfini_locker_uid", x => x.uid);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_dolfini_locker_item",
                schema: "pangya",
                columns: table => new
                {
                    idx = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    uid = table.Column<int>(type: "int", nullable: false),
                    item_id = table.Column<int>(type: "int", nullable: false),
                    flag = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_dolfini_locker_item_idx", x => x.idx);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_donation_epin",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    donation_id = table.Column<long>(type: "bigint", nullable: false),
                    uid = table.Column<int>(type: "int", nullable: false),
                    epin = table.Column<Guid>(type: "char(36)", nullable: false, defaultValueSql: "(newid())"),
                    qntd = table.Column<long>(type: "bigint", nullable: false),
                    retrive_uid = table.Column<int>(type: "int", nullable: true),
                    valid = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_donation_epin", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_donation_item_log",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    donation_id = table.Column<long>(type: "bigint", nullable: false),
                    item_typeid = table.Column<int>(type: "int", nullable: false),
                    item_qntd = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_donation_log",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ADM_uid = table.Column<int>(type: "int", nullable: false, comment: "quem registrou a do doação para o usuário"),
                    uid = table.Column<int>(type: "int", nullable: false),
                    plataforma = table.Column<byte>(type: "tinyint unsigned", nullable: false, comment: "0 nenhum, 1 Paypal, 2 PagSeguro"),
                    cash = table.Column<int>(type: "int", nullable: false),
                    cookie_point = table.Column<int>(type: "int", nullable: false),
                    email = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    obs = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    red_date = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_donation_new",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    uid = table.Column<int>(type: "int", nullable: false, defaultValue: -1),
                    plataforma = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    email = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    update = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    type = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    status = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    reference = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    gross_amount = table.Column<double>(type: "double", nullable: false),
                    net_amount = table.Column<double>(type: "double", nullable: false),
                    escrow = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    epin_id = table.Column<long>(type: "bigint", nullable: false, defaultValue: -1L),
                    reg_date = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_donation_new", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_event_site",
                schema: "pangya",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    NOME_EVENTO = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    STATUS = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    TIPO_EVENTO = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    DATA_INICIAL = table.Column<DateTime>(type: "datetime", nullable: false),
                    DATA_FIM = table.Column<DateTime>(type: "datetime", nullable: false),
                    DATA_REGISTRO = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    ITENS = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__pangya_e__3214EC27E5986E28", x => x.ID);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_exception_log",
                schema: "pangya",
                columns: table => new
                {
                    ExceptionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UID = table.Column<int>(type: "int", nullable: true),
                    Username = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    ExceptionMessage = table.Column<string>(type: "varchar(2000)", unicode: false, maxLength: 2000, nullable: true),
                    Server = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    CreateDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_fast_pass_event",
                schema: "pangya",
                columns: table => new
                {
                    UID = table.Column<int>(type: "int", nullable: false),
                    HOLES_INIT = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    HOLES_COUNTER = table.Column<long>(type: "bigint", nullable: false),
                    END_DATE = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    REG_DATE = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_friend_list",
                schema: "pangya",
                columns: table => new
                {
                    uid = table.Column<int>(type: "int", nullable: false),
                    uid_friend = table.Column<int>(type: "int", nullable: false),
                    apelido = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: false, defaultValue: "Friend"),
                    unknown1 = table.Column<int>(type: "int", nullable: false, defaultValue: -1),
                    unknown2 = table.Column<int>(type: "int", nullable: false),
                    unknown3 = table.Column<int>(type: "int", nullable: false, defaultValue: -1),
                    unknown4 = table.Column<int>(type: "int", nullable: false),
                    unknown5 = table.Column<int>(type: "int", nullable: false),
                    unknown6 = table.Column<int>(type: "int", nullable: false),
                    flag1 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)-1),
                    state_flag = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    flag5 = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_gacha",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<int>(type: "int", nullable: false),
                    townno = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    charno = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    shop = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    numero = table.Column<int>(type: "int", nullable: false),
                    coin = table.Column<int>(type: "int", nullable: false),
                    rate = table.Column<int>(type: "int", nullable: false, defaultValue: 100)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_gacha_index", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_gacha_coin",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    qntd = table.Column<int>(type: "int", nullable: false),
                    preco = table.Column<int>(type: "int", nullable: false),
                    tipo = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_gacha_coin_index", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_gacha_items",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    gacha_num = table.Column<int>(type: "int", nullable: false),
                    nome = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true, defaultValueSql: "(NULL)"),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    qntd = table.Column<int>(type: "int", nullable: false),
                    probabilidade = table.Column<int>(type: "int", nullable: false),
                    tipo = table.Column<short>(type: "smallint", nullable: false),
                    premio = table.Column<short>(type: "smallint", nullable: false),
                    secret = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_gacha_items_index", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_gacha_jp_all_item_list",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    char_type = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    reg_date = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_gacha_jp_all_item_list", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_gacha_jp_item_list",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    active = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)1),
                    gacha_num = table.Column<int>(type: "int", nullable: false),
                    typeid_1 = table.Column<int>(type: "int", nullable: false),
                    typeid_2 = table.Column<int>(type: "int", nullable: true),
                    qnty_1 = table.Column<long>(type: "bigint", nullable: false),
                    qnty_2 = table.Column<long>(type: "bigint", nullable: true),
                    rarity_type = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    reg_date = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya.pangya_gacha_jp_item_list", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_gacha_jp_player_win",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    uid = table.Column<int>(type: "int", nullable: false),
                    gacha_num = table.Column<int>(type: "int", nullable: false),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    qnty = table.Column<long>(type: "bigint", nullable: false),
                    rarity_type = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    send_mail = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    valid = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    reg_date = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya.pangya_gacha_jp_player_win", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_gacha_jp_rate",
                schema: "pangya",
                columns: table => new
                {
                    gacha_num = table.Column<int>(type: "int", nullable: false),
                    rate_rare = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    rate_normal = table.Column<int>(type: "int", nullable: false, defaultValue: 100)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_gacha_user_key",
                schema: "pangya",
                columns: table => new
                {
                    uid = table.Column<int>(type: "int", nullable: false),
                    coin_count_entrou = table.Column<int>(type: "int", nullable: false),
                    att_flag = table.Column<short>(type: "smallint", nullable: false),
                    key = table.Column<string>(type: "varchar(22)", unicode: false, maxLength: 22, nullable: false),
                    date_key_generation = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_gacha_user_key_uid", x => x.uid);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_gacha_user_won",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    gacha_num = table.Column<int>(type: "int", nullable: false),
                    uid = table.Column<int>(type: "int", nullable: false),
                    item_name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true, defaultValueSql: "(NULL)"),
                    item_typeid = table.Column<int>(type: "int", nullable: false),
                    get_date = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_gacha_user_won_index", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_gift_table",
                schema: "pangya",
                columns: table => new
                {
                    Msg_ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UID = table.Column<int>(type: "int", nullable: false),
                    Flag = table.Column<short>(type: "smallint", nullable: false),
                    fromid = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    message = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    giftdate = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    enddate = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(NULL)"),
                    Contador_Vista = table.Column<int>(type: "int", nullable: false),
                    Lida_YN = table.Column<short>(type: "smallint", nullable: false),
                    valid = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_gift_table_Msg_ID", x => x.Msg_ID);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_gm_gift_web_log",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    GM_UID = table.Column<int>(type: "int", nullable: false),
                    PLAYER_UID = table.Column<int>(type: "int", nullable: false),
                    MSG_ID = table.Column<int>(type: "int", nullable: false),
                    REG_DATE = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_golden_time_info",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    type = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    begin = table.Column<DateOnly>(type: "date", nullable: false),
                    end = table.Column<DateOnly>(type: "date", nullable: true),
                    rate = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    is_end = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    reg_date = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_golden_time_info", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_golden_time_item",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    golden_time_id = table.Column<long>(type: "bigint", nullable: false),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    qntd = table.Column<int>(type: "int", nullable: false),
                    qntd_time = table.Column<int>(type: "int", nullable: false),
                    rate = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    reg_date = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_golden_time_item", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_golden_time_round",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    golden_time_id = table.Column<long>(type: "bigint", nullable: false),
                    time = table.Column<TimeOnly>(type: "time", nullable: false),
                    reg_date = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_golden_time_round", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_grand_zodiac_pontos",
                schema: "pangya",
                columns: table => new
                {
                    uid = table.Column<int>(type: "int", nullable: false),
                    pontos = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_grand_zodiac_pontos_uid", x => x.uid);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_grand_zodiac_times",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    inicio_time = table.Column<TimeOnly>(type: "time", nullable: false, defaultValueSql: "(getdate())"),
                    fim_time = table.Column<TimeOnly>(type: "time", nullable: false, defaultValueSql: "(getdate())"),
                    type = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    valid = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_grand_zodiac_times_index", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_grandprix_clear",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    uid = table.Column<int>(type: "int", nullable: false),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    flag = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_grandprix_clear_index", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_grandprix_event_config",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    flag = table.Column<int>(type: "int", nullable: false),
                    Active = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_grandprix_event_config_index", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_guild",
                schema: "pangya",
                columns: table => new
                {
                    GUILD_UID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    GUILD_ID = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false, defaultValue: ""),
                    GUILD_NAME = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    GUILD_LEADER = table.Column<int>(type: "int", nullable: false),
                    GUILD_SUB_MASTER = table.Column<int>(type: "int", nullable: false),
                    GUILD_CONDITION_LEVEL = table.Column<short>(type: "smallint", nullable: false),
                    GUILD_STATE = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    GUILD_FLAG = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    GUILD_PERMITION_JOIN = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)1),
                    GUILD_PANG = table.Column<long>(type: "bigint", nullable: false),
                    GUILD_POINT = table.Column<long>(type: "bigint", nullable: false),
                    GUILD_WIN = table.Column<int>(type: "int", nullable: false),
                    GUILD_LOSE = table.Column<int>(type: "int", nullable: false),
                    GUILD_DRAW = table.Column<int>(type: "int", nullable: false),
                    GUILD_MARK_IMG = table.Column<string>(type: "varchar(12)", unicode: false, maxLength: 12, nullable: false, defaultValue: "guildmark"),
                    GUILD_MARK_IMG_IDX = table.Column<int>(type: "int", nullable: false),
                    GUILD_NEW_MARK_IDX = table.Column<int>(type: "int", nullable: false),
                    GUILD_INTRO_IMG = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    GUILD_NOTICE = table.Column<string>(type: "varchar(110)", unicode: false, maxLength: 110, nullable: false, defaultValue: ""),
                    GUILD_INFO = table.Column<string>(type: "varchar(110)", unicode: false, maxLength: 110, nullable: false, defaultValue: ""),
                    GUILD_REG_DATE = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    GUILD_ACCEPT_DATE = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    GUILD_CLOSURE_DATE = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_guild_atividade_player",
                schema: "pangya",
                columns: table => new
                {
                    IDX = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UID = table.Column<int>(type: "int", nullable: false),
                    GUILD_UID = table.Column<int>(type: "int", nullable: false),
                    FLAG = table.Column<int>(type: "int", nullable: false),
                    REG_DATE = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_guild_bbs",
                schema: "pangya",
                columns: table => new
                {
                    SEQ = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    OWNER_UID = table.Column<int>(type: "int", nullable: false),
                    TITLE = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    TEXT = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    TYPE = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    STATE = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)1),
                    VIEWS = table.Column<long>(type: "bigint", nullable: false),
                    REG_DATE = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_guild_bbs_res",
                schema: "pangya",
                columns: table => new
                {
                    SEQ = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    BBS_SEQ = table.Column<long>(type: "bigint", nullable: false),
                    OWNER_UID = table.Column<int>(type: "int", nullable: false),
                    TEXT = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    STATE = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)1),
                    REG_DATE = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_guild_intro_img_log",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    intro_img = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    reg_date = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_guild_mark_log",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    mark_idx = table.Column<int>(type: "int", nullable: false),
                    reg_date = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_guild_match",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    guild_1_uid = table.Column<int>(type: "int", nullable: false),
                    guild_2_uid = table.Column<int>(type: "int", nullable: false),
                    guild_1_point = table.Column<int>(type: "int", nullable: false),
                    guild_2_point = table.Column<int>(type: "int", nullable: false),
                    guild_1_pang = table.Column<int>(type: "int", nullable: false),
                    guild_2_pang = table.Column<int>(type: "int", nullable: false),
                    reg_date = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_guild_member",
                schema: "pangya",
                columns: table => new
                {
                    GUILD_UID = table.Column<int>(type: "int", nullable: false),
                    MEMBER_UID = table.Column<int>(type: "int", nullable: false),
                    MEMBER_MSG = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true, defaultValueSql: "(NULL)"),
                    GUILD_PANG = table.Column<int>(type: "int", nullable: false),
                    GUILD_POINT = table.Column<int>(type: "int", nullable: false),
                    MEMBER_FLAG = table.Column<int>(type: "int", nullable: false),
                    MEMBER_STATE_FLAG = table.Column<int>(type: "int", nullable: false, defaultValue: 3),
                    REG_DATE = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_guild_notice",
                schema: "pangya",
                columns: table => new
                {
                    SEQ = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    GUILD_UID = table.Column<int>(type: "int", nullable: false),
                    OWNER_UID = table.Column<int>(type: "int", nullable: false),
                    TITLE = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    TEXT = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    STATE = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)1),
                    VIEWS = table.Column<long>(type: "bigint", nullable: false),
                    REG_DATE = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_guild_private_bbs",
                schema: "pangya",
                columns: table => new
                {
                    SEQ = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    GUILD_UID = table.Column<int>(type: "int", nullable: false),
                    OWNER_UID = table.Column<int>(type: "int", nullable: false),
                    TITLE = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    TEXT = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    VIEWS = table.Column<long>(type: "bigint", nullable: false),
                    STATE = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)1),
                    REG_DATE = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_guild_private_bbs_res",
                schema: "pangya",
                columns: table => new
                {
                    SEQ = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    GUILD_BBS_SEQ = table.Column<long>(type: "bigint", nullable: false),
                    OWNER_UID = table.Column<int>(type: "int", nullable: false),
                    TEXT = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    STATE = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)1),
                    REG_DATE = table.Column<DateTime>(type: "datetime(6)", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_guild_ranking",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    GUILD_UID = table.Column<int>(type: "int", nullable: false),
                    RANK = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    LAST_RANK = table.Column<int>(type: "int", nullable: false),
                    REG_DATE = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya.pangya_guild_ranking", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_guild_update_activity",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    GUILD_UID = table.Column<int>(type: "int", nullable: false),
                    OWNER_UPDATE = table.Column<int>(type: "int", nullable: false),
                    PLAYER_UID = table.Column<int>(type: "int", nullable: false),
                    TYPE_UPDATE = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    STATE = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    REG_DATE = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_guild_update_activity", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_gz_event_2016121600_rare_win",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    uid = table.Column<int>(type: "int", nullable: false),
                    item_typeid = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    win_date = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_gz_event_2016121600_rare_win_index", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_hio_event",
                schema: "pangya",
                columns: table => new
                {
                    INDEX = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UID = table.Column<int>(type: "int", nullable: false),
                    START_HIOS = table.Column<int>(type: "int", nullable: false),
                    PROCESS_HIOS = table.Column<int>(type: "int", nullable: false),
                    STATUS = table.Column<int>(type: "int", nullable: false),
                    FINISH_DATE = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_hio_event", x => x.INDEX);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_hio_event_items",
                schema: "pangya",
                columns: table => new
                {
                    IDX = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    HIO_COUNT = table.Column<int>(type: "int", nullable: false),
                    ITEM_NAME = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    ITEM_TYPEID = table.Column<int>(type: "int", nullable: false),
                    ITEM_QNTD = table.Column<int>(type: "int", nullable: false),
                    ITEM_QNTD_TIME = table.Column<int>(type: "int", nullable: false),
                    EVENT_DESCRIPTION = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    END_EVENT = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    REG_DATE = table.Column<DateTime>(type: "datetime(6)", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_hio_event_log",
                schema: "pangya",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    uid = table.Column<int>(type: "int", nullable: false),
                    item_typeid = table.Column<int>(type: "int", nullable: false),
                    hio_count = table.Column<int>(type: "int", nullable: false),
                    received_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__pangya_h__3213E83F99883BEE", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_hole_event",
                schema: "pangya",
                columns: table => new
                {
                    INDEX = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UID = table.Column<int>(type: "int", nullable: false),
                    START_HOLES = table.Column<int>(type: "int", nullable: false),
                    PROCESS_HOLES = table.Column<int>(type: "int", nullable: false),
                    STATUS = table.Column<int>(type: "int", nullable: false),
                    FINISH_DATE = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_hole_event", x => x.INDEX);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_hole_event_config",
                schema: "pangya",
                columns: table => new
                {
                    EVENT_ID = table.Column<int>(type: "int", nullable: true),
                    START_EVENT = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    END_EVENT = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_hole_event_items",
                schema: "pangya",
                columns: table => new
                {
                    EVENT_ID = table.Column<int>(type: "int", nullable: true),
                    HOLE_COUNT = table.Column<int>(type: "int", nullable: false),
                    ITEM_NAME = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    ITEM_TYPEID = table.Column<int>(type: "int", nullable: false),
                    ITEM_QNTD = table.Column<int>(type: "int", nullable: false),
                    ITEM_QNTD_TIME = table.Column<int>(type: "int", nullable: false),
                    EVENT_DESCRIPTION = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_ip_table",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ip = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    mask = table.Column<string>(type: "varchar(18)", unicode: false, maxLength: 18, nullable: false, defaultValue: "255.255.255.255"),
                    date = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_ip_table_index", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_item_buff",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    uid = table.Column<int>(type: "int", nullable: false),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    reg_date = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())"),
                    end_date = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())"),
                    tipo = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)2),
                    percent = table.Column<int>(type: "int", nullable: false),
                    use_yn = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)1)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_item_buy_shop_log",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    uid = table.Column<int>(type: "int", nullable: false),
                    item_id = table.Column<int>(type: "int", nullable: false),
                    item_typeid = table.Column<int>(type: "int", nullable: false),
                    item_time = table.Column<int>(type: "int", nullable: false),
                    item_type = table.Column<int>(type: "int", nullable: false),
                    item_qntd = table.Column<int>(type: "int", nullable: false),
                    item_pang = table.Column<long>(type: "bigint", nullable: false),
                    item_cookie = table.Column<long>(type: "bigint", nullable: false),
                    reg_date = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_item_mail",
                schema: "pangya",
                columns: table => new
                {
                    Msg_ID = table.Column<int>(type: "int", nullable: false),
                    item_id = table.Column<int>(type: "int", nullable: false, defaultValue: -1),
                    item_typeid = table.Column<int>(type: "int", nullable: false),
                    Flag = table.Column<short>(type: "smallint", nullable: false),
                    GET_DATE = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(getdate())"),
                    Quantidade_item = table.Column<int>(type: "int", nullable: false),
                    Quantidade_Dia = table.Column<int>(type: "int", nullable: false),
                    Pang = table.Column<long>(type: "bigint", nullable: false),
                    Cookie = table.Column<long>(type: "bigint", nullable: false),
                    GM_ID = table.Column<int>(type: "int", nullable: false, defaultValue: -1),
                    Flag_Gift = table.Column<int>(type: "int", nullable: false),
                    UCC_IMG_MARK = table.Column<string>(type: "varchar(12)", unicode: false, maxLength: 12, nullable: false, defaultValue: "0"),
                    Type = table.Column<short>(type: "smallint", nullable: false),
                    valid = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_item_typelist",
                schema: "pangya",
                columns: table => new
                {
                    TYPEID = table.Column<int>(type: "int", nullable: false),
                    NAME = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true, defaultValue: "NAME ITEM"),
                    ICON = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true, defaultValue: "icon_x.png"),
                    PRICE = table.Column<int>(type: "int", nullable: true, defaultValueSql: "('0')"),
                    ISCASH = table.Column<short>(type: "smallint", nullable: false),
                    IFF_TYPE = table.Column<short>(type: "smallint", nullable: true, defaultValueSql: "('0')"),
                    TYPE = table.Column<int>(type: "int", nullable: true, defaultValueSql: "('0')"),
                    COM0 = table.Column<int>(type: "int", nullable: true, defaultValueSql: "('0')"),
                    COM1 = table.Column<int>(type: "int", nullable: true, defaultValueSql: "('0')"),
                    COM2 = table.Column<int>(type: "int", nullable: true, defaultValueSql: "('0')"),
                    COM3 = table.Column<int>(type: "int", nullable: true, defaultValueSql: "('0')"),
                    COM4 = table.Column<int>(type: "int", nullable: true, defaultValueSql: "('0')"),
                    CHAR_SERIALNO = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true, defaultValue: "0"),
                    DESC = table.Column<string>(type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: true, defaultValue: "NO HAVE DESC"),
                    TNAME = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true, defaultValue: "NO HAVE TNAME"),
                    IS_SALABLE = table.Column<short>(type: "smallint", nullable: true, defaultValueSql: "('0')"),
                    CHAR_ID = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    NAME_ITEM = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_item_warehouse",
                schema: "pangya",
                columns: table => new
                {
                    item_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UID = table.Column<int>(type: "int", nullable: false),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    valid = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    regdate = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(NULL)"),
                    Gift_flag = table.Column<short>(type: "smallint", nullable: false),
                    flag = table.Column<short>(type: "smallint", nullable: false),
                    Applytime = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(getdate())"),
                    EndDate = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(getdate())"),
                    C0 = table.Column<short>(type: "smallint", nullable: false),
                    C1 = table.Column<short>(type: "smallint", nullable: false),
                    C2 = table.Column<short>(type: "smallint", nullable: false),
                    C3 = table.Column<short>(type: "smallint", nullable: false),
                    C4 = table.Column<short>(type: "smallint", nullable: false),
                    Purchase = table.Column<short>(type: "smallint", nullable: false),
                    ItemType = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)2),
                    ClubSet_WorkShop_Flag = table.Column<short>(type: "smallint", nullable: false),
                    ClubSet_WorkShop_C0 = table.Column<short>(type: "smallint", nullable: false),
                    ClubSet_WorkShop_C1 = table.Column<short>(type: "smallint", nullable: false),
                    ClubSet_WorkShop_C2 = table.Column<short>(type: "smallint", nullable: false),
                    ClubSet_WorkShop_C3 = table.Column<short>(type: "smallint", nullable: false),
                    ClubSet_WorkShop_C4 = table.Column<short>(type: "smallint", nullable: false),
                    Mastery_Pts = table.Column<int>(type: "int", nullable: false),
                    Recovery_Pts = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Up = table.Column<int>(type: "int", nullable: false),
                    Total_Mastery_Pts = table.Column<long>(type: "bigint", nullable: false),
                    Mastery_Gasto = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_item_warehouse_item_id", x => x.item_id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_last_players_user",
                schema: "pangya",
                columns: table => new
                {
                    UID = table.Column<int>(type: "int", nullable: false),
                    SEX_0 = table.Column<int>(type: "int", nullable: true, defaultValueSql: "(NULL)"),
                    ID_0 = table.Column<string>(type: "varchar(22)", unicode: false, maxLength: 22, nullable: true, defaultValueSql: "(NULL)"),
                    NICK_0 = table.Column<string>(type: "varchar(22)", unicode: false, maxLength: 22, nullable: true, defaultValueSql: "(NULL)"),
                    UID_0 = table.Column<int>(type: "int", nullable: true, defaultValueSql: "(NULL)"),
                    SEX_1 = table.Column<int>(type: "int", nullable: true, defaultValueSql: "(NULL)"),
                    ID_1 = table.Column<string>(type: "varchar(22)", unicode: false, maxLength: 22, nullable: true, defaultValueSql: "(NULL)"),
                    NICK_1 = table.Column<string>(type: "varchar(22)", unicode: false, maxLength: 22, nullable: true, defaultValueSql: "(NULL)"),
                    UID_1 = table.Column<int>(type: "int", nullable: true, defaultValueSql: "(NULL)"),
                    SEX_2 = table.Column<int>(type: "int", nullable: true, defaultValueSql: "(NULL)"),
                    ID_2 = table.Column<string>(type: "varchar(22)", unicode: false, maxLength: 22, nullable: true, defaultValueSql: "(NULL)"),
                    NICK_2 = table.Column<string>(type: "varchar(22)", unicode: false, maxLength: 22, nullable: true, defaultValueSql: "(NULL)"),
                    UID_2 = table.Column<int>(type: "int", nullable: true, defaultValueSql: "(NULL)"),
                    SEX_3 = table.Column<int>(type: "int", nullable: true, defaultValueSql: "(NULL)"),
                    ID_3 = table.Column<string>(type: "varchar(22)", unicode: false, maxLength: 22, nullable: true, defaultValueSql: "(NULL)"),
                    NICK_3 = table.Column<string>(type: "varchar(22)", unicode: false, maxLength: 22, nullable: true, defaultValueSql: "(NULL)"),
                    UID_3 = table.Column<int>(type: "int", nullable: true, defaultValueSql: "(NULL)"),
                    SEX_4 = table.Column<int>(type: "int", nullable: true, defaultValueSql: "(NULL)"),
                    ID_4 = table.Column<string>(type: "varchar(22)", unicode: false, maxLength: 22, nullable: true, defaultValueSql: "(NULL)"),
                    NICK_4 = table.Column<string>(type: "varchar(22)", unicode: false, maxLength: 22, nullable: true, defaultValueSql: "(NULL)"),
                    UID_4 = table.Column<int>(type: "int", nullable: true, defaultValueSql: "(NULL)")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_last_up_clubset",
                schema: "pangya",
                columns: table => new
                {
                    item_id = table.Column<int>(type: "int", nullable: false),
                    UID = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<short>(type: "smallint", nullable: false),
                    item_usado = table.Column<int>(type: "int", nullable: false),
                    Quantidade = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_login_reward",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    type = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    days_to_gift = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    n_times_gift = table.Column<int>(type: "int", nullable: false),
                    item_typeid = table.Column<int>(type: "int", nullable: false),
                    item_qntd = table.Column<int>(type: "int", nullable: false),
                    item_qntd_time = table.Column<int>(type: "int", nullable: false),
                    is_end = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    end_date = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    reg_date = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_login_reward", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_login_reward_player",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    login_reward_id = table.Column<long>(type: "bigint", nullable: false),
                    uid = table.Column<int>(type: "int", nullable: false),
                    count_days = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    count_seq = table.Column<int>(type: "int", nullable: false),
                    is_clear = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    update_date = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())"),
                    reg_date = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_login_reward_player", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_lucia_attendance",
                schema: "pangya",
                columns: table => new
                {
                    UID = table.Column<int>(type: "int", nullable: false),
                    count_day = table.Column<long>(type: "bigint", nullable: false),
                    last_day_attendance = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    last_day_get_item = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    try_hacking_count = table.Column<int>(type: "int", nullable: false),
                    block_type = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    block_end_date = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_lucia_attendance", x => x.UID);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_lucia_attendance_reward_log",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UID = table.Column<int>(type: "int", nullable: false),
                    MSG_ID = table.Column<int>(type: "int", nullable: false),
                    ERROR = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    REG_DATE = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya.pangya_lucia_attendance_log", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_mac_table",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    mac = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    date = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_maniadonation_log",
                schema: "pangya",
                columns: table => new
                {
                    ADM_UID = table.Column<int>(type: "int", nullable: false),
                    UID = table.Column<int>(type: "int", nullable: false),
                    cash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    cookie_point = table.Column<int>(type: "int", nullable: false),
                    pangs = table.Column<int>(type: "int", nullable: false),
                    ITEM_TYPEID_1 = table.Column<int>(type: "int", nullable: true),
                    ITEM_TYPEID_2 = table.Column<int>(type: "int", nullable: true),
                    ITEM_TYPEID_3 = table.Column<int>(type: "int", nullable: true),
                    ITEM_TYPEID_4 = table.Column<int>(type: "int", nullable: true),
                    ITEM_TYPEID_5 = table.Column<int>(type: "int", nullable: true),
                    ITEM_QNTD_1 = table.Column<int>(type: "int", nullable: true),
                    ITEM_QNTD_2 = table.Column<int>(type: "int", nullable: true),
                    ITEM_QNTD_3 = table.Column<int>(type: "int", nullable: true),
                    ITEM_QNTD_4 = table.Column<int>(type: "int", nullable: true),
                    ITEM_QNTD_5 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__pangya_m__02CC300AD4C936EC", x => new { x.ADM_UID, x.UID });
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_mascot_info",
                schema: "pangya",
                columns: table => new
                {
                    item_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UID = table.Column<int>(type: "int", nullable: false),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    mLevel = table.Column<short>(type: "smallint", nullable: false),
                    mExp = table.Column<int>(type: "int", nullable: false),
                    Flag = table.Column<short>(type: "smallint", nullable: false),
                    Tipo = table.Column<short>(type: "smallint", nullable: false),
                    RegDate = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    Period = table.Column<short>(type: "smallint", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true),
                    Message = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "Pangya GZ"),
                    IsCash = table.Column<short>(type: "smallint", nullable: false),
                    Price = table.Column<int>(type: "int", nullable: false),
                    Valid = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_mascot_info_item_id", x => x.item_id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_msg_user",
                schema: "pangya",
                columns: table => new
                {
                    msg_idx = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    uid = table.Column<int>(type: "int", nullable: false),
                    uid_from = table.Column<int>(type: "int", nullable: false),
                    valid = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    msg = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false, defaultValue: "hello"),
                    reg_date = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_msg_user_msg_idx", x => x.msg_idx);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_myroom",
                schema: "pangya",
                columns: table => new
                {
                    uid = table.Column<int>(type: "int", nullable: false),
                    senha = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: true, defaultValueSql: "(NULL)"),
                    public_lock = table.Column<short>(type: "smallint", nullable: false),
                    state = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_myroom_uid", x => x.uid);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_new_box",
                schema: "pangya",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    nome = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    opened_typeid = table.Column<int>(type: "int", nullable: false),
                    numero = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    tipo_open = table.Column<byte>(type: "tinyint unsigned", nullable: false, comment: "0 SEND ITEM TO MAIL, 1 SEND ITEM TO MY ROOM"),
                    tipo = table.Column<byte>(type: "tinyint unsigned", nullable: false, comment: "0 SEND ITEM TO MAIL, 1 SEND ITEM TO MY ROOM"),
                    message = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false, defaultValue: "OUUUU VOCÊ GANHOU UM ITEM<GZ>"),
                    active = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)1)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_new_box_item",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    box_id = table.Column<int>(type: "int", nullable: false),
                    nome = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    numero = table.Column<int>(type: "int", nullable: false, defaultValue: -1),
                    probabilidade = table.Column<int>(type: "int", nullable: true, defaultValue: 100),
                    qntd = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    raridade = table.Column<byte>(type: "tinyint unsigned", nullable: false, comment: "0 NORMAL, 1 RARE, 2 SUPER RARE"),
                    duplicar = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    active = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)1)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_new_box_rare_win_log",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    uid = table.Column<int>(type: "int", nullable: false),
                    box_typeid = table.Column<int>(type: "int", nullable: false),
                    item_typeid = table.Column<int>(type: "int", nullable: false),
                    qntd = table.Column<int>(type: "int", nullable: false),
                    raridade = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    win_date = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_new_card_pack",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    quantidade = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    tipo = table.Column<short>(type: "smallint", nullable: false),
                    rate_N = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)100),
                    rate_R = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)100),
                    rate_SR = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)100),
                    rate_SC = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)100)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_new_cards",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    probabilidade = table.Column<int>(type: "int", nullable: false),
                    tipo = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    pack = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_new_course_drop",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    rate_mana_artefact = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    rate_grand_prix_ticket = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    rate_SSC_ticket = table.Column<int>(type: "int", nullable: false, defaultValue: 100)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_new_course_drop_item",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    course = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    tipo = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    quantidade = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    probabilidade_3H = table.Column<int>(type: "int", nullable: false),
                    probabilidade_6H = table.Column<int>(type: "int", nullable: false),
                    probabilidade_9H = table.Column<int>(type: "int", nullable: false),
                    probabilidade_18H = table.Column<int>(type: "int", nullable: false),
                    active = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)1)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_new_memorial_coin",
                schema: "pangya",
                columns: table => new
                {
                    tipo = table.Column<int>(type: "int", nullable: true),
                    nome = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    typeid = table.Column<int>(type: "int", nullable: true),
                    probabilidade = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_new_memorial_level",
                schema: "pangya",
                columns: table => new
                {
                    level = table.Column<int>(type: "int", nullable: false),
                    gacha_start = table.Column<int>(type: "int", nullable: false),
                    gacha_end = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_new_memorial_lucky_set",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    set_id = table.Column<int>(type: "int", nullable: false),
                    nome = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    qntd = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_new_memorial_normal_item",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    nome = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    qntd = table.Column<int>(type: "int", nullable: false),
                    tipo = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    active = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)1)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_new_memorial_rare_item",
                schema: "pangya",
                columns: table => new
                {
                    item_tipo = table.Column<int>(type: "int", nullable: true),
                    item_nome = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    item_typeid = table.Column<int>(type: "int", nullable: true),
                    item_probabilidade = table.Column<int>(type: "int", nullable: true),
                    item_gacha_number = table.Column<int>(type: "int", nullable: true),
                    item_active = table.Column<int>(type: "int", nullable: true),
                    coin_typeid = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_new_memorial_rare_win_log",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    uid = table.Column<int>(type: "int", nullable: false),
                    coin_typeid = table.Column<int>(type: "int", nullable: false),
                    item_typeid = table.Column<int>(type: "int", nullable: false),
                    item_qntd = table.Column<int>(type: "int", nullable: false),
                    item_raridade = table.Column<int>(type: "int", nullable: false),
                    item_probabilidade = table.Column<int>(type: "int", nullable: false),
                    win_date = table.Column<DateTime>(type: "datetime(6)", nullable: true, defaultValueSql: "(getdate())"),
                    memorial_nr = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_new_premium_user",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    uid = table.Column<int>(type: "int", nullable: false),
                    limit_cnt = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    Start = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    End = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Received = table.Column<short>(type: "smallint", nullable: false),
                    last_update = table.Column<DateTime>(type: "datetime(6)", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_new_premium_user_item",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    qtd = table.Column<int>(type: "int", nullable: false),
                    tipo = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    active = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)1)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_new_premium_user_log",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    uid = table.Column<int>(type: "int", nullable: false),
                    date = table.Column<DateTime>(type: "datetime(6)", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_notice_list",
                schema: "pangya",
                columns: table => new
                {
                    notice_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    message = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: true, defaultValueSql: "(NULL)"),
                    replayCount = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    refreshTime = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_notice_list_notice_id", x => x.notice_id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_papel_shop_config",
                schema: "pangya",
                columns: table => new
                {
                    Numero = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Price_Normal = table.Column<long>(type: "bigint", nullable: false, defaultValue: 900L),
                    Price_Big = table.Column<long>(type: "bigint", nullable: false, defaultValue: 10000L),
                    Limitted_YN = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)1),
                    Update_Date = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_papel_shop_coupon",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    active = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)1)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_papel_shop_info",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    uid = table.Column<int>(type: "int", nullable: false),
                    current_cnt = table.Column<short>(type: "smallint", nullable: false),
                    remain_cnt = table.Column<short>(type: "smallint", nullable: false),
                    limit_cnt = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)50),
                    last_update = table.Column<DateTime>(type: "datetime(6)", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_papel_shop_item",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    probabilidade = table.Column<int>(type: "int", nullable: false),
                    numero = table.Column<int>(type: "int", nullable: false, defaultValue: -1),
                    tipo = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    active = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)1)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_papel_shop_rare_win_log",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    uid = table.Column<int>(type: "int", nullable: false),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    qntd = table.Column<int>(type: "int", nullable: false),
                    ball_color = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    probabilidade = table.Column<int>(type: "int", nullable: false),
                    reg_date = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_parts_list",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    tipo = table.Column<int>(type: "int", nullable: false),
                    equip_flag = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_parts_list_index", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_personal_shop_config",
                schema: "pangya",
                columns: table => new
                {
                    Index = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    ID = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<int>(type: "int", nullable: false),
                    reg_date = table.Column<DateTime>(type: "datetime(6)", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__pangya_p__9A5B62289F36E46A", x => x.Index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_personal_shop_log",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    player_sell_uid = table.Column<int>(type: "int", nullable: false),
                    player_buy_uid = table.Column<int>(type: "int", nullable: false),
                    item_typeid = table.Column<int>(type: "int", nullable: false),
                    item_id_sell = table.Column<int>(type: "int", nullable: false),
                    item_id_buy = table.Column<int>(type: "int", nullable: false),
                    item_qntd = table.Column<int>(type: "int", nullable: false),
                    item_pang = table.Column<long>(type: "bigint", nullable: false),
                    total_pang = table.Column<long>(type: "bigint", nullable: false),
                    reg_date = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_player_birth_day_log",
                schema: "pangya",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserID = table.Column<int>(type: "int", nullable: true),
                    LOGIN = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    SendDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__pangya_p__3214EC27880F6C54", x => x.ID);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_player_ip",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    uid = table.Column<int>(type: "int", nullable: false),
                    ip = table.Column<string>(type: "varchar(18)", unicode: false, maxLength: 18, nullable: false, defaultValue: "000.000.000.000"),
                    block_beta = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)1),
                    flag_day = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    change_count = table.Column<int>(type: "int", nullable: false),
                    change_date = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_player_ip_index", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_player_location",
                schema: "pangya",
                columns: table => new
                {
                    UID = table.Column<int>(type: "int", nullable: false),
                    channel = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)-1),
                    lobby = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)-1),
                    room = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)-1),
                    place = table.Column<short>(type: "smallint", nullable: false),
                    RoomId = table.Column<Guid>(type: "char(36)", nullable: true)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_point_event",
                schema: "pangya",
                columns: table => new
                {
                    uid = table.Column<int>(type: "int", nullable: false),
                    points = table.Column<long>(type: "bigint", nullable: false),
                    limit_buy = table.Column<long>(type: "bigint", nullable: false),
                    last_day = table.Column<DateTime>(type: "datetime", nullable: true),
                    reg_date = table.Column<DateTime>(type: "datetime(6)", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_point_event_uid", x => x.uid);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_point_event_items",
                schema: "pangya",
                columns: table => new
                {
                    NAME = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: false, defaultValue: "NAME ITEM"),
                    TYPEID = table.Column<int>(type: "int", nullable: false),
                    ICON = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true, defaultValue: "icon_x"),
                    PRICE = table.Column<int>(type: "int", nullable: true, defaultValueSql: "('0')"),
                    IFF_TYPE = table.Column<short>(type: "smallint", nullable: true, defaultValueSql: "('0')"),
                    CHAR_TYPE = table.Column<int>(type: "int", nullable: true, defaultValueSql: "('0')"),
                    ACTIVED = table.Column<int>(type: "int", nullable: true, defaultValueSql: "('0')")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_premioindicacao_log",
                schema: "pangya",
                columns: table => new
                {
                    Log_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ADM_UID = table.Column<int>(type: "int", nullable: false),
                    UID = table.Column<int>(type: "int", nullable: false),
                    cash = table.Column<int>(type: "int", nullable: false),
                    cookie_point = table.Column<int>(type: "int", nullable: false),
                    pangs = table.Column<int>(type: "int", nullable: false),
                    ITEM_TYPEID_1 = table.Column<int>(type: "int", nullable: false),
                    ITEM_TYPEID_2 = table.Column<int>(type: "int", nullable: false),
                    ITEM_TYPEID_3 = table.Column<int>(type: "int", nullable: false),
                    ITEM_TYPEID_4 = table.Column<int>(type: "int", nullable: false),
                    ITEM_TYPEID_5 = table.Column<int>(type: "int", nullable: false),
                    ITEM_QNTD_1 = table.Column<int>(type: "int", nullable: false),
                    ITEM_QNTD_2 = table.Column<int>(type: "int", nullable: false),
                    ITEM_QNTD_3 = table.Column<int>(type: "int", nullable: false),
                    ITEM_QNTD_4 = table.Column<int>(type: "int", nullable: false),
                    ITEM_QNTD_5 = table.Column<int>(type: "int", nullable: false),
                    Log_Date = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__pangya_p__2D26E7AE20251924", x => x.Log_ID);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_quest",
                schema: "pangya",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    achievement_id = table.Column<int>(type: "int", nullable: false),
                    uid = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    counter_item_id = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime(6)", nullable: true, defaultValueSql: "(NULL)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_quest_copy1", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_quest_clear",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    uid = table.Column<int>(type: "int", nullable: false),
                    quest_id = table.Column<int>(type: "int", nullable: false),
                    option = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_quest_clear_index", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_rank_antes",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    position = table.Column<int>(type: "int", nullable: false),
                    UID = table.Column<int>(type: "int", nullable: false),
                    tipo_rank = table.Column<short>(type: "smallint", nullable: false),
                    tipo_rank_seq = table.Column<short>(type: "smallint", nullable: false),
                    valor = table.Column<int>(type: "int", nullable: false),
                    reg_date = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_rank_antes_index", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_rank_atual",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    position = table.Column<int>(type: "int", nullable: false),
                    UID = table.Column<int>(type: "int", nullable: false),
                    tipo_rank = table.Column<short>(type: "smallint", nullable: false),
                    tipo_rank_seq = table.Column<short>(type: "smallint", nullable: false),
                    valor = table.Column<int>(type: "int", nullable: false),
                    reg_date = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_rank_atual_index", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_rank_atual_character",
                schema: "pangya",
                columns: table => new
                {
                    uid = table.Column<int>(type: "int", nullable: false),
                    item_id = table.Column<int>(type: "int", nullable: false),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    itemid_parts_1 = table.Column<int>(type: "int", nullable: false),
                    itemid_parts_2 = table.Column<int>(type: "int", nullable: false),
                    itemid_parts_3 = table.Column<int>(type: "int", nullable: false),
                    itemid_parts_4 = table.Column<int>(type: "int", nullable: false),
                    itemid_parts_5 = table.Column<int>(type: "int", nullable: false),
                    itemid_parts_6 = table.Column<int>(type: "int", nullable: false),
                    itemid_parts_7 = table.Column<int>(type: "int", nullable: false),
                    itemid_parts_8 = table.Column<int>(type: "int", nullable: false),
                    itemid_parts_9 = table.Column<int>(type: "int", nullable: false),
                    itemid_parts_10 = table.Column<int>(type: "int", nullable: false),
                    itemid_parts_11 = table.Column<int>(type: "int", nullable: false),
                    itemid_parts_12 = table.Column<int>(type: "int", nullable: false),
                    itemid_parts_13 = table.Column<int>(type: "int", nullable: false),
                    itemid_parts_14 = table.Column<int>(type: "int", nullable: false),
                    itemid_parts_15 = table.Column<int>(type: "int", nullable: false),
                    itemid_parts_16 = table.Column<int>(type: "int", nullable: false),
                    itemid_parts_17 = table.Column<int>(type: "int", nullable: false),
                    itemid_parts_18 = table.Column<int>(type: "int", nullable: false),
                    itemid_parts_19 = table.Column<int>(type: "int", nullable: false),
                    itemid_parts_20 = table.Column<int>(type: "int", nullable: false),
                    itemid_parts_21 = table.Column<int>(type: "int", nullable: false),
                    itemid_parts_22 = table.Column<int>(type: "int", nullable: false),
                    itemid_parts_23 = table.Column<int>(type: "int", nullable: false),
                    itemid_parts_24 = table.Column<int>(type: "int", nullable: false),
                    parts_1 = table.Column<int>(type: "int", nullable: false),
                    parts_2 = table.Column<int>(type: "int", nullable: false),
                    parts_3 = table.Column<int>(type: "int", nullable: false),
                    parts_4 = table.Column<int>(type: "int", nullable: false),
                    parts_5 = table.Column<int>(type: "int", nullable: false),
                    parts_6 = table.Column<int>(type: "int", nullable: false),
                    parts_7 = table.Column<int>(type: "int", nullable: false),
                    parts_8 = table.Column<int>(type: "int", nullable: false),
                    parts_9 = table.Column<int>(type: "int", nullable: false),
                    parts_10 = table.Column<int>(type: "int", nullable: false),
                    parts_11 = table.Column<int>(type: "int", nullable: false),
                    parts_12 = table.Column<int>(type: "int", nullable: false),
                    parts_13 = table.Column<int>(type: "int", nullable: false),
                    parts_14 = table.Column<int>(type: "int", nullable: false),
                    parts_15 = table.Column<int>(type: "int", nullable: false),
                    parts_16 = table.Column<int>(type: "int", nullable: false),
                    parts_17 = table.Column<int>(type: "int", nullable: false),
                    parts_18 = table.Column<int>(type: "int", nullable: false),
                    parts_19 = table.Column<int>(type: "int", nullable: false),
                    parts_20 = table.Column<int>(type: "int", nullable: false),
                    parts_21 = table.Column<int>(type: "int", nullable: false),
                    parts_22 = table.Column<int>(type: "int", nullable: false),
                    parts_23 = table.Column<int>(type: "int", nullable: false),
                    parts_24 = table.Column<int>(type: "int", nullable: false),
                    default_hair = table.Column<short>(type: "smallint", nullable: false),
                    default_shirts = table.Column<short>(type: "smallint", nullable: false),
                    gift_flag = table.Column<short>(type: "smallint", nullable: false),
                    PCL0 = table.Column<short>(type: "smallint", nullable: false),
                    PCL1 = table.Column<short>(type: "smallint", nullable: false),
                    PCL2 = table.Column<short>(type: "smallint", nullable: false),
                    PCL3 = table.Column<short>(type: "smallint", nullable: false),
                    PCL4 = table.Column<short>(type: "smallint", nullable: false),
                    purchase = table.Column<short>(type: "smallint", nullable: false),
                    AUXPARTS_1 = table.Column<int>(type: "int", nullable: false),
                    AUXPARTS_2 = table.Column<int>(type: "int", nullable: false),
                    AUXPARTS_3 = table.Column<int>(type: "int", nullable: false),
                    AUXPARTS_4 = table.Column<int>(type: "int", nullable: false),
                    AUXPARTS_5 = table.Column<int>(type: "int", nullable: false),
                    CutIn_1 = table.Column<int>(type: "int", nullable: false),
                    CutIn_2 = table.Column<int>(type: "int", nullable: false),
                    CutIn_3 = table.Column<int>(type: "int", nullable: false),
                    CutIn_4 = table.Column<int>(type: "int", nullable: false),
                    mastery = table.Column<int>(type: "int", nullable: false),
                    CARD_CHARACTER_1 = table.Column<int>(type: "int", nullable: false),
                    CARD_CHARACTER_2 = table.Column<int>(type: "int", nullable: false),
                    CARD_CHARACTER_3 = table.Column<int>(type: "int", nullable: false),
                    CARD_CHARACTER_4 = table.Column<int>(type: "int", nullable: false),
                    CARD_CADDIE_1 = table.Column<int>(type: "int", nullable: false),
                    CARD_CADDIE_2 = table.Column<int>(type: "int", nullable: false),
                    CARD_CADDIE_3 = table.Column<int>(type: "int", nullable: false),
                    CARD_CADDIE_4 = table.Column<int>(type: "int", nullable: false),
                    CARD_NPC_1 = table.Column<int>(type: "int", nullable: false),
                    CARD_NPC_2 = table.Column<int>(type: "int", nullable: false),
                    CARD_NPC_3 = table.Column<int>(type: "int", nullable: false),
                    CARD_NPC_4 = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_rank_atual_character_uid", x => x.uid);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_rank_config",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    refresh_time_H = table.Column<int>(type: "int", nullable: false),
                    reg_date = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_rank_config_index", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_record",
                schema: "pangya",
                columns: table => new
                {
                    UID = table.Column<int>(type: "int", nullable: false),
                    tipo = table.Column<short>(type: "smallint", nullable: false),
                    course = table.Column<short>(type: "smallint", nullable: false),
                    best_score = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)127),
                    best_pang = table.Column<long>(type: "bigint", nullable: false),
                    character_typeid = table.Column<int>(type: "int", nullable: false),
                    event_score = table.Column<short>(type: "smallint", nullable: false),
                    tacada = table.Column<int>(type: "int", nullable: false),
                    putt = table.Column<int>(type: "int", nullable: false),
                    hole = table.Column<int>(type: "int", nullable: false),
                    fairway = table.Column<int>(type: "int", nullable: false),
                    puttin = table.Column<int>(type: "int", nullable: false),
                    total_score = table.Column<int>(type: "int", nullable: false),
                    holein = table.Column<int>(type: "int", nullable: false),
                    assist = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_rescue_pwd_log",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    uid = table.Column<int>(type: "int", nullable: false),
                    tipo = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)1),
                    key_uniq = table.Column<Guid>(type: "char(36)", nullable: false, defaultValueSql: "(newid())"),
                    state = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    send_date = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_reward_ssc",
                schema: "pangya",
                columns: table => new
                {
                    valor = table.Column<int>(type: "int", nullable: false),
                    probabilidade = table.Column<int>(type: "int", nullable: false),
                    tipo = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_room_log",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    RoomId = table.Column<Guid>(type: "char(36)", nullable: true, defaultValueSql: "(newid())"),
                    Name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Master_UID = table.Column<int>(type: "int", nullable: true),
                    Number_Players = table.Column<int>(type: "int", nullable: true, defaultValueSql: "('0')"),
                    Max_Players = table.Column<int>(type: "int", nullable: true, defaultValueSql: "('0')"),
                    GM_EVENT = table.Column<int>(type: "int", nullable: true, defaultValueSql: "('0')"),
                    Tipo = table.Column<int>(type: "int", nullable: true),
                    TipoEx = table.Column<int>(type: "int", nullable: true),
                    Modo = table.Column<int>(type: "int", nullable: true),
                    NaturalMode = table.Column<int>(type: "int", nullable: true),
                    ShotMode = table.Column<int>(type: "int", nullable: true),
                    QntdHole = table.Column<int>(type: "int", nullable: true),
                    Course = table.Column<int>(type: "int", nullable: true),
                    Hole = table.Column<int>(type: "int", nullable: true),
                    UID = table.Column<int>(type: "int", nullable: true),
                    Character = table.Column<int>(type: "int", nullable: true),
                    Club = table.Column<int>(type: "int", nullable: true),
                    Mascot = table.Column<int>(type: "int", nullable: true),
                    Caddie = table.Column<int>(type: "int", nullable: true),
                    SpecialShot = table.Column<int>(type: "int", nullable: true),
                    Score = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    Exp = table.Column<int>(type: "int", nullable: true),
                    Pang = table.Column<long>(type: "bigint", nullable: true),
                    BonusPang = table.Column<long>(type: "bigint", nullable: true),
                    TacadaNum = table.Column<int>(type: "int", nullable: true),
                    TotalTacadaNum = table.Column<int>(type: "int", nullable: true),
                    Hio_Hit = table.Column<int>(type: "int", nullable: true),
                    Alba_Hit = table.Column<int>(type: "int", nullable: true),
                    Eagle_Hit = table.Column<int>(type: "int", nullable: true, defaultValueSql: "('0')"),
                    Birdie_Hit = table.Column<int>(type: "int", nullable: true),
                    Par_Hit = table.Column<int>(type: "int", nullable: true),
                    Bogey_Hit = table.Column<int>(type: "int", nullable: true),
                    DoubleBogey_Hit = table.Column<int>(type: "int", nullable: true),
                    TripleBogey_Hit = table.Column<int>(type: "int", nullable: true),
                    GiveUp = table.Column<int>(type: "int", nullable: true),
                    TimeOut = table.Column<int>(type: "int", nullable: true),
                    EnterAfterStarted = table.Column<int>(type: "int", nullable: true),
                    AssistFlag = table.Column<int>(type: "int", nullable: true),
                    Trofeu = table.Column<int>(type: "int", nullable: true),
                    FinishGame = table.Column<int>(type: "int", nullable: true),
                    Data = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_server_list",
                schema: "pangya",
                columns: table => new
                {
                    Name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, defaultValue: "JaCk2 Server"),
                    UID = table.Column<int>(type: "int", nullable: false),
                    IP = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    MaxUser = table.Column<int>(type: "int", nullable: false),
                    CurrUser = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<short>(type: "smallint", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    State = table.Column<short>(type: "smallint", nullable: false),
                    PCBangUser = table.Column<short>(type: "smallint", nullable: false),
                    PangRate = table.Column<int>(type: "int", nullable: false),
                    ServerVersion = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false, defaultValue: ""),
                    ClientVersion = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    property = table.Column<int>(type: "int", nullable: false),
                    AngelicWingsNum = table.Column<int>(type: "int", nullable: false),
                    EventFlag = table.Column<short>(type: "smallint", nullable: false),
                    ExpRate = table.Column<int>(type: "int", nullable: false),
                    RareItemRate = table.Column<int>(type: "int", nullable: false),
                    CookieItemRate = table.Column<int>(type: "int", nullable: false),
                    ServiceControl = table.Column<int>(type: "int", nullable: false),
                    ImgNo = table.Column<short>(type: "smallint", nullable: false),
                    AppRate = table.Column<short>(type: "smallint", nullable: false),
                    ScratchRate = table.Column<short>(type: "smallint", nullable: false),
                    EventMap = table.Column<int>(type: "int", nullable: false),
                    EventDropRate = table.Column<int>(type: "int", nullable: false),
                    HanbitUser = table.Column<int>(type: "int", nullable: false),
                    ParanUser = table.Column<int>(type: "int", nullable: false),
                    AuthState = table.Column<short>(type: "smallint", nullable: false),
                    MasteryRate = table.Column<short>(type: "smallint", nullable: false),
                    TreasureRate = table.Column<short>(type: "smallint", nullable: false),
                    ChuvaRate = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_shop_gift",
                schema: "pangya",
                columns: table => new
                {
                    gift_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    gift_name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    item_name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, defaultValue: "item name"),
                    item_typeid = table.Column<int>(type: "int", nullable: false),
                    item_qntd = table.Column<int>(type: "int", nullable: false),
                    item_qntd_time = table.Column<int>(type: "int", nullable: false),
                    item_period = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                    required_price = table.Column<int>(type: "int", nullable: false),
                    end_date = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    reg_date = table.Column<DateTime>(type: "datetime(6)", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__pangya_s__C1A26301B9694CA3", x => x.gift_id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_shop_gift_log",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    uid = table.Column<int>(type: "int", nullable: false),
                    gift_id = table.Column<int>(type: "int", nullable: false),
                    item_typeid = table.Column<int>(type: "int", nullable: false),
                    item_qntd = table.Column<int>(type: "int", nullable: false),
                    reg_date = table.Column<DateTime>(type: "datetime(6)", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__pangya_s__1D0A3348E3DDBF0B", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_shutdown_list",
                schema: "pangya",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    date_shutdown = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(getdate())"),
                    replayCount = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    refreshTime = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_shutdown_list_id", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_ticker_list",
                schema: "pangya",
                columns: table => new
                {
                    ticker_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    message = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    nick = table.Column<string>(type: "varchar(22)", unicode: false, maxLength: 22, nullable: false),
                    replayCount = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    refreshTime = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_ticker_list_ticker_id", x => x.ticker_id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_ticket_report",
                schema: "pangya",
                columns: table => new
                {
                    idx = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    trofel_typeid = table.Column<int>(type: "int", nullable: false, defaultValue: 738197504),
                    flag = table.Column<short>(type: "smallint", nullable: false),
                    reg_date = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(getdate())"),
                    tipo = table.Column<int>(type: "int", nullable: false, defaultValue: 4)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_ticket_report_idx", x => x.idx);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_ticket_report_dados",
                schema: "pangya",
                columns: table => new
                {
                    report_id = table.Column<long>(type: "bigint", nullable: false),
                    player_uid = table.Column<int>(type: "int", nullable: false),
                    player_score = table.Column<short>(type: "smallint", nullable: false),
                    player_medalha = table.Column<short>(type: "smallint", nullable: false),
                    player_trofel = table.Column<short>(type: "smallint", nullable: false),
                    player_pang = table.Column<long>(type: "bigint", nullable: false),
                    player_bonus_pang = table.Column<long>(type: "bigint", nullable: false),
                    player_exp = table.Column<int>(type: "int", nullable: false),
                    player_mascot_typeid = table.Column<int>(type: "int", nullable: false),
                    player_state = table.Column<short>(type: "smallint", nullable: false),
                    flag_item_pang = table.Column<short>(type: "smallint", nullable: false),
                    flag_premium_user = table.Column<short>(type: "smallint", nullable: false),
                    finish_date = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_tiki_points",
                schema: "pangya",
                columns: table => new
                {
                    UID = table.Column<int>(type: "int", nullable: false),
                    Tiki_Points = table.Column<long>(type: "bigint", nullable: false),
                    REG_DATE = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    MOD_DATE = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_tiki_points_items",
                schema: "pangya",
                columns: table => new
                {
                    INDEX = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ITEM_NAME = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    ITEM_TYPEID = table.Column<int>(type: "int", nullable: false),
                    ITEM_QNTD = table.Column<int>(type: "int", nullable: false),
                    REQ_POINTS = table.Column<int>(type: "int", nullable: true),
                    ITEM_FLAG = table.Column<int>(type: "int", nullable: true),
                    ITEM_ACTIVE = table.Column<int>(type: "int", nullable: true),
                    REG_DATE = table.Column<DateTime>(type: "datetime(6)", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_transforme_clubset_temp",
                schema: "pangya",
                columns: table => new
                {
                    trans_index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UID = table.Column<int>(type: "int", nullable: false),
                    TAQUEIRA_ID = table.Column<int>(type: "int", nullable: false),
                    STATE = table.Column<int>(type: "int", nullable: false),
                    MASTERY = table.Column<int>(type: "int", nullable: false),
                    STATE2 = table.Column<int>(type: "int", nullable: false),
                    FLAG = table.Column<short>(type: "smallint", nullable: false),
                    CARD_TYPEID = table.Column<int>(type: "int", nullable: false),
                    CARD_QNTD = table.Column<int>(type: "int", nullable: false),
                    TAQUEIRA_TRANS_TYPEID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_transforme_clubset_temp_trans_index", x => x.trans_index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_treasure_hunter_event_item",
                schema: "pangya",
                columns: table => new
                {
                    Index = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    quantidade = table.Column<int>(type: "int", nullable: false),
                    probabilidade = table.Column<int>(type: "int", nullable: false),
                    tipo = table.Column<int>(type: "int", nullable: false),
                    flag = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_treasure_item",
                schema: "pangya",
                columns: table => new
                {
                    Name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true, defaultValueSql: "(NULL)"),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    quantidade = table.Column<int>(type: "int", nullable: false),
                    probabilidade = table.Column<int>(type: "int", nullable: false),
                    tipo = table.Column<int>(type: "int", nullable: false),
                    flag = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_trofel_especial",
                schema: "pangya",
                columns: table => new
                {
                    item_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UID = table.Column<int>(type: "int", nullable: false),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    qntd = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_trofel_especial_item_id", x => x.item_id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_trofel_grandprix",
                schema: "pangya",
                columns: table => new
                {
                    item_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UID = table.Column<int>(type: "int", nullable: false),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    qntd = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_trofel_grandprix_item_id", x => x.item_id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_user_equip",
                schema: "pangya",
                columns: table => new
                {
                    UID = table.Column<int>(type: "int", nullable: false),
                    caddie_id = table.Column<int>(type: "int", nullable: false),
                    character_id = table.Column<int>(type: "int", nullable: false),
                    club_id = table.Column<int>(type: "int", nullable: false),
                    ball_type = table.Column<int>(type: "int", nullable: false),
                    item_slot_1 = table.Column<int>(type: "int", nullable: false),
                    item_slot_2 = table.Column<int>(type: "int", nullable: false),
                    item_slot_3 = table.Column<int>(type: "int", nullable: false),
                    item_slot_4 = table.Column<int>(type: "int", nullable: false),
                    item_slot_5 = table.Column<int>(type: "int", nullable: false),
                    item_slot_6 = table.Column<int>(type: "int", nullable: false),
                    item_slot_7 = table.Column<int>(type: "int", nullable: false),
                    item_slot_8 = table.Column<int>(type: "int", nullable: false),
                    item_slot_9 = table.Column<int>(type: "int", nullable: false),
                    item_slot_10 = table.Column<int>(type: "int", nullable: false),
                    Skin_1 = table.Column<int>(type: "int", nullable: false),
                    Skin_2 = table.Column<int>(type: "int", nullable: false),
                    Skin_3 = table.Column<int>(type: "int", nullable: false),
                    Skin_4 = table.Column<int>(type: "int", nullable: false),
                    Skin_5 = table.Column<int>(type: "int", nullable: false),
                    Skin_6 = table.Column<int>(type: "int", nullable: false),
                    mascot_id = table.Column<int>(type: "int", nullable: false),
                    poster_1 = table.Column<int>(type: "int", nullable: false),
                    poster_2 = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_user_equip_UID", x => x.UID);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_user_macro",
                schema: "pangya",
                columns: table => new
                {
                    UID = table.Column<int>(type: "int", nullable: false),
                    Macro1 = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, defaultValue: "Pangya!"),
                    Macro2 = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, defaultValue: "Pangya!"),
                    Macro3 = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, defaultValue: "Pangya!"),
                    Macro4 = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, defaultValue: "Pangya!"),
                    Macro5 = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, defaultValue: "Pangya!"),
                    Macro6 = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, defaultValue: "Pangya!"),
                    Macro7 = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, defaultValue: "Pangya!"),
                    Macro8 = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, defaultValue: "Pangya!"),
                    Macro9 = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, defaultValue: "Pangya!"),
                    Macro10 = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, defaultValue: "Pangya!")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_users_editor_iff",
                schema: "pangya",
                columns: table => new
                {
                    UID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Username = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    Capability = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Time = table.Column<int>(type: "int", nullable: false, defaultValue: 120),
                    MacAdress = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true, defaultValueSql: "(NULL)"),
                    IsBlocked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    HWID = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true, defaultValueSql: "(NULL)"),
                    LastAcess = table.Column<DateTime>(type: "datetime", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    EndDate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(NULL)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__pangya_u__C5B19602111F3A37", x => x.UID);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_weblink_cookies_key",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    uid = table.Column<int>(type: "int", nullable: false),
                    key = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false, defaultValue: "123456"),
                    valid = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_weblink_cookies_key_index", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_weblink_key",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    uid = table.Column<int>(type: "int", nullable: false),
                    key = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false, defaultValue: "123456"),
                    valid = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_weblink_key_index", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_world_tour_config",
                schema: "pangya",
                columns: table => new
                {
                    EventID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, defaultValue: "World Tour"),
                    StartDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(sysdatetime())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorldTourConfig", x => x.EventID);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_world_tour_event",
                schema: "pangya",
                columns: table => new
                {
                    Index = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UID = table.Column<int>(type: "int", nullable: false),
                    Course = table.Column<int>(type: "int", nullable: false),
                    Completed = table.Column<int>(type: "int", nullable: false),
                    Finish_Data = table.Column<DateTime>(type: "datetime(6)", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_world_tour_event", x => x.Index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_world_tour_event_items",
                schema: "pangya",
                columns: table => new
                {
                    INDEX = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    TOUR_EVENT = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    ITEM_NAME = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    ITEM_TYPEID = table.Column<int>(type: "int", nullable: false),
                    ITEM_QNTD = table.Column<int>(type: "int", nullable: false),
                    ITEM_QNTD_TIME = table.Column<int>(type: "int", nullable: false),
                    EVENT_DESCRIPTION = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    END_EVENT = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    REG_DATE = table.Column<DateTime>(type: "datetime(6)", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_world_tour_event_log",
                schema: "pangya",
                columns: table => new
                {
                    Index = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UID = table.Column<int>(type: "int", nullable: false),
                    SendGift = table.Column<int>(type: "int", nullable: false),
                    Finish_Data = table.Column<DateTime>(type: "datetime(6)", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pangya_world_tour_event_log", x => x.Index);
                },
                comment: "envia o presente sim ou nao")
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "quest_items",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    nome = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false, defaultValue: ""),
                    stuff_typeid = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quest_items_index", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "quest_stuffs",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    nome = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false, defaultValue: ""),
                    counter_typeid = table.Column<int>(type: "int", nullable: false),
                    counter_qntd = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quest_stuffs_index", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "scratchy_item",
                schema: "pangya",
                columns: table => new
                {
                    Name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    TypeID = table.Column<int>(type: "int", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false, defaultValue: -1),
                    Quantidade = table.Column<int>(type: "int", nullable: false),
                    Probabilidade = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    flag = table.Column<short>(type: "smallint", nullable: false),
                    Active = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "scratchy_rare_win",
                schema: "pangya",
                columns: table => new
                {
                    UID = table.Column<int>(type: "int", nullable: false),
                    TypeID = table.Column<int>(type: "int", nullable: false),
                    REG_DATE = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "scratchy_rate",
                schema: "pangya",
                columns: table => new
                {
                    nome = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true, defaultValueSql: "(NULL)"),
                    tipo = table.Column<int>(type: "int", nullable: false),
                    probabilidade = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "shop_products",
                schema: "pangya",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(3)", precision: 3, nullable: false, defaultValueSql: "(getdate())"),
                    Category = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__shop_pro__3214EC27CC8904E0", x => x.ID);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "td_char_equip_s4",
                schema: "pangya",
                columns: table => new
                {
                    SEQ = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UID = table.Column<int>(type: "int", nullable: false),
                    CHAR_ITEMID = table.Column<int>(type: "int", nullable: false),
                    ITEMID = table.Column<int>(type: "int", nullable: false),
                    IN_DATE = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(NULL)"),
                    EQUIP_NUM = table.Column<int>(type: "int", nullable: false),
                    EQUIP_TYPE = table.Column<int>(type: "int", nullable: false),
                    USE_YN = table.Column<string>(type: "varchar(1)", unicode: false, maxLength: 1, nullable: true, defaultValue: "Y")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_td_char_equip_s4_SEQ", x => x.SEQ);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "td_room_data",
                schema: "pangya",
                columns: table => new
                {
                    MYROOM_ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UID = table.Column<int>(type: "int", nullable: false),
                    ROOM_NO = table.Column<int>(type: "int", nullable: false),
                    TYPEID = table.Column<int>(type: "int", nullable: false),
                    POS_X = table.Column<float>(type: "float", nullable: false),
                    POS_Y = table.Column<float>(type: "float", nullable: false),
                    POS_Z = table.Column<float>(type: "float", nullable: false),
                    POS_R = table.Column<float>(type: "float", nullable: false),
                    MOD_SEQ = table.Column<int>(type: "int", nullable: false),
                    DISPLAY_YN = table.Column<string>(type: "varchar(1)", unicode: false, maxLength: 1, nullable: false, defaultValue: "N"),
                    USE_YN = table.Column<string>(type: "varchar(1)", unicode: false, maxLength: 1, nullable: false, defaultValue: "Y"),
                    MOD_DT = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(getdate())"),
                    valid = table.Column<byte>(type: "tinyint unsigned", nullable: true, defaultValue: (byte)1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_td_room_data_MYROOM_ID", x => x.MYROOM_ID);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "temp_counter_typeid_init",
                schema: "pangya",
                columns: table => new
                {
                    index = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    uid = table.Column<int>(type: "int", nullable: false),
                    typeid = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_temp_counter_typeid_init_index", x => x.index);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "temp_tmp",
                schema: "pangya",
                columns: table => new
                {
                    idx = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    uid = table.Column<int>(type: "int", nullable: false),
                    item_id = table.Column<int>(type: "int", nullable: false),
                    typeid = table.Column<int>(type: "int", nullable: false),
                    time = table.Column<int>(type: "int", nullable: false),
                    qntd = table.Column<int>(type: "int", nullable: false),
                    pang = table.Column<int>(type: "int", nullable: false),
                    cookie = table.Column<int>(type: "int", nullable: false),
                    coupon_id = table.Column<int>(type: "int", nullable: false),
                    item_tipo = table.Column<short>(type: "smallint", nullable: false),
                    tipo = table.Column<short>(type: "smallint", nullable: false),
                    item_tempo = table.Column<int>(type: "int", nullable: false),
                    c1 = table.Column<short>(type: "smallint", nullable: false),
                    c2 = table.Column<short>(type: "smallint", nullable: false),
                    c3 = table.Column<short>(type: "smallint", nullable: false),
                    c4 = table.Column<short>(type: "smallint", nullable: false),
                    c5 = table.Column<short>(type: "smallint", nullable: false),
                    x = table.Column<decimal>(type: "numeric(10,0)", nullable: false),
                    y = table.Column<decimal>(type: "numeric(10,0)", nullable: false),
                    z = table.Column<decimal>(type: "numeric(10,0)", nullable: false),
                    r = table.Column<decimal>(type: "numeric(10,0)", nullable: false),
                    UCCIDX = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_temp_tmp_idx", x => x.idx);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "temp_typeid",
                schema: "pangya",
                columns: table => new
                {
                    ITEM_ID = table.Column<int>(type: "int", nullable: false),
                    TYPEID = table.Column<int>(type: "int", nullable: false),
                    QUANTIDADE = table.Column<int>(type: "int", nullable: false),
                    QNTD_DIA = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "trofel_stat",
                schema: "pangya",
                columns: table => new
                {
                    UID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    AMA_6_G = table.Column<short>(type: "smallint", nullable: false),
                    AMA_6_S = table.Column<short>(type: "smallint", nullable: false),
                    AMA_6_B = table.Column<short>(type: "smallint", nullable: false),
                    AMA_5_G = table.Column<short>(type: "smallint", nullable: false),
                    AMA_5_S = table.Column<short>(type: "smallint", nullable: false),
                    AMA_5_B = table.Column<short>(type: "smallint", nullable: false),
                    AMA_4_G = table.Column<short>(type: "smallint", nullable: false),
                    AMA_4_S = table.Column<short>(type: "smallint", nullable: false),
                    AMA_4_B = table.Column<short>(type: "smallint", nullable: false),
                    AMA_3_G = table.Column<short>(type: "smallint", nullable: false),
                    AMA_3_S = table.Column<short>(type: "smallint", nullable: false),
                    AMA_3_B = table.Column<short>(type: "smallint", nullable: false),
                    AMA_2_G = table.Column<short>(type: "smallint", nullable: false),
                    AMA_2_S = table.Column<short>(type: "smallint", nullable: false),
                    AMA_2_B = table.Column<short>(type: "smallint", nullable: false),
                    AMA_1_G = table.Column<short>(type: "smallint", nullable: false),
                    AMA_1_S = table.Column<short>(type: "smallint", nullable: false),
                    AMA_1_B = table.Column<short>(type: "smallint", nullable: false),
                    PRO_1_G = table.Column<short>(type: "smallint", nullable: false),
                    PRO_1_S = table.Column<short>(type: "smallint", nullable: false),
                    PRO_1_B = table.Column<short>(type: "smallint", nullable: false),
                    PRO_2_G = table.Column<short>(type: "smallint", nullable: false),
                    PRO_2_S = table.Column<short>(type: "smallint", nullable: false),
                    PRO_2_B = table.Column<short>(type: "smallint", nullable: false),
                    PRO_3_G = table.Column<short>(type: "smallint", nullable: false),
                    PRO_3_S = table.Column<short>(type: "smallint", nullable: false),
                    PRO_3_B = table.Column<short>(type: "smallint", nullable: false),
                    PRO_4_G = table.Column<short>(type: "smallint", nullable: false),
                    PRO_4_S = table.Column<short>(type: "smallint", nullable: false),
                    PRO_4_B = table.Column<short>(type: "smallint", nullable: false),
                    PRO_5_G = table.Column<short>(type: "smallint", nullable: false),
                    PRO_5_S = table.Column<short>(type: "smallint", nullable: false),
                    PRO_5_B = table.Column<short>(type: "smallint", nullable: false),
                    PRO_6_G = table.Column<short>(type: "smallint", nullable: false),
                    PRO_6_S = table.Column<short>(type: "smallint", nullable: false),
                    PRO_6_B = table.Column<short>(type: "smallint", nullable: false),
                    PRO_7_G = table.Column<short>(type: "smallint", nullable: false),
                    PRO_7_S = table.Column<short>(type: "smallint", nullable: false),
                    PRO_7_B = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trofel_stat_UID", x => x.UID);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tu_ucc",
                schema: "pangya",
                columns: table => new
                {
                    UID = table.Column<long>(type: "bigint", nullable: false),
                    TYPEID = table.Column<long>(type: "bigint", nullable: false),
                    SEQ = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    ITEM_ID = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    UCCIDX = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: true, defaultValue: ""),
                    UCC_NAME = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true, defaultValueSql: "(NULL)"),
                    USE_YN = table.Column<string>(type: "varchar(1)", unicode: false, maxLength: 1, nullable: false, defaultValue: "N"),
                    IN_DATE = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false, defaultValueSql: "(getdate())"),
                    COPIER = table.Column<int>(type: "int", nullable: true, defaultValueSql: "(NULL)"),
                    COPIER_NICK = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: true, defaultValueSql: "(NULL)"),
                    DRAW_DT = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true, defaultValueSql: "(NULL)"),
                    STATUS = table.Column<short>(type: "smallint", nullable: false),
                    Flag = table.Column<short>(type: "smallint", nullable: false),
                    SKEY = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true, defaultValueSql: "(NULL)"),
                    TRADE = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tu_ucc_UID", x => new { x.UID, x.TYPEID, x.SEQ, x.ITEM_ID });
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tutorial",
                schema: "pangya",
                columns: table => new
                {
                    UID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Rookie = table.Column<int>(type: "int", nullable: false),
                    Beginner = table.Column<int>(type: "int", nullable: false),
                    Advancer = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tutorial_UID", x => x.UID);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "type_list",
                schema: "pangya",
                columns: table => new
                {
                    TypeId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false, defaultValue: "0"),
                    Icon = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, defaultValue: "0"),
                    Price = table.Column<long>(type: "bigint", nullable: false),
                    type = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_type_list_TypeId", x => x.TypeId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "user_info",
                schema: "pangya",
                columns: table => new
                {
                    UID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Tacadas = table.Column<long>(type: "bigint", nullable: false),
                    Putt = table.Column<long>(type: "bigint", nullable: false),
                    Tempo = table.Column<long>(type: "bigint", nullable: false),
                    Tempotacadas = table.Column<long>(name: "Tempo tacadas", type: "bigint", nullable: false),
                    Max_distancia = table.Column<float>(type: "float", nullable: false),
                    Acerto_pangya = table.Column<long>(type: "bigint", nullable: false),
                    Bunker = table.Column<int>(type: "int", nullable: false),
                    OB = table.Column<long>(name: "O.B", type: "bigint", nullable: false),
                    Total_distancia = table.Column<long>(type: "bigint", nullable: false),
                    Holes = table.Column<long>(type: "bigint", nullable: false),
                    Holein = table.Column<int>(type: "int", nullable: false),
                    HIO = table.Column<long>(type: "bigint", nullable: false),
                    Timeout = table.Column<short>(type: "smallint", nullable: false),
                    Fairway = table.Column<long>(type: "bigint", nullable: false),
                    Albatross = table.Column<long>(type: "bigint", nullable: false),
                    MaConduta = table.Column<int>(type: "int", nullable: false),
                    Acerto_Putt = table.Column<long>(type: "bigint", nullable: false),
                    Longputt = table.Column<float>(name: "Long-putt", type: "float", nullable: false),
                    Chipin = table.Column<float>(name: "Chip-in", type: "float", nullable: false),
                    Xp = table.Column<long>(type: "bigint", nullable: false),
                    level = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Pang = table.Column<long>(type: "bigint", nullable: false),
                    Media_score = table.Column<int>(type: "int", nullable: false),
                    BestScore0 = table.Column<short>(type: "smallint", nullable: false),
                    BestScore1 = table.Column<short>(type: "smallint", nullable: false),
                    BestScore2 = table.Column<short>(type: "smallint", nullable: false),
                    BestScore3 = table.Column<short>(type: "smallint", nullable: false),
                    BestScore4 = table.Column<short>(type: "smallint", nullable: false),
                    MaxPang0 = table.Column<long>(type: "bigint", nullable: false),
                    maxPang1 = table.Column<long>(type: "bigint", nullable: false),
                    maxPang2 = table.Column<long>(type: "bigint", nullable: false),
                    maxPang3 = table.Column<long>(type: "bigint", nullable: false),
                    maxPang4 = table.Column<long>(type: "bigint", nullable: false),
                    SumPang = table.Column<long>(type: "bigint", nullable: false),
                    EventFlag = table.Column<short>(type: "smallint", nullable: false),
                    Jogado = table.Column<long>(type: "bigint", nullable: false),
                    Quitado = table.Column<long>(type: "bigint", nullable: false),
                    SkinPang = table.Column<long>(type: "bigint", nullable: false),
                    SkinWin = table.Column<int>(type: "int", nullable: false),
                    SkinLose = table.Column<int>(type: "int", nullable: false),
                    SkinRunHole = table.Column<int>(type: "int", nullable: false, defaultValue: -1),
                    SkinStrikePoint = table.Column<int>(type: "int", nullable: false),
                    SkinAllinCount = table.Column<int>(type: "int", nullable: false),
                    Todos_combos = table.Column<long>(type: "bigint", nullable: false),
                    Combos = table.Column<long>(type: "bigint", nullable: false),
                    TeamWin = table.Column<int>(type: "int", nullable: false),
                    TeamGames = table.Column<int>(type: "int", nullable: false),
                    Teamhole = table.Column<long>(type: "bigint", nullable: false),
                    LadderPoint = table.Column<int>(type: "int", nullable: false, defaultValue: 1000),
                    LadderWin = table.Column<int>(type: "int", nullable: false),
                    LadderLose = table.Column<int>(type: "int", nullable: false),
                    LadderDraw = table.Column<int>(type: "int", nullable: false),
                    LadderHole = table.Column<int>(type: "int", nullable: false),
                    EventValue = table.Column<short>(type: "smallint", nullable: false),
                    NaoSei = table.Column<int>(type: "int", nullable: false),
                    MaxJogoNaoSei = table.Column<int>(type: "int", nullable: false),
                    JogosNaoSei = table.Column<int>(type: "int", nullable: false),
                    GameCountSeason = table.Column<int>(type: "int", nullable: false),
                    Cookie = table.Column<long>(type: "bigint", nullable: false),
                    total_pang_win_game = table.Column<long>(type: "bigint", nullable: false),
                    lucky_medal = table.Column<int>(type: "int", nullable: false),
                    fast_medal = table.Column<int>(type: "int", nullable: false),
                    best_drive_medal = table.Column<int>(type: "int", nullable: false),
                    best_chipin_medal = table.Column<int>(type: "int", nullable: false),
                    best_puttin_medal = table.Column<int>(type: "int", nullable: false),
                    best_recovery_medal = table.Column<int>(type: "int", nullable: false),
                    _16bit_naosei = table.Column<short>(name: "16bit_naosei", type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_info_UID", x => x.UID);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pangya_event_itens_site",
                schema: "pangya",
                columns: table => new
                {
                    ITEM_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    EVENTO_ID = table.Column<int>(type: "int", nullable: false),
                    NOME_ITEM = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    QNT_JOGADA = table.Column<int>(type: "int", nullable: false),
                    TYPEID = table.Column<int>(type: "int", nullable: false),
                    QNT_ITEM = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__pangya_e__ADFD89A0F35AC350", x => x.ITEM_ID);
                    table.ForeignKey(
                        name: "FK_EVENTO",
                        column: x => x.EVENTO_ID,
                        principalSchema: "pangya",
                        principalTable: "pangya_event_site",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "shop_product_items",
                schema: "pangya",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ShopProductID = table.Column<int>(type: "int", nullable: false),
                    Cash = table.Column<long>(type: "bigint", nullable: true),
                    Pangs = table.Column<long>(type: "bigint", nullable: true),
                    ItemID = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(3)", precision: 3, nullable: false, defaultValueSql: "(getdate())"),
                    ItemQuantity = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__shop_pro__3214EC27B82E4F15", x => x.ID);
                    table.ForeignKey(
                        name: "fk_shop_product_items_shop_product_id",
                        column: x => x.ShopProductID,
                        principalSchema: "pangya",
                        principalTable: "shop_products",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "shop_purchases",
                schema: "pangya",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    AccountID = table.Column<string>(type: "varchar(25)", unicode: false, maxLength: 25, nullable: false),
                    ShopProductID = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(3)", precision: 3, nullable: false, defaultValueSql: "(getdate())"),
                    Status = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    payment_link = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__shop_pur__3214EC27ECEF865D", x => x.ID);
                    table.ForeignKey(
                        name: "fk_shop_purchases_account_id",
                        column: x => x.AccountID,
                        principalSchema: "pangya",
                        principalTable: "account",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_shop_purchases_shop_product_id",
                        column: x => x.ShopProductID,
                        principalSchema: "pangya",
                        principalTable: "shop_products",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_account",
                schema: "pangya",
                table: "account",
                column: "ID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_1st_aniversary_player_win_cp",
                schema: "pangya",
                table: "pangya_1st_anniversary_player_win_cp",
                column: "UID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_achievement",
                schema: "pangya",
                table: "pangya_achievement",
                column: "ID_ACHIEVEMENT",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya.auth_key",
                schema: "pangya",
                table: "pangya_auth_key",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_card_equip",
                schema: "pangya",
                table: "pangya_card_equip",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_change_email_log",
                schema: "pangya",
                table: "pangya_change_email_log",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_change_nickname_log",
                schema: "pangya",
                table: "pangya_change_nickname_log",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_change_nickname_log_1",
                schema: "pangya",
                table: "pangya_change_nickname_log",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_change_pwd_log",
                schema: "pangya",
                table: "pangya_change_pwd_log",
                column: "uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_change_pwd_log_1",
                schema: "pangya",
                table: "pangya_change_pwd_log",
                column: "uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_comet_refill",
                schema: "pangya",
                table: "pangya_comet_refill",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_command_gm_log",
                schema: "pangya",
                table: "pangya_command_gm_log",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_config",
                schema: "pangya",
                table: "pangya_config",
                column: "UID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_cookie_point_item_log",
                schema: "pangya",
                table: "pangya_cookie_point_item_log",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_cookie_point_log",
                schema: "pangya",
                table: "pangya_cookie_point_log",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya.counter_item",
                schema: "pangya",
                table: "pangya_counter_item",
                column: "Count_ID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_donation_epin",
                schema: "pangya",
                table: "pangya_donation_epin",
                column: "epin",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_donation_epin_1",
                schema: "pangya",
                table: "pangya_donation_epin",
                column: "donation_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_donation_item_log",
                schema: "pangya",
                table: "pangya_donation_item_log",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_donation_item_log_1",
                schema: "pangya",
                table: "pangya_donation_item_log",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_donation_log",
                schema: "pangya",
                table: "pangya_donation_log",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_donation_log_1",
                schema: "pangya",
                table: "pangya_donation_log",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_donation_new",
                schema: "pangya",
                table: "pangya_donation_new",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_EVENTO_ID",
                schema: "pangya",
                table: "pangya_event_itens_site",
                column: "EVENTO_ID");

            migrationBuilder.CreateIndex(
                name: "IX_pangya_gm_gift_web_log",
                schema: "pangya",
                table: "pangya_gm_gift_web_log",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_gm_gift_web_log_1",
                schema: "pangya",
                table: "pangya_gm_gift_web_log",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_guild",
                schema: "pangya",
                table: "pangya_guild",
                column: "GUILD_UID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_guild_atividade_player",
                schema: "pangya",
                table: "pangya_guild_atividade_player",
                column: "IDX",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_guild_bbs",
                schema: "pangya",
                table: "pangya_guild_bbs",
                column: "SEQ",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_guild_bbs_1",
                schema: "pangya",
                table: "pangya_guild_bbs",
                column: "SEQ",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_guild_bbs_2",
                schema: "pangya",
                table: "pangya_guild_bbs",
                column: "OWNER_UID");

            migrationBuilder.CreateIndex(
                name: "IX_pangya_guild_bbs_res",
                schema: "pangya",
                table: "pangya_guild_bbs_res",
                column: "SEQ",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_guild_bbs_res_1",
                schema: "pangya",
                table: "pangya_guild_bbs_res",
                column: "SEQ",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_guild_bbs_res_2",
                schema: "pangya",
                table: "pangya_guild_bbs_res",
                column: "BBS_SEQ");

            migrationBuilder.CreateIndex(
                name: "IX_pangya_guild_bbs_res_3",
                schema: "pangya",
                table: "pangya_guild_bbs_res",
                column: "OWNER_UID");

            migrationBuilder.CreateIndex(
                name: "IX_pangya_guild_intro_img_log",
                schema: "pangya",
                table: "pangya_guild_intro_img_log",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_guild_intro_img_log_1",
                schema: "pangya",
                table: "pangya_guild_intro_img_log",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya.pangya_guild_mark_log",
                schema: "pangya",
                table: "pangya_guild_mark_log",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya.pangya_guild_mark_log_1",
                schema: "pangya",
                table: "pangya_guild_mark_log",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_guild_match",
                schema: "pangya",
                table: "pangya_guild_match",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_guild_match_1",
                schema: "pangya",
                table: "pangya_guild_match",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_guild_match_2",
                schema: "pangya",
                table: "pangya_guild_match",
                column: "guild_1_uid");

            migrationBuilder.CreateIndex(
                name: "IX_pangya_guild_match_3",
                schema: "pangya",
                table: "pangya_guild_match",
                column: "guild_2_uid");

            migrationBuilder.CreateIndex(
                name: "IX_pangya_guild_notice",
                schema: "pangya",
                table: "pangya_guild_notice",
                column: "SEQ",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_guild_notice_1",
                schema: "pangya",
                table: "pangya_guild_notice",
                column: "SEQ",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_guild_notice_2",
                schema: "pangya",
                table: "pangya_guild_notice",
                column: "GUILD_UID");

            migrationBuilder.CreateIndex(
                name: "IX_pangya_guild_notice_3",
                schema: "pangya",
                table: "pangya_guild_notice",
                column: "OWNER_UID");

            migrationBuilder.CreateIndex(
                name: "IX_pangya_guild_private_bbs",
                schema: "pangya",
                table: "pangya_guild_private_bbs",
                column: "SEQ",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_guild_private_bbs_1",
                schema: "pangya",
                table: "pangya_guild_private_bbs",
                column: "SEQ",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_guild_private_bbs_2",
                schema: "pangya",
                table: "pangya_guild_private_bbs",
                column: "GUILD_UID");

            migrationBuilder.CreateIndex(
                name: "IX_pangya_guild_private_bbs_3",
                schema: "pangya",
                table: "pangya_guild_private_bbs",
                column: "OWNER_UID");

            migrationBuilder.CreateIndex(
                name: "IX_pangya_guild_private_bbs_res",
                schema: "pangya",
                table: "pangya_guild_private_bbs_res",
                column: "SEQ",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_guild_private_bbs_res_1",
                schema: "pangya",
                table: "pangya_guild_private_bbs_res",
                column: "SEQ",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_guild_private_bbs_res_2",
                schema: "pangya",
                table: "pangya_guild_private_bbs_res",
                column: "GUILD_BBS_SEQ");

            migrationBuilder.CreateIndex(
                name: "IX_pangya_guild_private_bbs_res_3",
                schema: "pangya",
                table: "pangya_guild_private_bbs_res",
                column: "OWNER_UID");

            migrationBuilder.CreateIndex(
                name: "IX_pangya_guild_update_activity",
                schema: "pangya",
                table: "pangya_guild_update_activity",
                column: "OWNER_UPDATE");

            migrationBuilder.CreateIndex(
                name: "IX_pangya_item_buff",
                schema: "pangya",
                table: "pangya_item_buff",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_item_buy_shop_log",
                schema: "pangya",
                table: "pangya_item_buy_shop_log",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_mac_table",
                schema: "pangya",
                table: "pangya_mac_table",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_mac_table_1",
                schema: "pangya",
                table: "pangya_mac_table",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_new_box",
                schema: "pangya",
                table: "pangya_new_box",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_new_box_rare_win_log",
                schema: "pangya",
                table: "pangya_new_box_rare_win_log",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__pangya_n__1D0A3349DE4D54D1",
                schema: "pangya",
                table: "pangya_new_card_pack",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_new_course_drop_1",
                schema: "pangya",
                table: "pangya_new_course_drop",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_new_course_drop",
                schema: "pangya",
                table: "pangya_new_course_drop_item",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_new_memorial_level",
                schema: "pangya",
                table: "pangya_new_memorial_level",
                column: "level",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_new_memorial_lucky_set",
                schema: "pangya",
                table: "pangya_new_memorial_lucky_set",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_new_memorial_normal_item",
                schema: "pangya",
                table: "pangya_new_memorial_normal_item",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_new_memorial_rare_win_log",
                schema: "pangya",
                table: "pangya_new_memorial_rare_win_log",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_new_premium_user",
                schema: "pangya",
                table: "pangya_new_premium_user",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_new_premium_user_item",
                schema: "pangya",
                table: "pangya_new_premium_user_item",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_new_premium_user_log",
                schema: "pangya",
                table: "pangya_new_premium_user_log",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_papel_shop_coupon",
                schema: "pangya",
                table: "pangya_papel_shop_coupon",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_papel_shop_info",
                schema: "pangya",
                table: "pangya_papel_shop_info",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_papel_shop_item",
                schema: "pangya",
                table: "pangya_papel_shop_item",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_papel_shop_rare_win",
                schema: "pangya",
                table: "pangya_papel_shop_rare_win_log",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_personal_shop_log",
                schema: "pangya",
                table: "pangya_personal_shop_log",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_personal_shop_log_1",
                schema: "pangya",
                table: "pangya_personal_shop_log",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_player_location",
                schema: "pangya",
                table: "pangya_player_location",
                column: "UID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__pangya_q__3213E83E970124D3",
                schema: "pangya",
                table: "pangya_quest",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_rescue_pwd_log",
                schema: "pangya",
                table: "pangya_rescue_pwd_log",
                column: "uid");

            migrationBuilder.CreateIndex(
                name: "IX_pangya_rescue_pwd_log_2",
                schema: "pangya",
                table: "pangya_rescue_pwd_log",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_rescue_pwd_log_3",
                schema: "pangya",
                table: "pangya_rescue_pwd_log",
                column: "index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pangya_server_list",
                schema: "pangya",
                table: "pangya_server_list",
                column: "UID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__pangya_u__536C85E418755C13",
                schema: "pangya",
                table: "pangya_users_editor_iff",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shop_product_items_ShopProductID",
                schema: "pangya",
                table: "shop_product_items",
                column: "ShopProductID");

            migrationBuilder.CreateIndex(
                name: "IX_shop_purchases_AccountID",
                schema: "pangya",
                table: "shop_purchases",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_shop_purchases_ShopProductID",
                schema: "pangya",
                table: "shop_purchases",
                column: "ShopProductID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "achievement_quest",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "achievement_tipo",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "achievements",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "authkey_game",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "authkey_login",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "char_equip",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "contas_beta",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "count_item",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "counter_items",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "indication_status",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "mania_cookies",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_1st_anniversary",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_1st_anniversary_player_win_cp",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_achievement",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_approach_missions",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_assistente",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_attendance_reward",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_attendance_table_item_reward",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_auth_key",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_bot_gm_event_reward",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_bot_gm_event_time",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_caddie_information",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_card",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_card_equip",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_change_email_log",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_change_nickname_log",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_change_pwd_log",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_character_information",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_character_part_padrao",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_clubset_enchant",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_coin_cube_info",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_coin_cube_location",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_comet_refill",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_command",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_command_gm_log",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_config",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_cookie_point_item_log",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_cookie_point_log",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_counter_item",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_coupon_desconto",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_course_cube_coin_temporada",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_course_reward_treasure",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_cube_coin_location",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_daily_quest",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_daily_quest_player",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_dolfini_locker",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_dolfini_locker_item",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_donation_epin",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_donation_item_log",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_donation_log",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_donation_new",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_event_itens_site",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_exception_log",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_fast_pass_event",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_friend_list",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_gacha",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_gacha_coin",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_gacha_items",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_gacha_jp_all_item_list",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_gacha_jp_item_list",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_gacha_jp_player_win",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_gacha_jp_rate",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_gacha_user_key",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_gacha_user_won",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_gift_table",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_gm_gift_web_log",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_golden_time_info",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_golden_time_item",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_golden_time_round",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_grand_zodiac_pontos",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_grand_zodiac_times",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_grandprix_clear",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_grandprix_event_config",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_guild",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_guild_atividade_player",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_guild_bbs",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_guild_bbs_res",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_guild_intro_img_log",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_guild_mark_log",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_guild_match",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_guild_member",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_guild_notice",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_guild_private_bbs",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_guild_private_bbs_res",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_guild_ranking",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_guild_update_activity",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_gz_event_2016121600_rare_win",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_hio_event",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_hio_event_items",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_hio_event_log",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_hole_event",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_hole_event_config",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_hole_event_items",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_ip_table",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_item_buff",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_item_buy_shop_log",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_item_mail",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_item_typelist",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_item_warehouse",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_last_players_user",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_last_up_clubset",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_login_reward",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_login_reward_player",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_lucia_attendance",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_lucia_attendance_reward_log",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_mac_table",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_maniadonation_log",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_mascot_info",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_msg_user",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_myroom",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_new_box",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_new_box_item",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_new_box_rare_win_log",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_new_card_pack",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_new_cards",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_new_course_drop",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_new_course_drop_item",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_new_memorial_coin",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_new_memorial_level",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_new_memorial_lucky_set",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_new_memorial_normal_item",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_new_memorial_rare_item",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_new_memorial_rare_win_log",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_new_premium_user",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_new_premium_user_item",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_new_premium_user_log",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_notice_list",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_papel_shop_config",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_papel_shop_coupon",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_papel_shop_info",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_papel_shop_item",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_papel_shop_rare_win_log",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_parts_list",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_personal_shop_config",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_personal_shop_log",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_player_birth_day_log",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_player_ip",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_player_location",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_point_event",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_point_event_items",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_premioindicacao_log",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_quest",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_quest_clear",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_rank_antes",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_rank_atual",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_rank_atual_character",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_rank_config",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_record",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_rescue_pwd_log",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_reward_ssc",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_room_log",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_server_list",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_shop_gift",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_shop_gift_log",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_shutdown_list",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_ticker_list",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_ticket_report",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_ticket_report_dados",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_tiki_points",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_tiki_points_items",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_transforme_clubset_temp",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_treasure_hunter_event_item",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_treasure_item",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_trofel_especial",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_trofel_grandprix",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_user_equip",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_user_macro",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_users_editor_iff",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_weblink_cookies_key",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_weblink_key",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_world_tour_config",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_world_tour_event",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_world_tour_event_items",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_world_tour_event_log",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "quest_items",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "quest_stuffs",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "scratchy_item",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "scratchy_rare_win",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "scratchy_rate",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "shop_product_items",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "shop_purchases",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "td_char_equip_s4",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "td_room_data",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "temp_counter_typeid_init",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "temp_tmp",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "temp_typeid",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "trofel_stat",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "tu_ucc",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "tutorial",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "type_list",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "user_info",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "pangya_event_site",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "account",
                schema: "pangya");

            migrationBuilder.DropTable(
                name: "shop_products",
                schema: "pangya");
        }
    }
}
