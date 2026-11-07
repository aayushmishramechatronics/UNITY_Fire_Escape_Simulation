# VR Emergency & Evacuation Simulator

A Unity-based fire evacuation simulator that lets users practise emergency decisions in a virtual indoor environment. Users navigate the building, respond to hazards, find an available exit and reach the assembly area. The project supports VR interaction and a desktop mode for development and demonstration.

## Features

- Indoor environment with rooms, corridors, doors, emergency exits and an assembly area.
- Fire, smoke, alarm audio, emergency lighting and warning messages.
- Player movement and interaction, including door interaction, evacuation-map inspection and torch pickup.
- Scripted exit blocking with falling-debris effects when the player enters a designated collapse area.
- Hazard-zone detection, unsafe-entry counting and exposure tracking. Reaching 100% exposure ends the attempt as a failure.
- Evacuation completion checks and session data logging.

> **Simulation note:** Fire and smoke are visual effects. Hazard detection and route closures use scripted areas and conditions; this is not a physically accurate fire-dynamics simulation.

## Technology Stack

- **Unity 6.5 (6000.5)** – development platform
- **C#** – simulation logic
- **URP** – rendering
- **OpenXR** and **XR Interaction Toolkit** – VR support and interaction
- **ProBuilder** – environment modelling
- **Unity Particle System / VFX** – fire and smoke effects
- **Git & GitHub** – version control

## Project Structure

```text
Assets/
├── _Project/                  # Environment assets, materials and layouts
├── FireEscapeStarter/
│   ├── Editor/                 # Environment-building tools
│   ├── Runtime/                # Gameplay and emergency scripts
│   ├── Shaders/                # Custom shaders
│   ├── InteriorAssets/         # Interior resources and layouts
│   ├── UniversityAssets/       # Campus resources, plans and audio
│   └── VisualAssets/           # Textures, effects and sound assets
├── Scenes/                     # Unity scenes
└── Settings/                   # Rendering and project settings
Packages/                       # Unity package dependencies
ProjectSettings/                # Project configuration
```

Main runtime scripts include `FEEmergency.cs`, `FEAtmosphere.cs`, `FEDoor.cs`, `FEFirstPerson.cs` and `FETorchPickup.cs`.

## Getting Started

1. Clone or download this repository.
2. Add the project in **Unity Hub**.
3. Open it with **Unity 6.5 (6000.5)** and allow the required packages to resolve.
4. Open the project's configured scene and press **Play** to run the desktop version.
5. For VR, connect a supported headset and configure its OpenXR runtime.

## Desktop Controls

| Control | Action |
|---|---|
| `W A S D` | Move |
| Mouse | Look around |
| `E` | Inspect / interact |
| `F` | Use torch |
| `Enter` | Begin evacuation from the briefing screen |
| `Esc` | Pause |
| `R` | Restart |

## Session Data

Evacuation results are appended to `FireEscape_Runs.csv` in Unity's `Application.persistentDataPath`. The log includes the run ID, UTC timestamp, outcome, elapsed time, exposure percentage, unsafe entries, route visits, collapse status and exit used. The CSV can be opened in Excel for review.

## Evaluation Scenarios

The project defines three evaluation conditions: **baseline evacuation**, **blocked primary route** and **reduced visibility**. These are intended to compare evacuation time, exit selection, unsafe entries and route deviation; do not treat them as completed participant-test results unless testing has been carried out.

## Team

- Ramanaa Vijayakumar
- Suhail Malik
- Mrinal Kumar
- Aayush Mishra

**Course:** MTE 4442 – Augmented Reality & Virtual Reality  
**Faculty:** Dr. Asha C S, Manipal Institute of Technology

## Repository

[GitHub – UNITY_Fire_Escape_Simulation](https://github.com/aayushmishramechatronics/UNITY_Fire_Escape_Simulation)
