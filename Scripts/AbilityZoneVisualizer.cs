using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// After assessment completes, visualises the ability map as coloured voxels.
/// Green = reached, Red = missed.
/// Call Show() and pass in the result + the grid origin world position.
/// </summary>
public class AbilityZoneVisualizer : MonoBehaviour
{
    [Header("Voxel Settings")]
    public GameObject voxelPrefab;
    public float voxelSize    = 0.04f;
    public float voxelSpacing = 0.15f;   // match TargetManager.cellSpacing

    [Header("Colors")]
    public Color successColor = new Color(0.2f, 0.9f, 0.3f, 0.6f);
    public Color failColor    = new Color(0.9f, 0.2f, 0.2f, 0.3f);

    [Header("Animation")]
    public float revealDelay = 0.02f;    // seconds between each voxel appearing

    private List<GameObject> _voxels = new List<GameObject>();

    // ── Public API ───────────────────────────────────────────────────

    public void Show(AbilityZoneResult result, Vector3 origin)
    {
        Clear();
        if (result == null || result.AbilityMapEntries == null || result.AbilityMapEntries.Count == 0)
        {
            Debug.LogWarning("[AbilityZoneVisualizer] No ability map data to display.");
            return;
        }
        StartCoroutine(RevealVoxels(result, origin));
    }

    public void Clear()
    {
        foreach (var v in _voxels)
            if (v != null) Destroy(v);
        _voxels.Clear();
    }

    // ── Internal ─────────────────────────────────────────────────────

    IEnumerator RevealVoxels(AbilityZoneResult result, Vector3 origin)
    {
        foreach (var entry in result.AbilityMapEntries)
        {
            if (entry == null || entry.Cell == null) continue;

            Vector3 offset = new Vector3(
                entry.Cell.x * voxelSpacing,
                entry.Cell.y * voxelSpacing,
                entry.Cell.z * voxelSpacing
            );
            Vector3 worldPos = origin + offset;

            if (voxelPrefab != null)
            {
                GameObject v = Instantiate(voxelPrefab, worldPos, Quaternion.identity, transform);
                v.transform.localScale = Vector3.one * voxelSize;

                var rend = v.GetComponent<Renderer>();
                if (rend != null)
                {
                    // Use a material instance so colors don't bleed between voxels
                    Material mat = rend.material;
                    Color c = entry.Success ? successColor : failColor;
                    mat.color = c;

                    // Enable transparency (Standard shader)
                    mat.SetFloat("_Mode", 3f);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.renderQueue = 3000;
                }

                _voxels.Add(v);
            }
            else
            {
                // No prefab assigned — draw debug spheres instead
                Debug.DrawLine(worldPos, worldPos + Vector3.up * 0.05f,
                    entry.Success ? Color.green : Color.red, 10f);
            }

            yield return new WaitForSeconds(revealDelay);
        }

        Debug.Log($"[AbilityZoneVisualizer] Displayed {_voxels.Count} voxels.");
    }

    void OnDisable() => Clear();
}
