using UnityEditor;

namespace TLP.UdonUtils.Editor
{
    [InitializeOnLoad]
    public class UdonUtilsDefinitions
    {
        static UdonUtilsDefinitions() {
            CustomDefinitionUtils.EnsureDefinitionsExist(typeof(UdonUtilsDefinitions), "TLP_UDONUTILS");
        }
    }
}