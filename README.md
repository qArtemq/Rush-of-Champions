# 🏁 Rush of Champions — Arcade Racing Experience

> A Unity-powered racing game that blends customizable cars, cinematic races, and AI competitors across multiple tracks.  
> Tune your ride, outpace rival drivers, and master nitro-fueled sprints to claim the podium!

---

## 🧩 Features

- Multiple race locations (`BrazilRace`, `ParisRace`, `SpaceRace`, `DriftRace`) with unique pacing  
- Adjustable lobby setup: choose laps, bot count, and track rewards before each event  
- Garage with car selection, color customization, and unlockable vehicles purchased via in-game currency  
- Nitro boost system with particle FX, exhaust lighting, and contextual audio feedback  
- Dynamic race HUD: minimap, lap counter, live leaderboard, progress percentage, and finish summary  
- Cinematic intro & finish sequences driven by timeline animation and multi-camera rigs  
- Rich settings menu covering video, audio, control toggles, and fullscreen/resolution presets  

---

## 🎮 Controls

| Action | Keys |
|--------|------|
| Accelerate / Brake | W / S (↑ / ↓) |
| Steer | A / D (← / →) |
| Handbrake | Space |
| Nitro Boost | Q |
| Toggle Headlights | L |
| Reset to Last Checkpoint | R |
| Switch Camera | V |
| Pause / Back | ESC |

---

## 🛠️ Tech Stack

- Unity 2023 LTS (URP) 
- C#  
- Unity Standard Assets — vehicle physics & AI waypoints  
- Unity UI (UGUI) & TextMeshPro  
- Unity Timeline
- Unity Post Processing 3.5  
- Audio Mixer for layered SFX and music control  

---

## 🧠 Architecture Overview

- **Menu.cs** — manages splash flow, garage UI, lobby setup, and car purchasing  
- **RaceManager.cs** — orchestrates race lifecycle, countdown, checkpoints, lap tracking, and leaderboard  
- **RaceManagerButtons.cs** — pause menu, in-race settings, and UI shortcuts  
- **CarController.cs** (`UnityStandardAssets.Vehicles.Car`) — car physics, nitro, lights, speedometer, and AI assist toggles  
- **CarUserControl.cs** — maps player input (keyboard / gamepad) to the car controller  
- **CarCam.cs** — multi-camera follow system with cinematic transitions and finish camera  
- **CarAudio.cs** — engine, tire, and collision audio mixing  
- **Setting.cs** — runtime settings menu with resolution, quality, and audio persistence via `PlayerPrefs`  
- **Checkpoint.cs** — lap progression and respawn positions  

---

## 📸 Screenshots

<img width="2559" height="1439" alt="Screenshot_1" src="https://github.com/user-attachments/assets/02203d0e-d2fb-45b5-a58a-14e46c0a3910" />
<img width="2559" height="1439" alt="Screenshot_2" src="https://github.com/user-attachments/assets/4726badf-48a4-4440-9bee-904697d1fa53" />
<img width="2559" height="1439" alt="Screenshot_3" src="https://github.com/user-attachments/assets/3e5b548e-050a-47c0-9d81-07c85cd07350" />
<img width="2559" height="1439" alt="Screenshot_4" src="https://github.com/user-attachments/assets/9a9bf5cc-50ae-4be9-97fb-514d2eb80bab" />
<img width="2559" height="1439" alt="Screenshot_5" src="https://github.com/user-attachments/assets/0e68b5d8-b7dd-4674-b984-045ae9ed4dd7" />

---

## 📜 License

MIT License — free to use with attribution.
