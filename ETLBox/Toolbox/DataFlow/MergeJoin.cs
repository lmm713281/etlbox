﻿using System;
using System.Threading.Tasks.Dataflow;


namespace ALE.ETLBox {
    public class MergeJoin<TInput1, TInput2, TOutput> : GenericTask, ITask, IDataFlowLinkSource<TOutput> {
        private Func<TInput1, TInput2, TOutput> _mergeJoinFunc;

        /* ITask Interface */
        public override string TaskType { get; set; } = "DF_JOIN";
        public override string TaskName { get; set; } = "Join (unnamed)";
        public override void Execute() { throw new Exception("Transformations can't be executed directly"); }

        /* Public Properties */
        public MergeJoinTarget<TInput1> Target1 { get; set; }
        public MergeJoinTarget<TInput2> Target2 { get; set; }
        public ISourceBlock<TOutput> SourceBlock => Transformation.SourceBlock;        

        public Func<TInput1, TInput2, TOutput> MergeJoinFunc {
            get { return _mergeJoinFunc; }
            set {
                _mergeJoinFunc = value;
                Transformation.RowTransformationFunc = new Func<Tuple<TInput1, TInput2>, TOutput>(tuple => _mergeJoinFunc.Invoke(tuple.Item1, tuple.Item2));
                JoinBlock.LinkTo(Transformation.TargetBlock, new DataflowLinkOptions { PropagateCompletion = true });
            }
        }

        /* Private stuff */
        internal BufferBlock<TInput1> Buffer1 { get; set; }
        internal BufferBlock<TInput1> Buffer2 { get; set; }
        internal JoinBlock<TInput1, TInput2> JoinBlock { get; set; }
        internal RowTransformation<Tuple<TInput1, TInput2>, TOutput> Transformation { get; set; }

        NLog.Logger NLogger { get; set; }

        public MergeJoin() {
            NLogger = NLog.LogManager.GetLogger("ETL");
            Transformation = new RowTransformation<Tuple<TInput1, TInput2>, TOutput>();
            JoinBlock = new JoinBlock<TInput1, TInput2>();            
            Target1 = new MergeJoinTarget<TInput1>(JoinBlock.Target1);
            Target2 = new MergeJoinTarget<TInput2>(JoinBlock.Target2);
            
            
        }

        public MergeJoin(Func<TInput1, TInput2, TOutput> mergeJoinFunc) : this() {
            MergeJoinFunc = mergeJoinFunc;
        }

        public MergeJoin(string name) : this() {
            this.TaskName = name;
        }

        public void LinkTo(IDataFlowLinkTarget<TOutput> target) {
            Transformation.LinkTo(target);
            NLogger.Debug(TaskName + " was linked to Target!", TaskType, "LOG", TaskHash, ControlFlow.STAGE, ControlFlow.CurrentLoadProcess?.LoadProcessKey);
        }

        public void LinkTo(IDataFlowLinkTarget<TOutput> target, Predicate<TOutput> predicate) {
            Transformation.LinkTo(target, predicate);
            NLogger.Debug(TaskName + " was linked to Target!", TaskType, "LOG", TaskHash, ControlFlow.STAGE, ControlFlow.CurrentLoadProcess?.LoadProcessKey);
        }

    }

    public class MergeJoinTarget<TInput> : IDataFlowDestination<TInput>{                        
        public ITargetBlock<TInput> TargetBlock { get; set; }

        public void Wait() {
            TargetBlock.Completion.Wait();     
        }
        public MergeJoinTarget(ITargetBlock<TInput> joinTarget) {
            TargetBlock = joinTarget;
        }
    }
}

