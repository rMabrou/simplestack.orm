using System;
using System.Data;
using System.Data.SqlClient;

namespace SimpleStack.Orm.SqlServer
{
    public class SqlServerTypeMapper : DbTypeMapperBase
    {
        public bool UseDateTime2 { get; set; }

        public override string GetFieldDefinition(DbType type, int? length = null, int? scale = null, int? precision = null)
        {
            var l = length ?? 4000;
            switch (type)
            {
                case DbType.AnsiStringFixedLength:
                case DbType.StringFixedLength:
                    return UseUnicode ? "NCHAR({l})" : "CHAR({l})";
                case DbType.AnsiString:
                case DbType.String:
                case DbType.Object: // Because of null management with Dapper
                    return UseUnicode ? $"NVARCHAR({l})" : $"VARCHAR({l*2})";
                case DbType.Binary:
                    return $"VARBINARY({(length.HasValue ? length.ToString() : "MAX")})";
                case DbType.Boolean:
                    return "BIT";
                case DbType.SByte:
                case DbType.Byte:
                case DbType.Int16:
                case DbType.UInt16:
                    return "smallint";
                case DbType.Int32:
                case DbType.UInt32:
                    return "integer";
                case DbType.Int64:
                case DbType.UInt64:
                    return "bigint";
                case DbType.Single:
                    return "FLOAT";
                case DbType.Double:
                    return "double precision";
                case DbType.Currency:
                    return "money";
                case DbType.Decimal:
                    return $"decimal({precision ?? 18},{scale ?? 0})";
                case DbType.VarNumeric:
                    return $"numeric({precision ?? 18}, {scale ?? 0})";
                case DbType.Date:
                    return "DATE";
                case DbType.DateTime:
                case DbType.DateTime2:
                    return UseDateTime2 ? "datetime2" : "datetime";
                case DbType.DateTimeOffset:
                    return "DATETIMEOFFSET";
                case DbType.Time:
                    return "TIME";
                case DbType.Guid:
                    return "UniqueIdentifier";
                case DbType.Xml:
                    return "xml";
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}