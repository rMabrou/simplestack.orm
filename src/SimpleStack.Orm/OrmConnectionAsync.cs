﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Dapper;
using SimpleStack.Orm.Expressions;
using SimpleStack.Orm.Expressions.Statements.Dynamic;
using SimpleStack.Orm.Expressions.Statements.Typed;

namespace SimpleStack.Orm
{
	public partial class OrmConnection
	{
		public async Task<IEnumerable<dynamic>> SelectAsync(string tableName, Action<DynamicSelectStatement> selectStatement, CommandFlags flags = CommandFlags.Buffered)
		{
			return await SelectAsync(tableName, null, selectStatement, flags);
		}
		
		public async Task<IEnumerable<dynamic>> SelectAsync(string tableName, string schemaName, Action<DynamicSelectStatement> selectStatement, CommandFlags flags = CommandFlags.Buffered)
		{
			DynamicSelectStatement statement = new DynamicSelectStatement(DialectProvider);

			selectStatement(statement.From(tableName, schemaName));

			CommandDefinition cmd = DialectProvider.ToSelectStatement(statement.Statement, flags);
			return await this.QueryAsync(cmd.CommandText, cmd.Parameters, cmd.Transaction, cmd.CommandTimeout,cmd.CommandType);
		}
		
		/// <summary>An OrmConnection method that selects.</summary>
		/// <typeparam name="T">Generic type parameter.</typeparam>
		/// <param name="predicate">The predicate.</param>
		/// <param name="flags"></param>
		/// <returns>A List&lt;T&gt;</returns>
		public async Task<IEnumerable<T>> SelectAsync<T>(Expression<Func<T, bool>> predicate, CommandFlags flags = CommandFlags.Buffered)
		{
			TypedSelectStatement<T> select = new TypedSelectStatement<T>(DialectProvider);
			select.Where(predicate);
			return await this.QueryAsync<T>(DialectProvider.ToSelectStatement(select.Statement,flags));
		}

		/// <summary>An OrmConnection method that selects.</summary>
		/// <typeparam name="T">Generic type parameter.</typeparam>
		/// <param name="expression">The expression.</param>
		/// <param name="flags"></param>
		/// <returns>A List&lt;T&gt;</returns>
		public async Task<IEnumerable<T>> SelectAsync<T>(Action<TypedSelectStatement<T>> expression, CommandFlags flags = CommandFlags.Buffered)
		{
			TypedSelectStatement<T> select = new TypedSelectStatement<T>(DialectProvider);
			expression(select);
			return await this.QueryAsync<T>(DialectProvider.ToSelectStatement(select.Statement,flags));
		}

		/// <summary>An OrmConnection method that selects.</summary>
		/// <typeparam name="T">Generic type parameter.</typeparam>
		/// <returns>A List&lt;T&gt;</returns>
		public async Task<IEnumerable<T>> SelectAsync<T>(CommandFlags flags = CommandFlags.Buffered)
		{
			return await this.QueryAsync<T>(DialectProvider.ToSelectStatement(new TypedSelectStatement<T>(DialectProvider).Statement,flags));
		}

		/// <summary>An OrmConnection method that selects based on a JoinSqlBuilder.</summary>
		/// <typeparam name="T">Generic type parameter.</typeparam>
		/// <typeparam name="V"></typeparam>
//		/// <returns>A List&lt;T&gt;</returns>
//		public async Task<IEnumerable<T>> SelectAsync<T, V>(JoinSqlBuilder<T, V> sqlBuilder)
//		{
//			return await this.QueryAsync<T>(sqlBuilder.ToSql(), sqlBuilder.Parameters);
//		}

		/// <summary>An OrmConnection method that firsts.</summary>
		/// <typeparam name="T">Generic type parameter.</typeparam>
		/// <param name="predicate">The predicate.</param>
		/// <returns>A T.</returns>
		public async Task<T> FirstAsync<T>(Expression<Func<T, bool>> predicate)
		{
			var r = await SelectAsync<T>((x)  =>
			{
				x.Where(predicate);
				x.Limit(1);
			});
			return r.First();
		}
		
		/// <summary>An OrmConnection method that first or default.</summary>
		/// <typeparam name="T">Generic type parameter.</typeparam>
		/// <param name="expression">The expression.</param>
		/// <returns>A T.</returns>
		public async Task<T> FirstAsync<T>(Action<TypedSelectStatement<T>> expression)
		{
			var r = await SelectAsync<T>((x) =>
			{
				expression(x);
				x.Limit(1);
			});
			return r.First();
		}

		/// <summary>An OrmConnection method that first or default.</summary>
		/// <typeparam name="T">Generic type parameter.</typeparam>
		/// <param name="predicate">The predicate.</param>
		/// <returns>A T.</returns>
		public async Task<T> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate)
		{
			var r = await SelectAsync<T>(x =>
			{
				x.Where(predicate);
				x.Limit(1);
			});
			return r.FirstOrDefault();
		}

		/// <summary>An OrmConnection method that first or default.</summary>
		/// <typeparam name="T">Generic type parameter.</typeparam>
		/// <param name="expression">The expression.</param>
		/// <returns>A T.</returns>
		public async Task<T> FirstOrDefaultAsync<T>(Action<TypedSelectStatement<T>> expression)
		{
			var r = await SelectAsync<T>((x) =>
			{
				expression(x);
				x.Limit(1);
			});
			return r.FirstOrDefault();
		}

		/// <summary>An OrmConnection method that gets a scalar.</summary>
		/// <typeparam name="T">   Generic type parameter.</typeparam>
		/// <typeparam name="TKey">Type of the key.</typeparam>
		/// <param name="field"> The field.</param>
		/// <returns>The scalar.</returns>
		public async Task<TKey> GetScalarAsync<T, TKey>(Expression<Func<T, TKey>> field)
		{
			//int maxAgeUnder50 = db.Scalar<Person, int>(x => Sql.Max(x.Age));
			TypedSelectStatement<T> select = new TypedSelectStatement<T>(DialectProvider);
			select.Select(field);
			return await this.ExecuteScalarAsync<TKey>(DialectProvider.ToSelectStatement(select.Statement,CommandFlags.None));
		}

		/// <summary>An OrmConnection method that gets a scalar.</summary>
		/// <typeparam name="T">   Generic type parameter.</typeparam>
		/// <typeparam name="TKey">Type of the key.</typeparam>
		/// <param name="field">    The field.</param>
		/// <param name="predicate">The predicate.</param>
		/// <returns>The scalar.</returns>
		public async Task<TKey> GetScalarAsync<T, TKey>(Expression<Func<T, TKey>> field, Expression<Func<T, bool>> predicate)
		{
			//int maxAgeUnder50 = db.Scalar<Person, int>(x => Sql.Max(x.Age), x => x.Age < 50);
			TypedSelectStatement<T> select = new TypedSelectStatement<T>(DialectProvider);
			select.Select(field);
			select.Where(predicate);
			return await this.ExecuteScalarAsync<TKey>(DialectProvider.ToSelectStatement(select.Statement,CommandFlags.None));
		}

		public async Task<long> CountAsync<T>(Action<TypedSelectStatement<T>> expression)
		{
			TypedSelectStatement<T> select = new TypedSelectStatement<T>(DialectProvider);
			expression(select);

			return await this.ExecuteScalarAsync<long>(DialectProvider.ToCountStatement(select.Statement,CommandFlags.None));
		}
		
		public async Task<long> CountAsync(string tableName, Action<DynamicSelectStatement> expression)
		{
			return await CountAsync(tableName, null, expression);
		}
		
		public async Task<long> CountAsync(string tableName, string schemaName, Action<DynamicSelectStatement> expression)
		{
			DynamicSelectStatement select = new DynamicSelectStatement(DialectProvider);
			expression(select.From(tableName,schemaName));

			return await this.ExecuteScalarAsync<long>(DialectProvider.ToCountStatement(select.Statement,CommandFlags.None));
		}
		
		/// <summary>
		///    An OrmConnection method that counts the given database connection.
		/// </summary>
		/// <typeparam name="T">Generic type parameter.</typeparam>
		/// <param name="expression">The expression.</param>
		/// <returns>A long.</returns>
		public async Task<long> CountAsync<T>(Expression<Func<T, bool>> expression)
		{
			return await CountAsync<T>( e => e.Where(expression) );
		}

		/// <summary>
		///    An OrmConnection method that counts the given database connection.
		/// </summary>
		/// <typeparam name="T">Generic type parameter.</typeparam>
		/// <returns>A long.</returns>
		public async Task<long> CountAsync<T>()
		{
			return await CountAsync<T>(e => {});
		}

		public async Task<int> UpdateAsync<T>(T model)
		{
			var s = new TypedUpdateStatement<T>(DialectProvider);
			s.ValuesOnly(model);
			var cmd = DialectProvider.ToUpdateStatement(s.Statement,CommandFlags.None);
			return await this.ExecuteScalarAsync<int>(cmd);
		}

		public async Task<int> UpdateAsync<T, TKey>(T model, Expression<Func<T, TKey>> onlyFields)
		{
			var s = new TypedUpdateStatement<T>(DialectProvider);
			s.ValuesOnly(model, onlyFields);
			var cmd = DialectProvider.ToUpdateStatement(s.Statement,CommandFlags.None);
			return await this.ExecuteScalarAsync<int>(cmd);
		}

		public async Task<int> UpdateAllAsync<T, TKey>(object obj, Expression<Func<T, TKey>> onlyField, Expression<Func<T, bool>> where = null)
		{
			var s = new TypedUpdateStatement<T>(DialectProvider);
			if (where != null)
			{
				s.Where(where);
			}

			s.Values(obj, onlyField);
			
			var cmd = DialectProvider.ToUpdateStatement(s.Statement,CommandFlags.None);
			return await this.ExecuteScalarAsync<int>(cmd);
		}

		public async Task<int> InsertAsync<T>(T obj)
		{
			try
			{
				var insertStatement = new TypedInsertStatement<T>(DialectProvider);
				insertStatement.Values(obj,new List<string>());
			
				await this.ExecuteScalarAsync<int>(DialectProvider.ToInsertStatement(insertStatement.Statement, CommandFlags.None));
				return 1;
			}
			catch (Exception e)
			{
				throw new OrmException(e.Message,e);
			}
		}

		/// <summary>An OrmConnection method that inserts all.</summary>
		/// <typeparam name="T">Generic type parameter.</typeparam>
		/// <param name="objs">  The objects.</param>
		public async Task<int> InsertAsync<T>(IEnumerable<T> objs)
		{
			var count = 0;
			foreach (var t in objs)
			{
				//TODO: Optimize this only generating query once and use different parameters
				var insertStatement = new TypedInsertStatement<T>(DialectProvider);
				insertStatement.Values(t, new List<string>());
				
				await this.ExecuteScalarAsync<int>(DialectProvider.ToInsertStatement(insertStatement.Statement, CommandFlags.None));
				count++;
			}

			return count;
		}

		/// <summary>An OrmConnection method that inserts an only.</summary>
		/// <typeparam name="T">Generic type parameter.</typeparam>
		/// <param name="obj">       The object.</param>
		/// <param name="onlyFields">The only fields.</param>
		public async Task<int> InsertOnlyAsync<T,TKey>(T obj, Expression<Func<T, TKey>> onlyFields)
			where T : new()
		{
			var insertStatement = new TypedInsertStatement<T>(DialectProvider);
			insertStatement.Values(obj, onlyFields);

			return await this.ExecuteScalarAsync<int>(DialectProvider.ToInsertStatement(insertStatement.Statement, CommandFlags.None));
		}

		/// <summary>An OrmConnection method that inserts an only.</summary>
		/// <typeparam name="T">Generic type parameter.</typeparam>
		/// <param name="obj">       The object.</param>
		/// <param name="onlyFields">The only fields.</param>
		public async Task<int> InsertOnlyAsync<T>(T obj, Action<TypedInsertStatement<T>> statement) where T : new()
		{
			var insertStatement = new TypedInsertStatement<T>(DialectProvider);
			statement(insertStatement);
			
			return await this.ExecuteScalarAsync<int>(DialectProvider.ToInsertStatement(insertStatement.Statement, CommandFlags.None));
		}

		/// <summary>An OrmConnection method that deletes this object.</summary>
		/// <typeparam name="T">Generic type parameter.</typeparam>
		/// <param name="where"> The where.</param>
		/// <returns>An int.</returns>
		public async Task<int> DeleteAllAsync<T>(Expression<Func<T, bool>> where = null)
		{
			return await DeleteAllAsync<T>(x => { x.Where(where); });
		}

		/// <summary>An OrmConnection method that deletes this object.</summary>
		/// <typeparam name="T">Generic type parameter.</typeparam>
		/// <param name="where"> The where.</param>
		/// <returns>An int.</returns>
		public async Task<int> DeleteAllAsync<T>(Action<TypedWhereStatement<T>> where)
		{
			var s = new TypedDeleteStatement<T>(DialectProvider);
			where(s);
			return await this.ExecuteScalarAsync<int>(DialectProvider.ToDeleteStatement(s.Statement));
		}

		/// <summary>An OrmConnection method that deletes this object.</summary>
		/// <typeparam name="T">Generic type parameter.</typeparam>
		/// <param name="where"> The where.</param>
		/// <returns>An int.</returns>
		public async Task<int> DeleteAsync<T>(T obj)
		{
			var s = new TypedDeleteStatement<T>(DialectProvider);
			s.AddPrimaryKeyWhereCondition(obj);
			
			return await this.ExecuteScalarAsync<int>(DialectProvider.ToDeleteStatement(s.Statement));
		}
		
		public async Task CreateTableAsync<T>(bool dropIfExists)
		{
			if(!dropIfExists && await TableExistsAsync<T>())
				throw new OrmException("Table already exists");

			var tableType = typeof (T);
			await CreateTableAsync(dropIfExists, tableType);
		}

		public async Task CreateTableIfNotExistsAsync<T>()
		{
			var tableType = typeof(T);
			await CreateTableAsync(false, tableType);
		}
		
		public async Task CreateSchemaIfNotExistsAsync(string schemaName)
		{
			await this.ExecuteScalarAsync(DialectProvider.GetCreateSchemaStatement(schemaName, true));
		}
		public async Task CreateSchemaAsync(string schemaName)
		{
			await this.ExecuteScalarAsync(DialectProvider.GetCreateSchemaStatement(schemaName, false));
		}

		public async Task<bool> TableExistsAsync<T>()
		{
			var tableModelDef = typeof(T).GetModelDefinition();
			return await Task.Run(() => 
				DialectProvider.DoesTableExist(this,
					tableModelDef.Alias ?? tableModelDef.ModelName,
					tableModelDef.Schema));
		}

		public async Task<bool> TableExistsAsync(string tableName, string schema = null)
		{
			return await Task.Run(() =>
				DialectProvider.DoesTableExist(this, tableName, schema));
		}

		public async Task<bool> DropTableIfExistsAsync<T>()
		{
			if (await TableExistsAsync<T>())
			{
				var tableModelDef = typeof(T).GetModelDefinition();
				await DropTableAsync(tableModelDef);
			}
			return false;
		}

		/// <summary>An IDbCommand method that creates a table.</summary>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		/// <param name="overwrite">true to overwrite, false to preserve.</param>
		/// <param name="modelType">Type of the model.</param>
		private async Task CreateTableAsync(bool overwrite, Type modelType)
		{
			var modelDef = modelType.GetModelDefinition();

			var dialectProvider = DialectProvider;
			var tableName = dialectProvider.NamingStrategy.GetTableName(modelDef.ModelName);
			var tableExists = dialectProvider.DoesTableExist(this, tableName, modelDef.Schema);

			if (overwrite && tableExists)
			{
				await DropTableAsync(modelDef);
				tableExists = false;
			}

			if (!tableExists)
			{
				await this.ExecuteAsync(dialectProvider.ToCreateTableStatement(modelDef));

				var sqlIndexes = dialectProvider.ToCreateIndexStatements(modelDef);
				foreach (var sqlIndex in sqlIndexes)
				{
					await this.ExecuteAsync(sqlIndex);
				}

				var sequenceList = dialectProvider.SequenceList(modelDef);
				if (sequenceList.Count > 0)
				{
					foreach (var seq in sequenceList)
					{
						if (dialectProvider.DoesSequenceExist(this, seq) == false)
						{
							var seqSql = dialectProvider.ToCreateSequenceStatement(modelDef, seq);
							await this.ExecuteAsync(seqSql);
						}
					}
				}
				else
				{
					var sequences = dialectProvider.ToCreateSequenceStatements(modelDef);
					foreach (var seq in sequences)
					{
						await this.ExecuteAsync(seq);
					}
				}
			}
		}

		/// <summary>Drop table (Table MUST exists).</summary>
		/// <param name="modelDef">The model definition.</param>
		private async Task DropTableAsync(ModelDefinition modelDef)
		{
			var dropTableFks = DialectProvider.GetDropForeignKeyConstraints(modelDef);
			if (!string.IsNullOrEmpty(dropTableFks))
			{
				await this.ExecuteAsync(dropTableFks);
			}
			await this.ExecuteAsync(DialectProvider.GetDropTableStatement(modelDef));
		}

        public async Task<IEnumerable<ITableDefinition>> GetTablesInformationAsync(string schemaName = null, bool includeViews = false)
        {
            return await DialectProvider.GetTableDefinitions(DbConnection, schemaName, includeViews);
        }
        
        public async Task<IEnumerable<IColumnDefinition>> GetTableColumnsInformationAsync(string tableName, string schemaName = null)
        {
            return await Task.Run(() =>  DialectProvider.GetTableColumnDefinitions(DbConnection, tableName, schemaName));
        }
	}
}