using System;
using System.Collections.Generic;
using UnityEngine.VisualScripting;
using Object = UnityEngine.Object;

public static class Debugger
{
    // TODO make that a user option
    const int k_MaxRecordedFrames = 500;

    public class VisualScriptingFrameTrace
    {
        public enum StepType
        {
            None,
            Exception,
        }
        public struct ValueRecord
        {
            public string readableValue;
            public int nodeId;
        }

        public struct NodeRecord
        {
            public int nodeId;
            public StepType type;
            public string exceptionText;
        }
        public List<NodeRecord> steps;
        public Dictionary<int, List<ValueRecord>> values;

        int m_NextStepIndex;

        public void SetNextStep(int nodeId, StepType type = StepType.None, string exceptionText = "")
        {
            if (steps == null)
                steps = new List<NodeRecord>();

            var nodeRecord = new NodeRecord { nodeId = nodeId, type = type, exceptionText = exceptionText};

            // skip records already used by Insert() calls
            while (m_NextStepIndex < steps.Count && steps[m_NextStepIndex].nodeId != -1)
                m_NextStepIndex++;

            if (m_NextStepIndex < steps.Count)
                steps[m_NextStepIndex] = nodeRecord;
            else
                steps.Add(nodeRecord);
            m_NextStepIndex++;
        }

        public void PadAndInsert(int stepOffset, int nodeId, StepType type = StepType.None, string exceptionText = "")
        {
            if (steps == null)
                steps = new List<NodeRecord>(stepOffset + 1);

            // skip records already used by Insert() calls
            while (m_NextStepIndex < steps.Count && steps[m_NextStepIndex].nodeId != -1)
                m_NextStepIndex++;

            // pad with empty records
            for (int i = steps.Count; i < m_NextStepIndex + stepOffset; i++)
                steps.Add(new NodeRecord { nodeId = -1 });

            steps.Insert(m_NextStepIndex + stepOffset, new NodeRecord { nodeId = nodeId, type = type, exceptionText = exceptionText});
        }

        public void AddValue(object value, int nodeId)
        {
            var step = m_NextStepIndex - 1;
            if (values == null)
                values = new Dictionary<int, List<ValueRecord>>();
            if (!values.ContainsKey(step))
                values[step] = new List<ValueRecord>();
            ValueRecord record = new ValueRecord
            {
                readableValue = value?.ToString() ?? "<null>", nodeId = nodeId
            };
            values[step].Add(record);
        }
    }

    static Dictionary<int, TraceRecorder> s_Data;
    static Dictionary<int, VisualScriptingFrameTrace> s_CurrentFrameData;
    static int s_LastFrame = -1;

    public static VisualScriptingFrameTrace SetLastCallFrame(int nodeId, int graphId, int frameCount, string name, int stepOffset = 0)
    {
        if (s_LastFrame != frameCount)
        {
            if (s_Data == null)
            {
                s_Data = new Dictionary<int, TraceRecorder>();
            }

            if (s_LastFrame > 0)
            {
                // record last frame, before setting up new one
                foreach (var keypair in s_CurrentFrameData)
                {
                    if (!s_Data.TryGetValue(keypair.Key, out var recorder))
                    {
                        s_Data.Add(keypair.Key, recorder = new TraceRecorder(k_MaxRecordedFrames));
                    }

                    VisualScriptingFrameTrace data = keypair.Value;
                    recorder.Record(frameCount, data);
                }
            }

            s_CurrentFrameData = null;
            s_LastFrame = frameCount;
        }

        if (s_CurrentFrameData == null)
        {
            s_CurrentFrameData = new Dictionary<int, VisualScriptingFrameTrace>();
        }

        if (!s_CurrentFrameData.TryGetValue(graphId, out var frameData))
        {
            s_CurrentFrameData.Add(graphId, frameData = new VisualScriptingFrameTrace());
        }

        if (stepOffset == 0)
            frameData.SetNextStep(nodeId);
        else
            frameData.PadAndInsert(stepOffset, nodeId);
        return frameData;
    }

    public static T RecordValue<T>(T value, int nodeId, int graphId, int frameCount)
    {
        VisualScriptingFrameTrace data = SetLastCallFrame(nodeId, graphId, frameCount, "");

        data.AddValue(value, nodeId);
        return value;
    }

    public static VisualScriptingFrameTrace GetData(int frameCount, Object vsGraphModel)
    {
        var instanceId = vsGraphModel.GetInstanceID();
        TraceRecorder recorder = null;
        s_Data?.TryGetValue(instanceId, out recorder);
        return recorder?.Get(frameCount) as VisualScriptingFrameTrace;
    }
}
