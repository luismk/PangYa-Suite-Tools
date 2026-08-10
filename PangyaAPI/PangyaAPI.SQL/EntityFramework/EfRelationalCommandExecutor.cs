using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;

namespace PangyaAPI.SQL.EntityFramework
{
    public sealed class EfRelationalCommandExecutor : IRelationalCommandExecutor
    {
        private readonly PangyaDbContextFactory _contextFactory;
        private readonly DatabaseOptions _database;

        public EfRelationalCommandExecutor(PangyaDbContextFactory contextFactory, DatabaseOptions database)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public Response ExecuteText(string commandText, IReadOnlyList<RelationalParameter> parameters)
        {
            if (string.IsNullOrWhiteSpace(commandText))
                throw new ArgumentException("A command is required.", nameof(commandText));

            return Execute(commandText, parameters ?? Array.Empty<RelationalParameter>());
        }

        public Response ExecuteStoredProcedure(string procedureName, IReadOnlyList<RelationalParameter> parameters)
        {
            if (string.IsNullOrWhiteSpace(procedureName))
                throw new ArgumentException("A procedure name is required.", nameof(procedureName));

            parameters ??= Array.Empty<RelationalParameter>();
            var placeholders = string.Join(", ", GetParameterNames(parameters));
            var engine = _database.Engine.Trim().ToUpperInvariant();
            var commandText = engine switch
            {
                "MSSQL" or "SQLSERVER" => string.IsNullOrEmpty(placeholders)
                    ? $"EXEC {procedureName}"
                    : $"EXEC {procedureName} {placeholders}",
                "MYSQL" => $"CALL {procedureName}({placeholders})",
                "POSTGRESQL" or "PGSQL" => $"CALL {procedureName}({placeholders})",
                _ => throw new NotSupportedException($"Unsupported database engine '{_database.Engine}'.")
            };

            return Execute(commandText, parameters);
        }

        private Response Execute(string commandText, IReadOnlyList<RelationalParameter> parameters)
        {
            using var context = _contextFactory.CreateDbContext();
            var strategy = context.Database.CreateExecutionStrategy();
            return strategy.Execute(() => ExecuteOnce(context, commandText, parameters));
        }

        private Response ExecuteOnce(
            PangyaDbContext context,
            string commandText,
            IReadOnlyList<RelationalParameter> parameters)
        {
            var connection = context.Database.GetDbConnection();
            var openedHere = connection.State != ConnectionState.Open;

            try
            {
                if (openedHere)
                    connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = commandText;
                command.CommandType = CommandType.Text;
                command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();

                foreach (var value in parameters)
                    command.Parameters.Add(CreateParameter(command, value));

                if (_database.LogCommands)
                    PangyaLog.Write($"[EF::Execute][Command] {commandText}", LogDestination.GeneralFile);

                using var reader = command.ExecuteReader();
                return ReadResponse(reader);
            }
            catch (Exception caught)
            {
                throw new exception(
                    $"[EF::Execute][Error] {caught.Message} [Command: {commandText}]",
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB, 0, 0));
            }
            finally
            {
                if (openedHere && connection.State != ConnectionState.Closed)
                    connection.Close();
            }
        }

        private static DbParameter CreateParameter(DbCommand command, RelationalParameter value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = value.Name;
            parameter.Direction = value.Direction;
            parameter.Value = NormalizeParameterValue(value.Value);
            if (value.SqlDbType.HasValue)
                parameter.DbType = ToDbType(value.SqlDbType.Value);
            return parameter;
        }

        internal static object NormalizeParameterValue(object value)
        {
            return value switch
            {
                null => DBNull.Value,
                sbyte signedByte => (short)signedByte,
                ushort unsignedShort => (int)unsignedShort,
                uint unsignedInteger => (long)unsignedInteger,
                ulong unsignedLong => (decimal)unsignedLong,
                _ => value
            };
        }

        private static Response ReadResponse(DbDataReader reader)
        {
            var response = new Response();
            do
            {
                var fieldCount = reader.FieldCount;
                while (fieldCount > 0 && reader.Read())
                {
                    var table = new DataTable();
                    for (var index = 0; index < fieldCount; index++)
                    {
                        var name = reader.GetName(index);
                        if (string.IsNullOrWhiteSpace(name) || table.Columns.Contains(name))
                            name = $"Column{index}";
                        table.Columns.Add(name, typeof(object));
                    }

                    var values = new object[fieldCount];
                    reader.GetValues(values);
                    for (var index = 0; index < values.Length; index++)
                    {
                        if (values[index] is DBNull)
                            values[index] = null;
                    }

                    var row = table.Rows.Add(values);
                    response.addResultSet(new Result_Set(
                        (uint)Result_Set.STATE_TYPE.HAVE_DATA,
                        1,
                        (uint)fieldCount,
                        row));
                }
            }
            while (reader.NextResult());

            response.setRowsAffected(reader.RecordsAffected < 0 ? 0 : reader.RecordsAffected);
            return response;
        }

        private static IEnumerable<string> GetParameterNames(IReadOnlyList<RelationalParameter> parameters)
        {
            for (var index = 0; index < parameters.Count; index++)
                yield return parameters[index].Name;
        }

        private static DbType ToDbType(SqlDbType type)
        {
            return type switch
            {
                SqlDbType.BigInt => DbType.Int64,
                SqlDbType.Binary or SqlDbType.Image or SqlDbType.Timestamp or SqlDbType.VarBinary => DbType.Binary,
                SqlDbType.Bit => DbType.Boolean,
                SqlDbType.Char => DbType.AnsiStringFixedLength,
                SqlDbType.Date => DbType.Date,
                SqlDbType.DateTime or SqlDbType.SmallDateTime => DbType.DateTime,
                SqlDbType.DateTime2 => DbType.DateTime2,
                SqlDbType.DateTimeOffset => DbType.DateTimeOffset,
                SqlDbType.Decimal or SqlDbType.Money or SqlDbType.SmallMoney => DbType.Decimal,
                SqlDbType.Float => DbType.Double,
                SqlDbType.Int => DbType.Int32,
                SqlDbType.NChar => DbType.StringFixedLength,
                SqlDbType.NText or SqlDbType.NVarChar or SqlDbType.Xml => DbType.String,
                SqlDbType.Real => DbType.Single,
                SqlDbType.SmallInt => DbType.Int16,
                SqlDbType.Text or SqlDbType.VarChar => DbType.AnsiString,
                SqlDbType.Time => DbType.Time,
                SqlDbType.TinyInt => DbType.Byte,
                SqlDbType.UniqueIdentifier => DbType.Guid,
                _ => DbType.Object
            };
        }
    }
}
