﻿using Dotmim.Sync.Enumerations;
using Dotmim.Sync.SQLite;
using Dotmim.Sync.SqlServer;
using Dotmim.Sync.Test.SqlUtils;
using Dotmim.Sync.Tests.Misc;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Dotmim.Sync.Tests
{
    public class SyncDirectionFixture : IDisposable
    {
        private HelperDB helperDb = new HelperDB();
        private string serverDbName = "AdventureWorksLT2012";
        private string client1DbName = "AdventureWorksLT2012ClientDirection";
        public String ClientSQLiteConnectionString { get; set; }
        public string ClientSQLiteFilePath => Path.Combine(Directory.GetCurrentDirectory(), "AdventureWorksLT2012Sqlite.db");

        public String ServerConnectionString => HelperDB.GetDatabaseConnectionString(serverDbName);
        public String Client1ConnectionString => HelperDB.GetDatabaseConnectionString(client1DbName);

        public string[] Tables => new string[]
            {   "SalesLT.ProductCategory",
                "SalesLT.ProductModel",
                "SalesLT.ProductDescription",
                "SalesLT.Product",
                "SalesLT.ProductModelProductDescription",
                "SalesLT.Customer",
                "SalesLT.Address",
                "SalesLT.CustomerAddress",
                "SalesLT.SalesOrderHeader",
                "SalesLT.SalesOrderDetail",
                "ErrorLog"
            };

        public SyncDirectionFixture()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            var builder = new SQLiteConnectionStringBuilder { DataSource = ClientSQLiteFilePath };
            this.ClientSQLiteConnectionString = builder.ConnectionString;

            if (File.Exists(ClientSQLiteFilePath))
                File.Delete(ClientSQLiteFilePath);

            var backup = Path.Combine(Directory.GetCurrentDirectory(), "Backup", "AdventureWorksLT2012.bak");
            // create databases
            helperDb.RestoreDatabase(serverDbName, backup);
            helperDb.CreateDatabase(client1DbName);
        }
        public void Dispose()
        {
            helperDb.DeleteDatabase(serverDbName);
            helperDb.DeleteDatabase(client1DbName);

            GC.Collect();
            GC.WaitForPendingFinalizers();

            if (File.Exists(ClientSQLiteFilePath))
                File.Delete(ClientSQLiteFilePath);
        }
    }

    [TestCaseOrderer("Dotmim.Sync.Tests.Misc.PriorityOrderer", "Dotmim.Sync.Tests")]
    public class SyncDirectionTests : IClassFixture<SyncDirectionFixture>
    {
        SqlSyncProvider serverProvider;
        SqlSyncProvider clientProvider;
        SQLiteSyncProvider sqliteClientProvider;
        SyncDirectionFixture fixture;
        SyncAgent agent;

        public SyncDirectionTests(SyncDirectionFixture fixture)
        {
            this.fixture = fixture;

        }

        [Fact, TestPriority(1)]
        public async Task Initialize()
        {
            serverProvider = new SqlSyncProvider(fixture.ServerConnectionString);
            clientProvider = new SqlSyncProvider(fixture.Client1ConnectionString);
            var simpleConfiguration = new SyncConfiguration(fixture.Tables);

            foreach (var dmTable in simpleConfiguration)
                dmTable.SyncDirection = SyncDirection.UploadOnly;

            agent = new SyncAgent(clientProvider, serverProvider, simpleConfiguration);

            var session = await agent.SynchronizeAsync();

            Assert.Equal(0, session.TotalChangesDownloaded);
            Assert.Equal(0, session.TotalChangesUploaded);
        }

        

    }
}
