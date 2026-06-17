using System.Collections;
using UnityEngine;

namespace StarForce
{
    public partial class MyAssetComponent
    {
        private void EnqueueLoadTask(MyAssetLoadTask task)
        {
            m_WaitingTasks.Add(task);
            ProcessLoadQueue();
        }

        private void ProcessLoadQueue()
        {
            int maxConcurrentCount = Mathf.Max(1, m_MaxConcurrentLoadTaskCount);
            while (m_RunningLoadTaskCount < maxConcurrentCount && m_WaitingTasks.Count > 0)
            {
                MyAssetLoadTask task = DequeueNextLoadTask();

                if (task.Handle.IsReleased)
                {
                    task.Handle.Complete(false, null, "MyAsset load task was released before start.");
                    continue;
                }

                m_RunningLoadTaskCount++;
                StartCoroutine(RunLoadTask(task));
            }
        }

        private MyAssetLoadTask DequeueNextLoadTask()
        {
            int bestIndex = 0;
            MyAssetLoadTask bestTask = m_WaitingTasks[0];
            for (int i = 1; i < m_WaitingTasks.Count; i++)
            {
                MyAssetLoadTask task = m_WaitingTasks[i];
                if (task.Priority > bestTask.Priority
                    || (task.Priority == bestTask.Priority && task.SerialId < bestTask.SerialId))
                {
                    bestIndex = i;
                    bestTask = task;
                }
            }

            m_WaitingTasks.RemoveAt(bestIndex);
            return bestTask;
        }

        private IEnumerator RunLoadTask(MyAssetLoadTask task)
        {
            yield return task.Routine;

            m_RunningLoadTaskCount--;
            ProcessLoadQueue();
        }

        private void ClearWaitingLoadTasks(string errorMessage)
        {
            for (int i = 0; i < m_WaitingTasks.Count; i++)
            {
                MyAssetLoadTask task = m_WaitingTasks[i];
                task.Handle.Complete(false, null, errorMessage);
            }

            m_WaitingTasks.Clear();
        }
    }
}
