# 2D Top-Down Shooter

A 2D top-down shooter developed in C# using the Godot game engine.

The game combines procedurally generated mazes, AI-controlled enemies and allies, multiple weapons, power-ups and A* pathfinding to create a replayable wave-based shooter.

## 🎮 Features

- Procedurally generated maze layouts
- Multiple types of AI enemies with different behaviours
- AI allies that fight alongside the player
- A* pathfinding for enemy navigation
- Multiple weapons, including pistols, assault rifles and shotguns
- Multiple power-ups with temporary effects
- Enemy spawning and object pooling to reduce performance overhead
- Collision and line-of-sight detection using Godot RayCast2D
- Wave-based gameplay with increasing difficulty
- Boss enemies
- Player health and scoring systems
- Dynamic weapon and enemy interactions
- Replayable gameplay through randomly generated levels

## 🧠 Technical Highlights

### Procedural Maze Generation

The maze is generated at runtime using a **recursive backtracking algorithm**. The maze is represented using a 2D array, with cells representing either walls or paths.

A random seed is used to ensure that the maze and the corresponding pathfinding grid remain synchronised.

### A* Pathfinding

Enemies use a custom implementation of **A* pathfinding** to navigate through the generated maze.

The algorithm evaluates possible routes using the distance already travelled and a heuristic estimate of the remaining distance, allowing enemies to efficiently find routes towards the player or allies.

### AI

Enemies use different behaviours depending on their type, including:

- Target selection
- Navigation towards targets
- Maintaining distances from the player
- Line-of-sight checks
- Automatic shooting
- Damage and death states

AI allies can also interact with the player and enemies.

### Object Pooling

Enemies are managed using **queues and object pooling** rather than constantly creating and destroying enemy instances.

Defeated enemies can be returned to the pool and reused later, reducing unnecessary object creation and helping maintain performance when large numbers of enemies are present.

### Data Structures

Several data structures are used throughout the project, including:

- **2D arrays** – representing the maze
- **Queues** – managing pooled enemies
- **Dictionaries** – storing and retrieving information such as enemy damage
- **HashSets** – efficiently tracking objects
- **Lists** – storing paths and collections of targets
- **Stacks** – implementing recursive-backtracking maze generation

### Object-Oriented Programming

The project uses C# and object-oriented programming principles to organise the game into separate reusable classes.

Examples include:

- Encapsulation of enemy and weapon properties
- Inheritance from Godot classes such as `CharacterBody2D` and `Node`
- Polymorphism through different enemy implementations
- Reusable systems for spawning, pathfinding and game management

## 🛠️ Technologies

- **C#**
- **Godot Engine**
- **.NET**
- **A* Pathfinding**
- **Procedural Generation**
- **Object-Oriented Programming**

## 📁 Project Structure

Some of the main systems in the project include:

| System | Purpose |
|---|---|
| `MazeGeneration` | Generates and renders the procedural maze |
| `AstarPathfinding` | Calculates routes through the maze |
| `GameManager` | Controls major game systems and game state |
| `EnemySpawner` | Handles enemy spawning and pooling |
| `WeaponSpawner` | Generates weapons and power-ups |
| `AiEnemy` classes | Control different enemy behaviours |
| `AiAlly` | Controls the player's AI ally |
| `Player` | Controls player movement, weapons and health |

## 📸 Gameplay

<img width="2559" height="1439" alt="Screenshot 2026-09-09 180315" src="https://github.com/user-attachments/assets/1ba47337-0629-43b8-828f-7b828857d026" />
<img width="2559" height="1439" alt="Screenshot 2026-09-09 180240" src="https://github.com/user-attachments/assets/8aea6825-74c3-49e3-8d9a-021da869673f" />


## 🚀 Running the Game

1. Clone this repository.
2. Open the project in Godot.
3. Ensure the required C#/.NET version is installed.
4. Open the main scene.
5. Run the project.

## 🎓 About the Project

This project was originally developed as a Computer Science programming project and was designed to demonstrate the practical application of algorithms, data structures and object-oriented programming within a complete game.

The project gave me experience with game-engine architecture, procedural generation, AI, pathfinding, performance optimisation and designing an engaging gameplay experience.

## 👤 Author

**Ben**

Computer Science student at the University of York.
