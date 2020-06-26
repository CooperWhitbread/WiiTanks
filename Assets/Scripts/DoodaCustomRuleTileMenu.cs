namespace UnityEditor
{
    static class DoodaCustomRuleTileMenu
    {
        [MenuItem("Assets/Create/CustomDoodaRuleTileScript", false, 89)]
        static void CreateCustomRuleTile()
        {
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile("NewCustomRuleTile.cs.txt", "NewCustomRuleTile.cs");
        }
    }
}
