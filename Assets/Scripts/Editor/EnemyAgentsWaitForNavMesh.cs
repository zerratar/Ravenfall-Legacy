using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Saves the agents of the enemies placed in a scene disabled, in builds and when entering play mode (the saved
/// scene is not changed), and has each enemy enable its agent in Start instead.
/// </summary>
/// <remarks>
/// The world's navmesh is not baked into the scene: its NavMeshSurface adds it in OnEnable. Unity creates the
/// agents of a loading scene before that, so every enabled enemy agent logged "Failed to create agent because
/// there is no valid NavMesh" (136 on each start). The enemies recovered when they first moved, but the errors
/// buried everything else in the log. Agents of objects that start inactive (dungeons) are handled the same way.
/// </remarks>
public class EnemyAgentsWaitForNavMesh : IProcessSceneWithReport
{
    public int callbackOrder => 0;

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var movement in root.GetComponentsInChildren<EnemyMovementController>(true))
            {
                var agent = movement.GetComponent<NavMeshAgent>();
                if (!agent || !agent.enabled) continue;
                agent.enabled = false;
                movement.EnableAgentOnStart = true;
            }
        }
    }
}
