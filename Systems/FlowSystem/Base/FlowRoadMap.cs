using System;
using System.Collections.Generic;

namespace PowerCellStudio
{
    public class FlowRoadMap : IFlowRoadMap
    {
        private sealed class FlowTransition
        {
            public ISceneFlow from;
            public ISceneFlow to;
            public Func<IFlowContext, bool> condition;
            public int priority;
        }

        private readonly HashSet<ISceneFlow> _flows = new HashSet<ISceneFlow>();
        private readonly List<FlowTransition> _transitions = new List<FlowTransition>();

        public FlowRoadMap(IFlowContext context, string roadMapName = null)
        {
            this.context = context;
            this.roadMapName = string.IsNullOrEmpty(roadMapName) ? GetType().Name : roadMapName;
        }
        
        public IFlowContext context { get; private set; }

        public string roadMapName { get; }

        public ISceneFlow entryFlow { get; private set; }

        public FlowRoadMap AddFlow(ISceneFlow flow, bool asEntry = false)
        {
            if (flow == null) return this;

            _flows.Add(flow);
            if (asEntry || entryFlow == null)
            {
                entryFlow = flow;
                // this.context.StartFlow(entryFlow);
            }
            return this;
        }

        public FlowRoadMap SetEntry(ISceneFlow flow)
        {
            if (flow == null) return this;

            _flows.Add(flow);
            entryFlow = flow;
            // context.StartFlow(entryFlow);
            return this;
        }

        public IFlowRoadMap AddTransition(ISceneFlow from, ISceneFlow to, Func<IFlowContext, bool> condition = null, int priority = 0)
        {
            return AddTransitionInternal(from, to, condition, priority);
        }

        public bool TryGetTransition(ISceneFlow from, out ISceneFlow transition)
        {
            for (var i = 0; i < _transitions.Count; i++)
            {
                var t = _transitions[i];
                if (t == null) continue;
                if (t.from != from) continue;
                // if (t.failureOnly != failureOnly) continue;
                // if (t.transitionId == skipTransitionId) continue;
                if (!(t.condition?.Invoke(context) ?? true)) continue;
                transition = t.to;
                return true;
            }
            transition = default;
            return false;
        }

        public ISceneFlow GetSceneFlow(int id)
        {
            ISceneFlow result = null;
            foreach (var iSceneFlow in _flows)
            {
                if (iSceneFlow.id == id)
                {
                    result = iSceneFlow;
                    break;
                }
            }
            return result;
        }
        
        private FlowRoadMap AddTransitionInternal(ISceneFlow from, ISceneFlow to,
            Func<IFlowContext, bool> condition, int priority)
        {
            if (from == null || to == null) return this;

            _flows.Add(from);
            _flows.Add(to);
            _transitions.Add(new FlowTransition
            {
                from = from,
                to = to,
                condition = condition,
                priority = priority,
            });
            _transitions.Sort((a, b) => b.priority.CompareTo(a.priority));
            return this;
        }

        public void Dispose()
        {
            foreach (var flow in _flows)
            {
                flow?.Dispose();
            }
            context?.Dispose();
            context =  null;
            // _nestedRoadMaps.Clear();
            _flows.Clear();
            _transitions.Clear();
            
        }
    }
}