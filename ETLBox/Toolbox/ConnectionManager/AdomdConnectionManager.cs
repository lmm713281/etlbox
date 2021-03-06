﻿using Microsoft.AnalysisServices.AdomdClient;
using System;
using System.Data;

namespace ALE.ETLBox {
    public class AdomdConnectionManager : DbConnectionManager<AdomdConnection, AdomdCommand> {

        public AdomdConnectionManager() : base() { }

        public AdomdConnectionManager(ConnectionString connectionString) : base(connectionString) { }

        public override void BulkInsert(IDataReader data, IColumnMappingCollection columnMapping, string tableName) {
            throw new NotImplementedException();
        }

        public override IDbConnectionManager Clone() {
            AdomdConnectionManager clone = new AdomdConnectionManager(ConnectionString) {
                MaxLoginAttempts = this.MaxLoginAttempts
            };
            return clone;

        }

    }
}
