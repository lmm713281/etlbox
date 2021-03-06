﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;


namespace ALE.ETLBox {
    public class BlockTransformation<TInput> : GenericTask, ITask, IDataFlowLinkTarget<TInput>, IDataFlowLinkSource<TInput> {
        /* ITask Interface */
        public override string TaskType { get; set; } = "DF_BLOCKTRANSFORMATION";
        public override string TaskName { get; set; } = "Block Transformation (unnamed)";
        public override void Execute() { throw new Exception("Transformations can't be executed directly"); }

        /* Public Properties */        
        public Func<List<TInput>, List<TInput>> BlockTransformationFunc {
            get {
                return _blockTransformationFunc;
            }
            set {
                _blockTransformationFunc = value;
                InputBuffer = new ActionBlock<TInput>(row => InputData.Add(row));
                InputBuffer.Completion.ContinueWith(t => {
                    InputData = BlockTransformationFunc(InputData);
                    WriteIntoOutput();
                });

            }
        }        
        public ISourceBlock<TInput> SourceBlock => OutputBuffer;
        public ITargetBlock<TInput> TargetBlock => InputBuffer;

        /* Private stuff */
        BufferBlock<TInput> OutputBuffer { get; set; }
        ActionBlock<TInput> InputBuffer { get; set; }
        Func<List<TInput>, List<TInput>> _blockTransformationFunc;
        List<TInput> InputData { get; set; }
        NLog.Logger NLogger { get; set; }
        public BlockTransformation() {
            NLogger = NLog.LogManager.GetLogger("ETL");
            InputData = new List<TInput>();
            OutputBuffer = new BufferBlock<TInput>();
        }

        public BlockTransformation(Func<List<TInput>, List<TInput>> blockTransformationFunc) : this() {
            BlockTransformationFunc = blockTransformationFunc;
        }

        public BlockTransformation(string name, Func<List<TInput>, List<TInput>> blockTransformationFunc) : this(blockTransformationFunc) {
            this.TaskName = name;
        }

        private void WriteIntoOutput() {
            foreach (TInput row in InputData) {
                OutputBuffer.Post(row);
            }
            OutputBuffer.Complete();
        }

        public void LinkTo(IDataFlowLinkTarget<TInput> target) {
            OutputBuffer.LinkTo(target.TargetBlock, new DataflowLinkOptions() { PropagateCompletion = true });
            NLogger.Debug(TaskName + " was linked to Target!", TaskType, "LOG", TaskHash, ControlFlow.STAGE, ControlFlow.CurrentLoadProcess?.LoadProcessKey);
        }

        public void LinkTo(IDataFlowLinkTarget<TInput> target, Predicate<TInput> predicate) {
            OutputBuffer.LinkTo(target.TargetBlock, new DataflowLinkOptions() { PropagateCompletion = true }, predicate);
            NLogger.Debug(TaskName + " was linked to Target!", TaskType, "LOG", TaskHash, ControlFlow.STAGE, ControlFlow.CurrentLoadProcess?.LoadProcessKey);
        }

    }


}
