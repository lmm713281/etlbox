﻿using ALE.ETLBox;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace ALE.ETLBoxTest {
    [TestClass]
    public class TestDataFlowBlockTransformation {
        public TestContext TestContext { get; set; }
        public string ConnectionStringParameter => TestContext?.Properties["connectionString"].ToString();
        public string DBNameParameter => TestContext?.Properties["dbName"].ToString();

        [ClassInitialize]
        public static void ClassInit(TestContext testContext) {
            TestHelper.RecreateDatabase(testContext);
            ControlFlow.CurrentDbConnection = new SqlConnectionManager(new ConnectionString(testContext.Properties["connectionString"].ToString()));
            CreateSchemaTask.Create("test");
        }

        [TestInitialize]
        public void TestInit() {
            CleanUpSchemaTask.CleanUp("test");
        }        

        public class MySimpleRow {
            public string Value1 { get; set; }
            public int Value2 { get; set; }
        }


        /*
        * DSBSource (out: object) -> BlockTransformation (in/out: object) -> DBDestination (in: object)
        */
        [TestMethod]
        public void DB_BlockTrans_DB() {
            TableDefinition sourceTableDefinition = CreateSourceTable("test.Source");
            TableDefinition destinationTableDefinition = CreateDestinationTable("test.Destination");

            DBSource<MySimpleRow> source = new DBSource<MySimpleRow>(sourceTableDefinition);
            DBDestination<MySimpleRow> dest = new DBDestination<MySimpleRow>(destinationTableDefinition);
            BlockTransformation<MySimpleRow> block = new BlockTransformation<MySimpleRow>(
                inputData => {
                    return inputData.Select( row => new MySimpleRow() { Value1 = row.Value1, Value2 = 3 }).ToList();
                });
            source.LinkTo(block);
            block.LinkTo(dest);
            source.Execute();
            dest.Wait();
            Assert.AreEqual(3, RowCountTask.Count("test.Destination","Col2 in (3)"));
        }

        private static TableDefinition CreateSourceTable(string tableName) {
            TableDefinition sourceTableDefinition = new TableDefinition(tableName, new List<TableColumn>() {
                new TableColumn("Col1", "nvarchar(100)", allowNulls: false),
                new TableColumn("Col2", "int", allowNulls: true)
            });
            sourceTableDefinition.CreateTable();
            SqlTask.ExecuteNonQuery("Insert demo data", $"insert into {tableName} values('Test1',1)");
            SqlTask.ExecuteNonQuery("Insert demo data", $"insert into {tableName} values('Test2',2)");
            SqlTask.ExecuteNonQuery("Insert demo data", $"insert into {tableName} values('Test3',3)");
            return sourceTableDefinition;
        }

        private static TableDefinition CreateDestinationTable(string tableName) {
            TableDefinition destinationTableDefinition = new TableDefinition(tableName, new List<TableColumn>() {
                new TableColumn("Col1", "nvarchar(100)", allowNulls: false),
                new TableColumn("Col2", "int", allowNulls: true)
            });
            destinationTableDefinition.CreateTable();
            return destinationTableDefinition;
        }



    }

}
