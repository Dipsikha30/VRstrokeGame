# Ability Zone Assessment Game — Unity Setup Guide

## Overview
A VR rehabilitation assessment game where players reach and grasp objects at positions 
on a 3D grid. The game records arm mobility data and generates a personalised **Ability Zone Map**.

---

## 1. Unity Project Setup

### Unity Version
- **Unity 2022.3 LTS** or newer (recommended)
- Render Pipeline: **Universal Render Pipeline (URP)**

### Required Packages (via Package Manager)
| Package | Purpose |
|---|---|
| `XR Interaction Toolkit` 2.5+ | VR hand/controller input, grab interactions |
| `XR Plugin Management` | Meta Quest / OpenXR support |
| `OpenXR Plugin` | Cross-platform VR backend |
| `TextMeshPro` | UI text |
| `Newtonsoft JSON` (optional) | Better JSON serialisation |

Install via **Window → Package Manager → Add package by name**.

---

## 2. Scene Hierarchy

```
[Scene]
├── XR Origin (XR Rig)
│   ├── Camera Offset
│   │   ├── Main Camera
│   │   ├── Left Controller  ← XRController + XRRayInteractor
│   │   └── Right Controller ← XRController + XRDirectInteractor  ★
├── GameManager (Empty GO)
│   ├── TargetManager.cs     ← Drag Right Controller here
│   ├── DataRecorder.cs
│   └── GameUIManager.cs
├── World Canvas (World Space, ~1.5m ahead)
│   ├── InstructionText (TMP)
│   ├── TrialCounterText (TMP)
│   ├── TimerText (TMP)
│   ├── DifficultyIndicator (Image)
│   └── ResultsPanel (deactivated by default)
│       └── ResultsText (TMP)
├── AbilityZoneVisualizer (Empty GO)
│   └── AbilityZoneVisualizer.cs
└── Environment
    ├── Floor
    ├── Directional Light
    └── (optional) Table / Room props
```

---

## 3. Target Prefab Setup

Create a prefab: **Assets/Prefabs/TargetSphere.prefab**

```
TargetSphere (Prefab Root)
├── Components:
│   ├── Sphere Collider  (isTrigger: OFF for physics grab, radius: 0.05)
│   ├── Rigidbody        (Use Gravity: OFF, Is Kinematic: ON initially)
│   ├── XRGrabInteractable
│   │     Attach Transform: self
│   │     Throw On Detach: OFF
│   └── TargetObject.cs  (drag MeshRenderer into sphereRenderer slot)
├── MeshFilter + MeshRenderer
│   └── Material: Standard with Emission enabled
└── SuccessVFX (ParticleSystem, deactivated)
```

**Material settings for glow:**
- Shader: `Universal Render Pipeline/Lit`
- Enable **Emission** checkbox
- Set **Emission Color** to orange/yellow

---

## 4. TargetManager Inspector Settings

| Field | Recommended Value |
|---|---|
| Grid Size | 9 |
| Cell Spacing | 0.1 (10 cm between nodes) |
| Grid Origin Offset | (0, 0.9, 0.3) — adjust per patient height |
| Target Display Duration | 5 seconds |
| Return To Rest Duration | 1.5 seconds |
| Rest Threshold | 0.08 m |
| Right Hand Controller | Drag Right Controller GO here |
| Hand Transform | Drag Right Controller Transform here |

---

## 5. XR Interaction Setup

### For Meta Quest (recommended hardware)
1. **File → Build Settings → Android**
2. **Player Settings → XR Plugin Management → Android tab → OpenXR** ✓
3. Add **Meta Quest feature group** under OpenXR
4. Set **Interaction Profile**: Oculus Touch Controller Profile

### For Desktop (Mouse simulation during development)
- In XR Interaction Toolkit, enable **XR Device Simulator**
- Add `XRDeviceSimulator` prefab to scene for PC testing without headset

---

## 6. Data Output

Session files are saved to:
```
Android : /sdcard/Android/data/com.yourcompany.abilityzone/files/AbilityZone/sessions/
Windows : C:\Users\<user>\AppData\LocalLow\<Company>\AbilityZone\sessions\
```

Each session creates a JSON file:
```json
{
  "PatientID": "P001",
  "SessionID": "a1b2c3d4",
  "Timestamp": "2025-06-01_14-30-00",
  "Trials": [
    {
      "GridCell": { "x": 4, "y": 5, "z": 6 },
      "WorldPosition": { "x": 0.0, "y": 1.0, "z": 0.9 },
      "DifficultyLevel": 1,
      "MovementTime": 1.23,
      "ReachedTarget": true,
      "MaxReachDistance": 0.45,
      "Trajectory": [...]
    }
  ],
  "Result": {
    "TotalTrials": 50,
    "SuccessfulTrials": 38,
    "SuccessRate": 0.76,
    "MaxReach3D": 0.52,
    "FatigueOnsetTrial": 42
  }
}
```

---

## 7. Scripts Reference

| Script | Responsibility |
|---|---|
| `TargetManager.cs` | Grid generation, trial sequence, ability zone calculation |
| `TargetObject.cs` | Target glow, grasp detection via XRI events |
| `DataModels.cs` | All data structs + DataRecorder singleton (JSON save/load) |
| `AbilityZoneVisualizer.cs` | Post-assessment 3D voxel map rendering |
| `GameUIManager.cs` | All in-scene UI: messages, timers, results |

---

## 8. Workflow Mapping (from Game Concept Doc)

```
Player plays task  →  TargetManager.RunAssessmentSequence()
       ↓
Collect performance →  TrialData recorded per trial
       ↓
3D grid ability map →  TargetManager._abilityMap (Dictionary<Vector3Int, bool>)
       ↓
Algorithm → zones  →  TargetManager.ClassifyDifficulty() + BuildAbilityZoneResult()
       ↓
Difficulty zones   →  DifficultyLevel enum (Easy / Medium / Hard / Extreme)
       ↓
Generate levels    →  AbilityZoneResult drives next game session target selection
```

---

## 9. Next Steps & Extensions

- **Fruit Picking Variant**: Replace sphere prefab with fruit models + basket trigger zone
- **Analytics Dashboard**: Export JSON to REST API / Firebase for clinician review
- **Adaptive Difficulty**: Feed `AbilityZoneResult.MaxReach3D` into next session's grid origin
- **Bilateral Assessment**: Duplicate controller slots for left-hand tracking
- **Progress Tracking**: Load previous sessions in `DataRecorder.LoadSessions()` and compare `MaxReach3D` over time
- **Non-VR Mode**: Swap XRController for mouse raycast for desktop/tablet use

---

## 10. Build & Deploy (Meta Quest)

```bash
# In Unity:
# 1. File → Build Settings → Android
# 2. Switch Platform
# 3. Player Settings:
#    - Company Name / Package Name: com.yourco.abilityzone
#    - Minimum API Level: Android 10 (API 29)
#    - Target API: Android 12 (API 32)
# 4. XR Plug-in Management → OpenXR → Meta Quest features
# 5. Build And Run  (Quest must be in Developer Mode + USB debugging ON)
```
