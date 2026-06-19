using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// ── Enums & Data Models ──────────────────────────────────────────────────────

[Serializable]
public enum DifficultyLevel { Easy, Medium, Hard, Extreme }

[Serializable]
public class Vector3Serializable
{
    public float x, y, z;
    public Vector3Serializable(Vector3 v) { x = v.x; y = v.y; z = v.z; }
    public Vector3 ToVector3() => new Vector3(x, y, z);
}

[Serializable]
public class Vector3IntSerializable
{
    public int x, y, z;
    public Vector3IntSerializable(Vector3Int v) { x = v.x; y = v.y; z = v.z; }
    public Vector3Int ToVector3Int() => new Vector3Int(x, y, z);
}

[Serializable]
public class TrialData
{
    public Vector3IntSerializable GridCell;
    public Vector3Serializable    WorldPosition;
    public DifficultyLevel        DifficultyLevel;
    public float                  StartTime;
    public float                  MovementTime;
    public bool                   ReachedTarget;
    public Vector3Serializable    StartHandPosition;
    public Vector3Serializable    FinalHandPosition;
    public float                  MaxReachDistance;
    public List<Vector3Serializable> Trajectory = new List<Vector3Serializable>();

    // Helper setters so TargetManager can use Vector3/Vector3Int directly
    public void SetGridCell(Vector3Int v)          => GridCell = new Vector3IntSerializable(v);
    public void SetWorldPosition(Vector3 v)        => WorldPosition = new Vector3Serializable(v);
    public void SetStartHandPosition(Vector3 v)    => StartHandPosition = new Vector3Serializable(v);
    public void SetFinalHandPosition(Vector3 v)    => FinalHandPosition = new Vector3Serializable(v);
    public void AddTrajectoryPoint(Vector3 v)      => Trajectory.Add(new Vector3Serializable(v));
}

[Serializable]
public class AbilityMapEntry
{
    public Vector3IntSerializable Cell;
    public bool                   Success;
}

[Serializable]
public class AbilityZoneResult
{
    public int   TotalTrials;
    public int   SuccessfulTrials;
    public float SuccessRate;
    public float MaxReach3D;
    public int   FatigueOnsetTrial;
    public string CompletedAt;
    public List<AbilityMapEntry> AbilityMapEntries = new List<AbilityMapEntry>();
}

[Serializable]
public class SessionRecord
{
    public string            PatientID;
    public string            SessionID;
    public string            Timestamp;
    public List<TrialData>   Trials;
    public AbilityZoneResult Result;
}

// ── DataRecorder Singleton ───────────────────────────────────────────────────

public class DataRecorder : MonoBehaviour
{
    public static DataRecorder Instance { get; private set; }

    [Header("Session Info")]
    public string PatientID = "P001";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SaveSession(List<TrialData> trials, AbilityZoneResult result)
    {
        result.CompletedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        var record = new SessionRecord
        {
            PatientID = PatientID,
            SessionID = Guid.NewGuid().ToString("N").Substring(0, 8),
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"),
            Trials    = trials,
            Result    = result
        };

        try
        {
            string dir = Path.Combine(Application.persistentDataPath, "AbilityZone", "sessions");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, $"session_{record.Timestamp}_{record.PatientID}.json");
            File.WriteAllText(path, JsonUtility.ToJson(record, true));
            Debug.Log($"[DataRecorder] Saved → {path}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DataRecorder] Save failed: {e.Message}");
        }
    }

    public List<SessionRecord> LoadSessions(string patientID)
    {
        var records = new List<SessionRecord>();
        string dir = Path.Combine(Application.persistentDataPath, "AbilityZone", "sessions");
        if (!Directory.Exists(dir)) return records;

        foreach (string file in Directory.GetFiles(dir, $"*_{patientID}.json"))
        {
            try
            {
                var rec = JsonUtility.FromJson<SessionRecord>(File.ReadAllText(file));
                if (rec != null) records.Add(rec);
            }
            catch (Exception e) { Debug.LogWarning($"[DataRecorder] Load failed {file}: {e.Message}"); }
        }
        return records;
    }
}
