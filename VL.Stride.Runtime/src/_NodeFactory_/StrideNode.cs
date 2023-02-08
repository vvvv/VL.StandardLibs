using System;
using System.Collections.Generic;
using System.Linq;
using VL.Core;

namespace VL.Stride
{
    class StrideNode : FactoryBasedVLNode
    {
        public bool needsUpdate;

        public StrideNode(NodeContext nodeContext) : base(nodeContext)
        {
        }
    }

    class StrideNode<TInstance> : StrideNode, IVLNode
        where TInstance : new()
    {
        readonly Pin<TInstance>[] inputs;
        readonly StrideNodeDesc<TInstance> nodeDescription;
        readonly StatePin output;

        public StrideNode(NodeContext nodeContext, StrideNodeDesc<TInstance> description)
            : base(nodeContext)
        {
            nodeDescription = description;

            inputs = description.Inputs.OfType<PinDescription>().Select(d => d.CreatePin<TInstance>(this)).ToArray();
            Outputs = new IVLPin[] { output = new StatePin(this, new TInstance()) };
        }

        public IVLNodeDescription NodeDescription => nodeDescription;

        public IVLPin[] Inputs => inputs;

        public IVLPin[] Outputs { get; }

        public void Update()
        {
            if (needsUpdate)
            {
                needsUpdate = false;

                TInstance instance;
                if (nodeDescription.CopyOnWrite)
                {
                    // TODO: Causes crash in pipeline
                    //if (Outputs[0].Value is IDisposable disposable)
                    //    disposable.Dispose();
                    instance = new TInstance();
                }
                else
                {
                    instance = output.value;
                }

                foreach (var pin in inputs)
                    pin.ApplyValue(instance);

                output.value = instance;
            }
        }

        public void Dispose()
        {
            if (!nodeDescription.CopyOnWrite && Outputs[0].Value is IDisposable disposable)
                disposable.Dispose();
        }

        class StatePin : IVLPin<TInstance>
        {
            readonly StrideNode<TInstance> node;
            readonly bool isFragmented;

            public StatePin(StrideNode<TInstance> node, TInstance instance)
            {
                this.node = node;
                this.isFragmented = node.nodeDescription.Fragmented;
                this.value = instance;
            }

            object IVLPin.Value
            {
                get => ((IVLPin<TInstance>)this).Value;
                set => ((IVLPin<TInstance>)this).Value = (TInstance)value;
            }

            TInstance IVLPin<TInstance>.Value
            { 
                get
                {
                    if (isFragmented && node.needsUpdate)
                        node.Update();
                    return value;
                }
                set => this.value = value;
            }
            public TInstance value;
        }
    }
}
