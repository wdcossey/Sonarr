using System;
using System.Collections.Generic;
using System.Linq;
using FluentMigrator.Model;
using FluentMigrator.Infrastructure;
using FluentMigrator.Infrastructure.Extensions;

namespace NzbDrone.Core.Datastore.Migration.Framework
{
    public class SqliteTableDefinition : ICloneable, ICanBeValidated
    {
        public SqliteTableDefinition()
        {
            Columns = new List<ColumnDefinition>();
            ForeignKeys = new List<ForeignKeyDefinition>();
            Indexes = new List<IndexDefinition>();
        }

        public virtual string Name { get; set; }
        public virtual string SchemaName { get; set; }
        public virtual ICollection<ColumnDefinition> Columns { get; set; }
        public virtual ICollection<ForeignKeyDefinition> ForeignKeys { get; set; }
        public virtual ICollection<IndexDefinition> Indexes { get; set; }

        public void CollectValidationErrors(ICollection<string> errors)
        {
            if (String.IsNullOrEmpty(Name))
                errors.Add("ErrorMessages.TableNameCannotBeNullOrEmpty");

            foreach (ColumnDefinition column in Columns)
                column.CollectValidationErrors(errors);

            foreach (IndexDefinition index in Indexes)
                index.CollectValidationErrors(errors);

            foreach (ForeignKeyDefinition fk in ForeignKeys)
                fk.CollectValidationErrors(errors);
        }

        public object Clone()
        {
            return new SqliteTableDefinition
            {
                Name = Name,
                SchemaName = SchemaName,
                Columns = Columns.CloneAll().ToList(),
                Indexes = Indexes.CloneAll().ToList()
            };
        }
    }
}
