using Shinobytes.OpenAI;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Editor
{
    public class IslandEditor : OdinEditorWindow
    {
        private bool openAIRequestActive;

        [MenuItem("Ravenfall/Island Editor")]
        public static void ShowWindow()
        {
            var editor = GetWindow<IslandEditor>();

            editor.Islands = GameObject.FindObjectsByType<IslandController>(FindObjectsSortMode.None);
            editor.AccessToken = EditorPrefs.GetString("openai_access_token", "");

            editor.Show();
        }

        [TabGroup("Islands")]
        public IslandController[] Islands;

        [TabGroup("OpenAI")]
        public string AccessToken;

        [TabGroup("OpenAI")]
        [ReadOnly, ShowIf("openAIRequestActive")]
        public string Status = "Loading...";

        [TabGroup("OpenAI")]
        [Button("Level requirements ok?"), ShowIf("AccessToken"), HideIf("openAIRequestActive")]
        public async void CheckLevelRequirementsAsync()
        {
            //new OpenAIClient()
            if (openAIRequestActive)
            {
                return;
            }

            if (Islands == null || Islands.Length == 0)
            {
                Islands = GameObject.FindObjectsByType<IslandController>(FindObjectsSortMode.None);
            }

            if (string.IsNullOrEmpty(AccessToken))
            {
                AccessToken = EditorPrefs.GetString("openai_access_token", AccessToken);
            }
            else
            {
                EditorPrefs.SetString("openai_access_token", AccessToken);
            }

            if (string.IsNullOrEmpty(AccessToken))
            {
                return;
            }
            openAIRequestActive = true;
            try
            {
                var openAI = new OpenAIClient(new OpenAITokenString(AccessToken));
                var promptBuilder = new StringBuilder();

                promptBuilder.AppendLine("I'm making a RPG and need help balancing out level requirements for different islands in my game world.");
                promptBuilder.AppendLine("There are " + Islands.Length + " different islands in the game that the players can visit, each with different level requirements.");
                promptBuilder.AppendLine("Could you suggest what level requirements I should have for the different areas so that there is not a too big of a gap but also not too small?");
                promptBuilder.AppendLine("I will provide the current level requirements and would like the response to be in json format.");
                promptBuilder.AppendLine("");
                promptBuilder.AppendLine("Please respond with the following json format:");
                promptBuilder.AppendLine("[{");
                promptBuilder.AppendLine("   \"island\": \"name of island\",");
                promptBuilder.AppendLine("   \"skills\": [ ");
                promptBuilder.AppendLine("      { \"name\": \"Fishing\", \"skillLevelRequirement\": 1, \"combatLevelRequirement\": 1 }");
                promptBuilder.AppendLine("   ]");
                promptBuilder.AppendLine("}]");
                promptBuilder.AppendLine("");

                foreach (var island in Islands)
                {
                    promptBuilder.AppendLine("Island: " + island);

                    foreach (var c in island.GetComponentsInChildren<Chunk>(true))
                    {
                        promptBuilder.AppendLine(" Skill: " + c.Type + ", Skill Level Requirement: " + Mathf.Max(c.RequiredSkilllevel, 1) + ", Combat Level Requirement: " + Mathf.Max(c.RequiredCombatLevel, 1));
                    }


                    promptBuilder.AppendLine("");
                }

                UnityEngine.Debug.Log("Asking AI, using prompt:\n" + promptBuilder.ToString());

                var result = await openAI.GetCompletionAsync(promptBuilder.ToString());

                UnityEngine.Debug.Log(result.Choices[0].Message.Content);
            }
            catch { }
            finally
            {
                openAIRequestActive = false;
            }
        }
    }
}
