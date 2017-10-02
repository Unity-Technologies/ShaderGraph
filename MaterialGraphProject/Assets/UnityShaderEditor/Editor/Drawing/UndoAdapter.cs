using System;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;
using System.Collections.Generic;
using UnityEngine.Experimental.UIElements;
using System.Linq;

namespace UnityEditor.MaterialGraph.Drawing
{
    public class MaterialGraphUndoAdapter : UndoAdapter<UnityEngine.MaterialGraph.MaterialGraph> {}
    public class SubGraphUndoAdapter : UndoAdapter<SubGraph> {}

    public enum GraphChangeCommandChangeType
    {
        NodeAdded,
        NodeRemoved,
        NodeModified,
        EdgeAdded,
        EdgeRemoved
    }

    [Serializable]
    public struct GraphChangeCommand
    {
        public GraphChangeCommandChangeType type;
        public System.Guid nodeId;
        public System.Guid auxNodeId;
        public int slotNodeId;
        public int auxSlotNodeId;
        public SerializationHelper.JSONSerializedElement data;
        public ModificationScope scope;
    }

    [Serializable]
    public class UndoAdapterData
    {
        public List<GraphChangeCommand> changes;
        public int stackPointerActual;
    }

    public class UndoAdapter<TGraphType> : ScriptableObject where TGraphType : AbstractMaterialGraph
    {
        [SerializeField]
        public int m_StackPointerForUndo;

        [NonSerialized]
        UndoAdapterData m_Data;

        [NonSerialized]
        TGraphType m_Graph;

        bool m_ReceivedChanges;

        public void Initialize(TGraphType graph, UndoAdapterData data)
        {
            m_Graph = graph;
            m_Graph.onChange += RecordChange;
            foreach (INode node in m_Graph.GetNodes<AbstractMaterialNode>())
                node.onModified += OnNodeChange;

            m_Data = data;
               
            if (m_Data.changes == null)
            {
                m_Data.changes = new List<GraphChangeCommand>();
                m_StackPointerForUndo = data.stackPointerActual = -1;
            }

            EditorApplication.playModeStateChanged += (PlayModeStateChange change) => {
                if (change == PlayModeStateChange.ExitingEditMode || change == PlayModeStateChange.ExitingPlayMode)
                {
                    data.changes.Clear();
                    m_StackPointerForUndo = data.stackPointerActual = -1;
                }
            };

            m_RecordChanges = true;

            SyncGraph();
        }

        void OnDestroy()
        {
            // FIXME No way to clear undo stack for a specific object?
            Undo.ClearAll();
        }

        void RecordChange(GraphChange change)
        {
            if (!m_RecordChanges)
                return;

            Debug.LogWarning(change);

            // If a new action is pushed onto the stack, clear the action in the redo portion
            if (m_Data.stackPointerActual < m_Data.changes.Count - 1)
            {
                m_Data.changes.RemoveRange(m_Data.stackPointerActual + 1, m_Data.changes.Count - m_Data.stackPointerActual - 1);
            }

            if (change is NodeAddedGraphChange)
            {  
                m_Data.changes.Add(new GraphChangeCommand() {
                    type = GraphChangeCommandChangeType.NodeAdded,
                    nodeId = ((NodeAddedGraphChange)change).node.guid
                });
            }
            else if (change is NodeRemovedGraphChange)
            {
                m_Data.changes.Add(new GraphChangeCommand() { 
                    type = GraphChangeCommandChangeType.NodeRemoved,
                    nodeId = ((NodeRemovedGraphChange)change).node.guid,
                    data = SerializationHelper.Serialize(((NodeRemovedGraphChange)change).node)
                });

                ((NodeRemovedGraphChange)change).node.onModified -= OnNodeChange;
            }            
            else if (change is EdgeAddedGraphChange)
            {
                m_Data.changes.Add(new GraphChangeCommand() { 
                    type = GraphChangeCommandChangeType.EdgeAdded,
                    nodeId = ((EdgeAddedGraphChange)change).edge.inputSlot.nodeGuid,
                    slotNodeId = ((EdgeAddedGraphChange)change).edge.inputSlot.slotId,
                    auxNodeId = ((EdgeAddedGraphChange)change).edge.outputSlot.nodeGuid,
                    auxSlotNodeId = ((EdgeAddedGraphChange)change).edge.outputSlot.slotId,
                });
            }

            if (change is EdgeRemovedGraphChange)
            {
                m_Data.changes.Add(new GraphChangeCommand() { 
                    type = GraphChangeCommandChangeType.EdgeRemoved,
                    nodeId = ((EdgeRemovedGraphChange)change).edge.inputSlot.nodeGuid,
                    slotNodeId = ((EdgeRemovedGraphChange)change).edge.inputSlot.slotId,
                    auxNodeId = ((EdgeRemovedGraphChange)change).edge.outputSlot.nodeGuid,
                    auxSlotNodeId = ((EdgeRemovedGraphChange)change).edge.outputSlot.slotId,
                    data = SerializationHelper.Serialize(((EdgeRemovedGraphChange)change).edge)
                });

            }

            m_Data.stackPointerActual = m_Data.changes.Count - 1;

            Undo.RecordObject(this, change.ToString());
            m_StackPointerForUndo = m_Data.stackPointerActual;
            Undo.FlushUndoRecordObjects();
        }

        void OnNodeChange(INode node, ModificationScope scope)
        {
            if (m_RecordChanges) return;

            //Debug.LogWarning("Node changed " + node + " scope = " + scope);

            // TODO node changes are sent after therefore it's too late to record object
            // ideas on how to fix that:
            // catch UI events going to the view and detect if a node is interacted
            // both on the graph view and inspector view

            /*Undo.RecordObject(this, "Node mofified ");

            m_Changes.Add(new GraphChangeCommand()
            {
                type = GraphChangeCommandChangeType.NodeModified,
                nodeId = node.guid,
                data = SerializationHelper.Serialize(node),
                scope = scope
            });
            m_StackPointerActual = m_StackPointer = m_Changes.Count - 1;*/
        }

        bool m_RecordChanges = true;

        // Called when undo/redo is performed
        void OnValidate()
        {
            if (m_Graph == null)
                return;
            
            SyncGraph();
        }

        void SyncGraph()
        {
            if (m_StackPointerForUndo == m_Data.stackPointerActual)
                return;

            Debug.Log("stack pointer moving from " + m_Data.stackPointerActual + " to " + m_StackPointerForUndo);

            m_RecordChanges = false;
            // Undo mode
            if (m_StackPointerForUndo < m_Data.stackPointerActual)
            {
                while (m_Data.stackPointerActual > m_StackPointerForUndo)
                {                    
                    GraphChangeCommand cmd = m_Data.changes[m_Data.stackPointerActual];
                    Debug.LogWarning("Undoing " + cmd.type);
                    switch(cmd.type)
                    {
                        case GraphChangeCommandChangeType.NodeAdded:
                            INode node = m_Graph.GetNodeFromGuid(cmd.nodeId);
                            cmd.data = SerializationHelper.Serialize(node);
                            m_Graph.RemoveNode(node);
                            m_Data.changes[m_Data.stackPointerActual] = cmd;
                        break;

                        case GraphChangeCommandChangeType.NodeRemoved:
                            m_Graph.AddNode(SerializationHelper.Deserialize<INode>(cmd.data, null));
                        break;

                        case GraphChangeCommandChangeType.EdgeAdded:
                            foreach(IEdge edge in m_Graph.edges)
                            {
                                if (edge.inputSlot.nodeGuid == cmd.nodeId &&
                                    edge.outputSlot.nodeGuid == cmd.auxNodeId &&
                                    edge.inputSlot.slotId == cmd.slotNodeId &&
                                    edge.outputSlot.slotId == cmd.auxSlotNodeId)
                                {
                                    cmd.data = SerializationHelper.Serialize(edge);                                    
                                    m_Data.changes[m_Data.stackPointerActual] = cmd;
                                    m_Graph.RemoveEdge(edge);
                                    break;   
                                }                                
                            }                            
                        break;

                        case GraphChangeCommandChangeType.EdgeRemoved:
                            IEdge restoredEdge = SerializationHelper.Deserialize<IEdge>(cmd.data, null);
                            // TODO consider adding a way to reuse the deserialized edge
                            if (m_Graph.Connect(restoredEdge.outputSlot, restoredEdge.inputSlot) == null)
                            {
                                Debug.LogError("Edge not added");
                            }
                        break;

                        case GraphChangeCommandChangeType.NodeModified:
                            INode existingNode = m_Graph.GetNodeFromGuid(cmd.nodeId);
                            EditorJsonUtility.FromJsonOverwrite(cmd.data.JSONnodeData, existingNode);
                            // notifies automatically after deserialization
                            //existingNode.onModified(existingNode, cmd.scope);
                        break;
                    }

                    m_Data.stackPointerActual--;
                }
            }
            // Redo mode
            else if (m_Data.stackPointerActual < m_StackPointerForUndo)
            {
                while (m_Data.stackPointerActual < m_StackPointerForUndo)
                {
                    m_Data.stackPointerActual++;
                    GraphChangeCommand cmd = m_Data.changes[m_Data.stackPointerActual];

                    Debug.LogWarning("Redoing " + cmd.type);

                    switch(cmd.type)
                    {
                        case GraphChangeCommandChangeType.NodeAdded:
                            m_Graph.AddNode(SerializationHelper.Deserialize<INode>(cmd.data, null));
                        break;

                        case GraphChangeCommandChangeType.NodeRemoved:
                            INode node = m_Graph.GetNodeFromGuid(cmd.nodeId);
                            m_Graph.RemoveNode(node);
                        break;

                        case GraphChangeCommandChangeType.EdgeAdded:
                            IEdge restoredEdge = SerializationHelper.Deserialize<IEdge>(cmd.data, null);
                            // TODO consider adding a way to reuse the deserialized edge
                            m_Graph.Connect(restoredEdge.outputSlot, restoredEdge.inputSlot);                         
                        break;

                        case GraphChangeCommandChangeType.EdgeRemoved:
                            bool removed = false;
                            foreach(IEdge edge in m_Graph.edges)
                            {
                                if (edge.inputSlot.nodeGuid == cmd.nodeId &&
                                        edge.outputSlot.nodeGuid == cmd.auxNodeId &&
                                        edge.inputSlot.slotId == cmd.slotNodeId &&
                                        edge.outputSlot.slotId == cmd.auxSlotNodeId)
                                {
                                    cmd.data = SerializationHelper.Serialize(edge);                                    
                                    m_Data.changes[m_Data.stackPointerActual] = cmd;
                                    m_Graph.RemoveEdge(edge);
                                    removed = true;
                                    break;   
                                }                                
                            }
                            if (!removed)
                                Debug.LogError("Edge not removed after redo");
                        break;
                    }
                }
            }
            m_RecordChanges = true;
        }
    }
}
