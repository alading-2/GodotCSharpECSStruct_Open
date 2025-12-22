# AI Steering - Brotato Project Rules

## 🎯 Core Directive
Implement a high-performance Survivor-like game using Godot 4.5 C# with a **Pseudo-ECS architecture**.

## 🛠️ Implementation Rules

### 1. Coding Style
- **Language**: C# (.NET 8.0).
- **Template**: `public partial class Name : Node`.
- **Naming**: PascalCase for EVERYTHING (Files, Folders, Classes, Props).
- **Namespaces**: None by default (global). Use `namespace BrotatoMy.Test` for tests only.

### 2. Architecture: Pseudo-ECS
- **Composition over Inheritance**: Use independent Nodes as components.
- **Data Storage**: Use `node.GetData()` for runtime state. NEVER store transient state in private fields if it needs to be shared or modified by buffs.
- **Communication**: 
    - **MANDATORY**: C# `event Action<T>` for logic.
    - **CLEANUP**: Unsubscribe in `_ExitTree` (`Damaged = null;`).
    - **AVOID**: Godot Signals in logic code.

### 3. Performance (Hot Paths)
- **Object Pooling**: Mandatory for Bullets, Enemies, Effects, DamageNumbers.
- **No Allocations in `_Process`**:
    - NO `new` (Class, List, Array).
    - NO String concatenation.
    - NO LINQ (`Where`, `Select`, `Any`).
- **Use Structs**: `Vector2`, `Color`, `Rect2` are safe.

### 4. Essential Tools
- **Logging**: `private static readonly Log _log = new Log("Name");`.
- **Dynamic Data**: `node.GetData().Set("Key", value);`.
- **Pools**: `ObjectPoolManager.ReturnToPool(node);`.

### 5. Static Variable Taboo
- **DO NOT** store `Node`, `Resource`, or `GodotObject` in `static` variables. It causes crashes on scene change.

## 📂 Project Map
- `Src/ECS/Components/`: Functional logic nodes.
- `Src/Tools/`: Core framework utilities.
- `Resources/Data/`: Static config files.
- `Assets/`: Visual/Audio assets.
