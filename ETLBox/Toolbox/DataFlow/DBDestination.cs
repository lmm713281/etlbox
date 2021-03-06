﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks.Dataflow;

namespace ALE.ETLBox {
    public class DBDestination<TInput> : GenericTask, ITask, IDataFlowDestination<TInput> {
        /* ITask Interface */
        public override string TaskType { get; set; } = "DF_DBDEST";
        public override string TaskName => $"Dataflow: Write Data batchwise into table {DestinationTableDefinition.Name}";
        public override void Execute() { throw new Exception("Dataflow destinations can't be started directly"); }

        /* Public properties */
        public TableDefinition DestinationTableDefinition { get; set; }
        public Func<TInput[], TInput[]> BeforeBatchWrite { get; set; }
        
        public ITargetBlock<TInput> TargetBlock => Buffer;

        /* Private stuff */
        int BatchSize { get; set; } = DEFAULT_BATCH_SIZE;
        const int DEFAULT_BATCH_SIZE = 100000;
        internal BatchBlock<TInput> Buffer { get; set; }
        internal ActionBlock<TInput[]> TargetAction { get; set; }
        NLog.Logger NLogger { get; set; }
        TypeInfo TypeInfo { get; set; }
        public DBDestination() {
            InitObjects(DEFAULT_BATCH_SIZE);
            
        }

        public DBDestination(int batchSize) {
            BatchSize = batchSize;
            InitObjects(batchSize);
        }

        public DBDestination(TableDefinition tableDefinition) {
            DestinationTableDefinition = tableDefinition;
            InitObjects(DEFAULT_BATCH_SIZE);
        }

        public DBDestination(TableDefinition tableDefinition, int batchSize) {
            DestinationTableDefinition = tableDefinition;
            BatchSize = batchSize;            
            InitObjects(batchSize);
        }

        public DBDestination(string name, TableDefinition tableDefinition, int batchSize) {
            this.TaskName = name;
            DestinationTableDefinition = tableDefinition;
            BatchSize = batchSize;
            InitObjects(batchSize);
        }

        private void InitObjects(int batchSize) {
            NLogger = NLog.LogManager.GetLogger("ETL");
            Buffer = new BatchBlock<TInput>(batchSize);
            TargetAction = new ActionBlock<TInput[]>(d => WriteBatch(d));
            Buffer.LinkTo(TargetAction, new DataflowLinkOptions() { PropagateCompletion = true });
            TypeInfo = new TypeInfo(typeof(TInput));
        }      

        private void WriteBatch(TInput[] data) {
            NLogStart();
            if (BeforeBatchWrite != null)
                data = BeforeBatchWrite.Invoke(data);
            TableData<object> td = new TableData<object>(DestinationTableDefinition, DEFAULT_BATCH_SIZE);
            td.Rows = ConvertRows(data);
            new SqlTask(this, $"Execute Bulk insert into {DestinationTableDefinition.Name}").BulkInsert(td, td.ColumnMapping, DestinationTableDefinition.Name);
            NLogFinish();
        }

      
        private List<object[]> ConvertRows(TInput[] data) {
            List<object[]> result = new List<object[]>();
            foreach (var CurrentRow in data) {
                object[] rowResult;
                if (TypeInfo.IsArray) {
                    rowResult = CurrentRow as object[];
                } else {
                    rowResult = new object[TypeInfo.PropertyLength];
                    int index = 0;
                    foreach (PropertyInfo propInfo in TypeInfo.PropertyInfos) {
                        rowResult[index] = propInfo.GetValue(CurrentRow);
                        index++;
                    }
                }
                result.Add(rowResult);
            }
            return result;          
        }

        public void Wait() =>  TargetAction.Completion.Wait();

        void NLogStart() {
            if (!DisableLogging)
                NLogger.Debug(TaskName, TaskType, "START", TaskHash, ControlFlow.STAGE, ControlFlow.CurrentLoadProcess?.LoadProcessKey);
        }

        void NLogFinish() {
            if (!DisableLogging)
                NLogger.Debug(TaskName, TaskType, "END", TaskHash, ControlFlow.STAGE, ControlFlow.CurrentLoadProcess?.LoadProcessKey);
        }
    }

}
