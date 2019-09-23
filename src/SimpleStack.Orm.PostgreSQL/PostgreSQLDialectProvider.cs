﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Dapper;
using Npgsql;
using SimpleStack.Orm.Attributes;
using SimpleStack.Orm.Expressions;

namespace SimpleStack.Orm.PostgreSQL
{
    /// <summary>A postgre SQL dialect provider.</summary>
    public class PostgreSQLDialectProvider : DialectProviderBase
    {
        /// <summary>
        ///     Prevents a default instance of the NServiceKit.OrmLite.PostgreSQL.PostgreSQLDialectProvider
        ///     class from being created.
        /// </summary>
        public PostgreSQLDialectProvider() : base(new PostgreSQLTypeMapper())
        {
            NamingStrategy = new PostgreSqlNamingStrategy();
            base.SelectIdentitySql = "SELECT LASTVAL()";
            ParamPrefix = ":";
        }

        /// <summary>Creates a connection.</summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="options">         Options for controlling the operation.</param>
        /// <returns>The new connection.</returns>
        public override DbConnection CreateIDbConnection(string connectionString)
        {
            return new NpgsqlConnection(connectionString);
        }

        /// <summary>Gets column definition.</summary>
        /// <param name="fieldName">    Name of the field.</param>
        /// <param name="fieldType">    Type of the field.</param>
        /// <param name="isPrimaryKey"> true if this object is primary key.</param>
        /// <param name="autoIncrement">true to automatically increment.</param>
        /// <param name="isNullable">   true if this object is nullable.</param>
        /// <param name="fieldLength">  Length of the field.</param>
        /// <param name="scale">        The scale.</param>
        /// <param name="defaultValue"> The default value.</param>
        /// <returns>The column definition.</returns>
        public override string GetColumnDefinition(
            string fieldName,
            Type fieldType,
            bool isPrimaryKey,
            bool autoIncrement,
            bool isNullable,
            int? fieldLength,
            int? scale,
            object defaultValue)
        {
            string fieldDefinition = null;
            if (autoIncrement)
            {
                if (fieldType == typeof(long))
                {
                    fieldDefinition = "bigserial";
                }
                else if (fieldType == typeof(int))
                {
                    fieldDefinition = "serial";
                }
            }
            else
            {
                fieldDefinition = GetColumnTypeDefinition(fieldType, fieldName, fieldLength);
            }

            var sql = new StringBuilder();
            sql.AppendFormat("{0} {1}", GetQuotedColumnName(fieldName), fieldDefinition);
            if (isPrimaryKey && autoIncrement)
            {
                sql.Append(" PRIMARY KEY");
            }
            else
            {
                if (isNullable)
                {
                    sql.Append(" NULL");
                }
                else
                {
                    sql.Append(" NOT NULL");
                }
            }

            if (defaultValue != null)
            {
                sql.AppendFormat(DefaultValueFormat, GetDefaultValueDefinition(defaultValue));
            }

            return sql.ToString();
        }

        /// <summary>Query if 'dbCmd' does table exist.</summary>
        /// <param name="connection">    The database command.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <returns>true if it succeeds, false if it fails.</returns>
        public override bool DoesTableExist(IDbConnection connection, string tableName)
        {
            var result = connection.ExecuteScalar<long>(
                @"SELECT COUNT(*) FROM pg_class
                                                  LEFT JOIN pg_namespace n ON n.oid = pg_class.relnamespace
                                               WHERE nspname = current_schema()
                                               AND  relname = :table;",
                new {table = tableName});

            return result > 0;
        }

        /// <summary>Gets quoted table name.</summary>
		/// <param name="modelDef">The model definition.</param>
		/// <returns>The quoted table name.</returns>
		public override string GetQuotedTableName(ModelDefinition modelDef)
		{
			if (!modelDef.IsInSchema)
			{
				return base.GetQuotedTableName(modelDef);
			}
			string escapedSchema = modelDef.Schema.Replace(".", "\".\"");
			return string.Format("\"{0}\".\"{1}\"", escapedSchema, base.NamingStrategy.GetTableName(modelDef.ModelName));
		}

		/// <summary>
		/// based on Npgsql2's source: Npgsql2\src\NpgsqlTypes\NpgsqlTypeConverters.cs.
		/// </summary>
		/// <param name="NativeData">.</param>
		/// <returns>A binary represenation of this object.</returns>
		/// ### <param name="TypeInfo">        .</param>
		/// ### <param name="ForExtendedQuery">.</param>
		internal static String ToBinary(Object NativeData)
		{
			Byte[] byteArray = (Byte[])NativeData;
			StringBuilder res = new StringBuilder(byteArray.Length * 5);
			foreach (byte b in byteArray)
				if (b >= 0x20 && b < 0x7F && b != 0x27 && b != 0x5C)
					res.Append((char)b);
				else
					res.Append("\\\\")
						.Append((char)('0' + (7 & (b >> 6))))
						.Append((char)('0' + (7 & (b >> 3))))
						.Append((char)('0' + (7 & b)));
			return res.ToString();
		}

		public override string GetDropTableStatement(ModelDefinition modelDef)
		{
			return "DROP TABLE " + GetQuotedTableName(modelDef) + " CASCADE";
		}

		public override string GetDatePartFunction(string name, string quotedColName)
		{
			return $"date_part('{name.ToLower()}', {quotedColName})";
		}

        private class PostgreSqlTableDefinition
        {
            public string Table_Name { get; set; }
        }

        public override IEnumerable<IColumnDefinition> GetTableColumnDefinitions(IDbConnection connection, string tableName, string schemaName = null)
        {
            string sqlQuery = "SELECT * FROM information_schema.columns WHERE lower(table_name) = @tableName ";
            if (string.IsNullOrWhiteSpace(schemaName))
            {
                sqlQuery += " AND table_schema = current_schema()";
            }
            else
            {
                sqlQuery += " AND table_schema = @SchemaName";
            }

            // ReSharper disable StringLiteralTypo
            var pks = connection.Query($@"SELECT a.attname
                FROM   pg_index i
                JOIN   pg_attribute a ON a.attrelid = i.indrelid
                                     AND a.attnum = ANY(i.indkey)
                WHERE  i.indrelid = '{tableName}'::regclass
                AND    i.indisprimary;").ToArray();
            // ReSharper enable StringLiteralTypo

            foreach (var c in connection.Query<ColumnInformationSchema>(sqlQuery, new { TableName = tableName.ToLower(), schemaName }))
            {
                yield return new ColumnDefinition
                             {
                                 Name = c.column_name,
                                 PrimaryKey = pks.Any(x => x.attname == c.column_name),
                                 Length = c.character_maximum_length,
                                 DefaultValue = c.column_default,
                                 Definition = c.data_type,
                                 Nullable = c.is_nullable == "YES",
                                 Precision = c.numeric_precision,
                                 Scale = c.numeric_scale,
                                 DbType = GetDbType(c)
                };
            }
        }

        protected virtual DbType GetDbType(ColumnInformationSchema c)
        {
            switch (c.data_type)
            {
                case "character":
                    return DbType.String;
                case "character varying":
                    return DbType.StringFixedLength;
                case "text":
                    return DbType.String;
                case "boolean":
                case "bit":
                    return DbType.Boolean;
                case "uuid" :
                    return DbType.Guid;
                case "smallint":
                    return DbType.Int16;
                case "integer":
                    return DbType.Int32;
                case "bigint":
                    return DbType.Int64;
                case "real":
                    return DbType.Single;
                case "double precision":
                    return DbType.Double;
                case "bytea":
                    return DbType.Binary;
                case "time without time zone":
                    return DbType.Time;
                case "date":
                case "timestamp without time zone":
                    return DbType.DateTime;
                case "timestamp whith time zone":
                    return DbType.DateTimeOffset;
                case "money":
                    return DbType.Currency;
                default:
                    return DbType.Object;
            }
        }

        public override IEnumerable<ITableDefinition> GetTableDefinitions(IDbConnection connection, string schemaName = null)
        {
            string sqlQuery = "SELECT * FROM information_schema.tables WHERE table_type = 'BASE TABLE'";
            
            if (string.IsNullOrWhiteSpace(schemaName))
            {
                sqlQuery += " AND table_schema = current_schema()";
            }
            else
            {
                sqlQuery += " AND table_schema = @SchemaName ";
            }
            
            foreach (var table in connection.Query(sqlQuery, new { SchemaName = schemaName }))
            {
                yield return new TableDefinition
                {
                    Name = table.table_name,
                    SchemaName = table.table_schema
                };
            }
        }

        public override string BindOperand(ExpressionType e, bool isLogical)
        {
	        return e == ExpressionType.ExclusiveOr ? "#" : base.BindOperand(e, isLogical);
        }
    }
}
