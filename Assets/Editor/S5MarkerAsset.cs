using UnityEditor;
using UnityEngine;

/// <summary>
/// 生成标记材质 Assets/Resources/Marker.mat（黄色）。
/// 用法：Unity -batchmode -executeMethod S5MarkerAsset.Create
/// </summary>
public static class S5MarkerAsset
{
    public static void Create()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        AssetDatabase.Refresh();   // 必须先刷新，AssetDatabase 才认识新目录

        Shader shader = Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.color = Color.yellow;
        AssetDatabase.CreateAsset(mat, "Assets/Resources/Marker.mat");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[S5MarkerAsset] 已创建 Assets/Resources/Marker.mat");
    }
}
