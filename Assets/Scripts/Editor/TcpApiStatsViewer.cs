// Add this to an appropriate class like GameManager or create a new TcpApiStatsViewer MonoBehaviour

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class TcpApiStatsViewer : EditorWindow
{
    private Vector2 _scrollPosition;
    private GameManager _gameManager;

    [MenuItem("Tools/Network Statistics")]
    public static void ShowWindow()
    {
        var window = GetWindow<TcpApiStatsViewer>();
        window.titleContent = new GUIContent("Network Stats");
        window.Show();
    }

    private void OnGUI()
    {
        if (_gameManager == null)
        {
            _gameManager = FindObjectOfType<GameManager>();
        }

        EditorGUILayout.BeginVertical();
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        // Get statistics from TcpApi
        string tcpApiStats = "TcpApi not available";
        string deltaClientStats = "DeltaClient not available";

        if (_gameManager != null)
        {
            // Get TcpApi statistics
            if (_gameManager.RavenNest != null && _gameManager.RavenNest.Tcp != null)
            {
                tcpApiStats = _gameManager.RavenNest.Tcp.GetStatisticsReport();
            }

            // Get DeltaClient statistics
            var deltaClientBehaviour = FindObjectOfType<DeltaClientBehaviour>();
            if (deltaClientBehaviour != null)
            {
                deltaClientStats = deltaClientBehaviour.GetStatisticsReport();
            }
        }

        EditorGUILayout.LabelField("TcpApi Statistics", EditorStyles.boldLabel);
        EditorGUILayout.TextArea(tcpApiStats, GUILayout.Height(200));

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("DeltaClient Statistics", EditorStyles.boldLabel);
        EditorGUILayout.TextArea(deltaClientStats, GUILayout.Height(200));

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Reset Statistics"))
        {
            if (_gameManager != null && _gameManager.RavenNest?.Tcp != null)
            {
                _gameManager.RavenNest.Tcp.ResetStatistics();
            }

            var deltaClientBehaviour = FindObjectOfType<DeltaClientBehaviour>();
            if (deltaClientBehaviour != null)
            {
                deltaClientBehaviour.ResetStatistics();
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        // Update every second in editor
        Repaint();
    }
}
#endif
