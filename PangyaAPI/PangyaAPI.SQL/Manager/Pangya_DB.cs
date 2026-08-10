using PangyaAPI.SQL.Manager;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using response = PangyaAPI.SQL.Response;
using PangyaAPI.SQL.EntityFramework;

namespace PangyaAPI.SQL
{

    public abstract class Pangya_DB
    {
        private static readonly call_db_cmd_st cdcs = new call_db_cmd_st();

        private IRelationalCommandExecutor _executor;
        public Pangya_DB() { loadIni(); }
        [Obsolete("The waiter parameter has no effect. Database commands execute synchronously in NormalManager.")]
        public Pangya_DB(bool wait = false)
        {
            loadIni();
        }

        public bool loadIni()
        {
            if (_executor != null)
            {
                return false;
            }
            try
            {
                _executor = DatabaseConfiguration.Executor;
            }
            catch (Exception ex)
            {
                PangyaLog.Write("[database::loadIni][Error] " + ex.Message + "]", LogDestination.Console);
                throw new exception("[database::loadIni][Error] " + ex.Message,
                    ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB, 0, 0));
            }
            return _executor != null;
        }
        private bool logExecuteCmds(string _name)
        {
            var v_cmds = cdcs.loadCmds();

            if (!v_cmds.TryGetValue(_name, out var value))
            {
                v_cmds[_name] = "yes";
                cdcs.saveCmds(v_cmds);
                return true; // show log
            }
            else if (value == "no")
            {
                v_cmds[_name] = "yes";
                cdcs.saveCmds(v_cmds);
                return true; // show log
            }

            return false;
        }

        public void exec()
        {
            uint num_result = 0;
            try
            {
                response r = null;
                if ((r = prepareConsulta()) != null)
                {
                    var results = r.getResultSet();
                    foreach (var _result in results)
                    {
                        lineResult(_result.getFirstLine(), num_result);
                        num_result++;
                    }
                    if (results.Count == 0)
                    {
                        lineResultNull();
                    }
                    r = null;
                }
                else
                {
                    PangyaLog.Write("[Pangya_DB::" + _getName + "::exec][Error] return prepareConsulta is null.", LogDestination.GeneralFile | LogDestination.Console);
                }
            }
            catch (exception e)
            {
                m_exception = e;
                PangyaLog.Write("[pangya_db::" + _getName + "::exec][Error] " + e.getFullMessageError(), LogDestination.GeneralFile | LogDestination.Console);
            }

             
            if (DatabaseConfiguration.Current.LogCommands && logExecuteCmds(_getName))
                PangyaLog.Write("[" + _getName + "::exec][Sucess] was Executed.", LogDestination.GeneralFile | LogDestination.Console);
        }

        public virtual exception getException() => m_exception ?? new exception("");

        public virtual response _update(string _query) { return _executor.ExecuteText(_query, Array.Empty<RelationalParameter>()); }

        public virtual response _delete(string _query) { return _executor.ExecuteText(_query, Array.Empty<RelationalParameter>()); }

        public virtual response consulta(string _query) { return _executor.ExecuteText(_query, Array.Empty<RelationalParameter>()); }

        public virtual response procedure(string _name, params object[] values)
        {
            // Legacy repository commands use a single empty string to mean that a
            // stored procedure has no arguments. Do not turn that sentinel into @p0.
            if (values?.Length == 1 && values[0] is string value && value.Length == 0)
                values = Array.Empty<object>();

            var parameters = values?.Select((value, index) =>
                new RelationalParameter("@p" + index, value ?? DBNull.Value)).ToArray()
                ?? Array.Empty<RelationalParameter>();
            return _executor.ExecuteStoredProcedure(_name, parameters);
        }
        //others
        public virtual response deleteWithParams(string _proc_name, string[] parameter = null, SqlDbType[] tipo = null, object[] valor = null, ParameterDirection Direcao = ParameterDirection.Input) { return ExecuteWithParams(_proc_name, parameter, tipo, valor, Direcao, false); }
        public virtual response consultaeWithParams(string _proc_name, string[] parameter = null, SqlDbType[] tipo = null, object[] valor = null, ParameterDirection Direcao = ParameterDirection.Input) { return ExecuteWithParams(_proc_name, parameter, tipo, valor, Direcao, false); }
        public virtual response _updateWithParams(string _proc_name, string[] parameter = null, SqlDbType[] tipo = null, object[] valor = null, ParameterDirection Direcao = ParameterDirection.Input) { return ExecuteWithParams(_proc_name, parameter, tipo, valor, Direcao, false); }
        public virtual response procedureWithParams(string _proc_name, string[] parameter = null, SqlDbType[] tipo = null, object[] valor = null, ParameterDirection Direcao = ParameterDirection.Input) { return ExecuteWithParams(_proc_name, parameter, tipo, valor, Direcao, true); }

        protected PangyaDbContext createContext()
        {
            return new PangyaDbContextFactory(DatabaseConfiguration.Current).CreateDbContext();
        }

        protected static response responseFromRows(IEnumerable<object[]> rows)
        {
            var result = new response();
            foreach (var values in rows)
            {
                var table = new DataTable();
                for (var index = 0; index < values.Length; index++)
                    table.Columns.Add("Column" + index, typeof(object));
                var row = table.Rows.Add(values);
                result.addResultSet(new Result_Set(
                    (uint)Result_Set.STATE_TYPE.HAVE_DATA,
                    1,
                    (uint)values.Length,
                    row));
            }
            result.setRowsAffected(0);
            return result;
        }

        protected static response responseFromRowsAffected(int rowsAffected)
        {
            var result = new response();
            result.setRowsAffected(rowsAffected);
            return result;
        }

        private response ExecuteWithParams(
            string commandName,
            string[] parameter,
            SqlDbType[] tipo,
            object[] valor,
            ParameterDirection direction,
            bool storedProcedure)
        {
            parameter ??= Array.Empty<string>();
            tipo ??= Array.Empty<SqlDbType>();
            valor ??= Array.Empty<object>();
            if (parameter.Length != valor.Length || (tipo.Length != 0 && tipo.Length != valor.Length))
                throw new ArgumentException("Database parameter names, types, and values must have matching lengths.");

            var parameters = new RelationalParameter[valor.Length];
            for (var index = 0; index < valor.Length; index++)
            {
                var name = string.IsNullOrWhiteSpace(parameter[index]) ? "@p" + index : parameter[index];
                parameters[index] = new RelationalParameter(
                    name,
                    valor[index] ?? DBNull.Value,
                    tipo.Length == 0 ? null : tipo[index],
                    direction);
            }

            return storedProcedure
                ? _executor.ExecuteStoredProcedure(commandName, parameters)
                : _executor.ExecuteText(commandName, parameters);
        }

        public virtual void checkColumnNumber(uint _number_cols1)
        {
            if (_number_cols1 <= 0)
                throw new exception("[Pangya_DB::" + _getName + "::checkColumnNumber][Error] numero de colunas retornada pela consulta sao diferente do esperado.");
        }
        public virtual void checkColumnNumber(uint _number_cols1, uint _number_cols2)
        {
            if (_number_cols1 <= 0 || _number_cols1 != _number_cols2)
                throw new exception("[Pangya_DB::" + _getName + "::checkColumnNumber][Error] numero de colunas retornada pela consulta sao diferente do esperado.");
        }

        public virtual void checkResponse(response r, string _exception_msg)
        {
            if (r == null || (r.getNumResultSet() <= 0 && r.getRowsAffected() == -1))
                throw new exception("[Pangya_DB::" + _getName + "::checkResponse][Error] " + _exception_msg, ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.PANGYA_DB, 0, 0));
        }
        public uint STDA_MAKE_ERROR(STDA_ERROR_TYPE code, uint err_code, uint _err_sys) => ExceptionError.STDA_MAKE_ERROR_TYPE(code, err_code, _err_sys);
        protected abstract void lineResult(ctx_res _result, uint _index_result);
        protected virtual void lineResultNull() { }//nao em dados, so confirma a execucao
        protected abstract response prepareConsulta();

        protected virtual string _getName { get => GetType().Name; }

        public static string _formatDate(DateTime date)
        {
            return UtilTime.FormatDate(date);
        }
        public static string formatDateLocal(long date)
        {
            return UtilTime.FormatDateLocal(date);
        }
        public static bool is_valid_c_string(object value)
        {
            if (value == null || value is DBNull || (value is string && string.IsNullOrEmpty((string)value)))
            {
                return false;
            }
            var _ptr_c_string = Convert.ToString(value);
            return _ptr_c_string != null && _ptr_c_string[0] != 0;
        }

        public static void STRCPY_TO_MEMORY_FIXED_SIZE(ref string v1, int size, object v2)
        {
            @v1 = Convert.ToString(v2);
        }


        public uint IFNULL(object value)
        {
            if (value == null || value is DBNull)
            {
                return 0;
            }

            try
            {
                if (value is int intValue && intValue == -1)
                {
                    return uint.MaxValue;
                }
                return Convert.ToUInt32(value);
            }
            catch
            {
                throw new InvalidCastException($"[{_getName}::IFNULL][Error] The provided value cannot be converted to uint.");
            }
        }

        public T IFNULL<T>(object value)
        {
            if (value == null || value is DBNull)
            {
                return default; // Retorna o valor padrão de T (ex: 0 para int, null para string)
            }

            try
            {
                return (T)Convert.ChangeType(value, typeof(T)); // Conversão segura para o tipo T
            }
            catch (Exception ex)
            {
                throw new InvalidCastException($"[{_getName}::IFNULL][Error] The provided value cannot be converted to {typeof(T).Name}.", ex);
            }
        }

        public static DateTime? _translateDate(object value)
        {
            if (value == null || value is DBNull)
                return null;

            if (value is DateTime dateTime)
                return dateTime;

            if (value is DateTimeOffset dateTimeOffset)
                return dateTimeOffset.DateTime;

            if (value is TimeSpan time)
                return DateTime.Today.Add(time);

            var text = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(text))
                return null;

            if (DateTime.TryParse(text,
                                  System.Globalization.CultureInfo.InvariantCulture,
                                  System.Globalization.DateTimeStyles.AllowWhiteSpaces,
                                  out var result)
                || DateTime.TryParse(text,
                                     System.Globalization.CultureInfo.CurrentCulture,
                                     System.Globalization.DateTimeStyles.AllowWhiteSpaces,
                                     out result))
                return result;

            return null;
        }
        protected exception m__exception { get; set; }
        public exception m_exception { get => m__exception; set => m__exception = value; }
    }
}
