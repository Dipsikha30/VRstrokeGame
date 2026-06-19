using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetManager : MonoBehaviour
{
    [Header("Grid Configuration")]
    public int    gridSize    = 5;
    public float  cellSpacing = 0.15f;
    public Vector3 gridOriginOffset = new Vector3(0f, 1.2f, 0.5f);

    [Header("Target Settings")]
    public GameObject targetPrefab;
    public float targetDisplayDuration = 5f;
    public float restThreshold = 0.1f;

    [Header("Auto Start")]
    [Tooltip("Automatically start assessment when scene loads")]
    public bool autoStart = true;
    [Tooltip("Seconds to wait before auto-starting")]
    public float autoStartDelay = 3f;

    [Header("Hand Transform")]
    public Transform handTransform;

    private Vector3 _gridOriginWorld;
    private Vector3 _restPosition;
    private GameObject _activeTarget;
    private List<TrialData> _completedTrials = new List<TrialData>();
    private List<AbilityMapEntry> _abilityMap = new List<AbilityMapEntry>();
    private bool _assessmentRunning = false;

    public System.Action<TrialData>         OnTrialCompleted;
    public System.Action<AbilityZoneResult> OnAssessmentComplete;

    void Start()
    {
        _gridOriginWorld = Camera.main != null
            ? Camera.main.transform.position + gridOriginOffset
            : gridOriginOffset;
        _restPosition = _gridOriginWorld;

        if (autoStart)
        {
            Debug.Log($"[TargetManager] Auto-starting in {autoStartDelay} seconds...");
            GameUIManager.Instance?.ShowMessage($"Assessment starting in {autoStartDelay}s...");
            Invoke(nameof(StartAssessment), autoStartDelay);
        }
    }

    public void StartAssessment()
    {
        if (_assessmentRunning) { Debug.LogWarning("[TargetManager] Already running!"); return; }
        _assessmentRunning = true;
        _completedTrials.Clear();
        _abilityMap.Clear();
        Debug.Log("[TargetManager] Assessment started!");
        StartCoroutine(RunAssessmentSequence());
    }

    IEnumerator RunAssessmentSequence()
    {
        yield return StartCoroutine(CalibrateRestPosition());
        var positions = GetGridPositions();
        ShuffleList(positions);
        Debug.Log($"[TargetManager] {positions.Count} targets to test.");
        GameUIManager.Instance?.ShowMessage($"Starting — {positions.Count} targets");
        GameUIManager.Instance?.SetTotalTrials(positions.Count);
        yield return new WaitForSeconds(1.5f);
        foreach (var cell in positions)
        {
            yield return StartCoroutine(RunSingleTrial(cell));
            yield return new WaitForSeconds(0.4f);
        }
        FinishAssessment();
    }

    IEnumerator CalibrateRestPosition()
    {
        GameUIManager.Instance?.ShowMessage("Hold your arm at rest for 2 seconds...");
        Debug.Log("[TargetManager] Calibrating rest position...");
        float stableTimer = 0f;
        Vector3 lastPos = GetHandPosition();
        while (stableTimer < 2f)
        {
            Vector3 cur = GetHandPosition();
            stableTimer = Vector3.Distance(cur, lastPos) < 0.02f ? stableTimer + Time.deltaTime : 0f;
            lastPos = cur;
            yield return null;
        }
        _restPosition = GetHandPosition();
        _gridOriginWorld = _restPosition + gridOriginOffset;
        Debug.Log($"[TargetManager] Rest position: {_restPosition}");
        GameUIManager.Instance?.ShowMessage("Rest position locked! Starting...");
        yield return new WaitForSeconds(0.8f);
    }

    IEnumerator RunSingleTrial(Vector3Int cell)
    {
        if (targetPrefab == null) { Debug.LogError("[TargetManager] targetPrefab NOT assigned!"); yield break; }
        Vector3 worldPos = CellToWorld(cell);
        _activeTarget = Instantiate(targetPrefab, worldPos, Quaternion.identity);
        var targetObj = _activeTarget.GetComponent<TargetObject>();
        targetObj?.Activate();
        Debug.Log($"[TargetManager] Target spawned at {worldPos}");
        var trial = new TrialData();
        trial.SetGridCell(cell);
        trial.SetWorldPosition(worldPos);
        trial.DifficultyLevel = ClassifyDifficulty(cell);
        trial.StartTime = Time.time;
        trial.SetStartHandPosition(GetHandPosition());
        float elapsed = 0f;
        bool success = false;
        GameUIManager.Instance?.StartTrialTimer(targetDisplayDuration);
        while (elapsed < targetDisplayDuration)
        {
            elapsed += Time.deltaTime;
            trial.AddTrajectoryPoint(GetHandPosition());
            if (targetObj != null && targetObj.IsGrasped) { success = true; break; }
            yield return null;
        }
        trial.MovementTime = elapsed;
        trial.ReachedTarget = success;
        trial.SetFinalHandPosition(GetHandPosition());
        trial.MaxReachDistance = MaxDistFromOrigin(trial.Trajectory, _restPosition);
        _abilityMap.Add(new AbilityMapEntry { Cell = new Vector3IntSerializable(cell), Success = success });
        _completedTrials.Add(trial);
        OnTrialCompleted?.Invoke(trial);
        GameUIManager.Instance?.OnTrialEnded(trial);
        Debug.Log($"[TargetManager] Trial — reached:{success} time:{elapsed:F2}s");
        if (_activeTarget != null) Destroy(_activeTarget);
        GameUIManager.Instance?.ShowMessage("Return to rest position...");
        float returnTimer = 0f;
        while (returnTimer < 1.5f)
        {
            returnTimer = Vector3.Distance(GetHandPosition(), _restPosition) < restThreshold
                ? returnTimer + Time.deltaTime : 0f;
            yield return null;
        }
    }

    void FinishAssessment()
    {
        _assessmentRunning = false;
        var result = BuildResult();
        OnAssessmentComplete?.Invoke(result);
        DataRecorder.Instance?.SaveSession(_completedTrials, result);
        GameUIManager.Instance?.ShowAssessmentComplete(result);
        Debug.Log("=== ASSESSMENT COMPLETE ===");
        Debug.Log($"Total: {result.TotalTrials} | Success: {result.SuccessfulTrials} | Rate: {result.SuccessRate*100f:F0}%");
        Debug.Log($"Max Reach: {result.MaxReach3D*100f:F1} cm");
        Debug.Log($"Saved to: {Application.persistentDataPath}/AbilityZone/sessions/");
    }

    List<Vector3Int> GetGridPositions()
    {
        var list = new List<Vector3Int>();
        for (int x = 0; x < gridSize; x++)
        for (int y = 0; y < gridSize; y++)
        for (int z = 0; z < gridSize; z++)
        {
            var off = CellToOffset(new Vector3Int(x, y, z));
            if (off.z >= 0f) list.Add(new Vector3Int(x, y, z));
        }
        return list;
    }

    Vector3 CellToOffset(Vector3Int cell)
    {
        float half = (gridSize - 1) / 2f;
        return new Vector3((cell.x-half)*cellSpacing, (cell.y-half)*cellSpacing, (cell.z-half)*cellSpacing);
    }

    Vector3 CellToWorld(Vector3Int cell) => _gridOriginWorld + CellToOffset(cell);

    DifficultyLevel ClassifyDifficulty(Vector3Int cell)
    {
        float ratio = CellToOffset(cell).magnitude / (gridSize * cellSpacing * 0.5f);
        if (ratio <= 0.25f) return DifficultyLevel.Easy;
        if (ratio <= 0.55f) return DifficultyLevel.Medium;
        if (ratio <= 0.85f) return DifficultyLevel.Hard;
        return DifficultyLevel.Extreme;
    }

    AbilityZoneResult BuildResult()
    {
        int s = _completedTrials.FindAll(t => t.ReachedTarget).Count;
        float maxR = 0f;
        foreach (var t in _completedTrials) if (t.ReachedTarget) maxR = Mathf.Max(maxR, t.MaxReachDistance);
        return new AbilityZoneResult
        {
            TotalTrials=_completedTrials.Count, SuccessfulTrials=s,
            SuccessRate=_completedTrials.Count>0?(float)s/_completedTrials.Count:0f,
            MaxReach3D=maxR, FatigueOnsetTrial=-1, AbilityMapEntries=_abilityMap
        };
    }

    Vector3 GetHandPosition()
    {
        if (handTransform != null) return handTransform.position;
        return Camera.main != null ? Camera.main.transform.position : Vector3.zero;
    }

    float MaxDistFromOrigin(List<Vector3Serializable> traj, Vector3 origin)
    {
        float max = 0f;
        foreach (var p in traj) max = Mathf.Max(max, Vector3.Distance(p.ToVector3(), origin));
        return max;
    }

    void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count-1; i > 0; i--)
        { int j = Random.Range(0,i+1); (list[i],list[j])=(list[j],list[i]); }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 origin = Application.isPlaying ? _gridOriginWorld
            : (Camera.main != null ? Camera.main.transform.position + gridOriginOffset : gridOriginOffset);
        for (int x = 0; x < gridSize; x++)
        for (int y = 0; y < gridSize; y++)
        for (int z = 0; z < gridSize; z++)
        {
            float half = (gridSize-1)/2f;
            Vector3 off = new Vector3((x-half)*cellSpacing,(y-half)*cellSpacing,(z-half)*cellSpacing);
            if (off.z >= 0f) Gizmos.DrawWireSphere(origin+off, 0.02f);
        }
    }
}
