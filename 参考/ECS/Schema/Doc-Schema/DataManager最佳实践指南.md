# DataManager 最佳实践指南

## 📋 概述

本指南提供了使用 DataManager 的最佳实践，帮助开发者充分利用新的数据管理架构，避免常见陷阱，并优化性能。

## 🎯 核心原则

### 1. 单一职责原则
- **DataManager** 只负责数据存储和管理
- **Component** 负责业务逻辑和行为
- **Entity** 作为容器协调两者

### 2. 数据驱动设计
- 优先使用 Schema 定义数据结构
- 通过数据变化驱动游戏逻辑
- 避免在 DataManager 中包含业务逻辑

### 3. 类型安全
- 始终使用 TypeScript 接口定义数据结构
- 利用泛型确保类型安全
- 在编译时捕获类型错误

## 🏗️ Schema 设计最佳实践

### 1. 接口定义

```typescript
// ✅ 好的做法：清晰的接口定义
interface PlayerData {
    // 基础属性
    name: string;
    level: number;
    experience: number;
    
    // 资源
    gold: number;
    wood: number;
    
    // 状态
    isOnline: boolean;
    isReady: boolean;
}

// ❌ 避免：模糊的属性名
interface BadPlayerData {
    data1: any;  // 不明确的命名
    value: number;  // 过于通用
    flag: boolean;  // 不知道表示什么
}
```

### 2. Schema 约束

```typescript
// ✅ 好的做法：合理的约束
const PLAYER_SCHEMA: Schema<PlayerData> = {
    interfaceName: "PlayerData",
    description: "玩家数据Schema",
    version: "1.0.0",
    properties: [
        {
            key: "name",
            type: "string",
            defaultValue: "Unknown Player",
            category: "基础",
            description: "玩家名称",
            constraints: {
                minLength: 1,
                maxLength: 20,
                pattern: /^[a-zA-Z0-9_\u4e00-\u9fa5]+$/  // 允许字母、数字、下划线、中文
            }
        },
        {
            key: "level",
            type: "number",
            defaultValue: 1,
            category: "属性",
            description: "玩家等级",
            constraints: {
                min: 1,
                max: 100,
                step: 1  // 整数
            }
        },
        {
            key: "gold",
            type: "number",
            defaultValue: 0,
            category: "资源",
            description: "金币数量",
            constraints: {
                min: 0,
                max: 999999
            }
        }
    ]
};
```

### 3. 计算属性

```typescript
// ✅ 好的做法：使用计算属性
const UNIT_SCHEMA: Schema<UnitData> = {
    // ... 其他配置
    computed: [
        {
            key: "healthPercentage",
            dependencies: ["health", "maxHealth"],
            compute: (data) => (data.health / data.maxHealth) * 100,
            cache: true,  // 启用缓存
            description: "生命值百分比"
        },
        {
            key: "isLowHealth",
            dependencies: ["healthPercentage"],
            compute: (data) => data.healthPercentage < 30,
            cache: true,
            description: "是否生命值较低"
        }
    ]
};
```

## 💾 DataManager 使用最佳实践

### 1. 初始化

```typescript
// ✅ 好的做法：在Entity构造函数中初始化
class Player extends Entity {
    constructor(id: string, playerName: string) {
        super("Player", id);
        
        // 立即添加数据管理器
        this.addDataManager("PlayerData", {
            name: playerName,
            level: 1,
            gold: 100
        });
    }
    
    initialize(): void {
        // 设置主数据管理器
        const dataManager = this.getDataManager("PlayerData");
        if (dataManager) {
            this.setPrimaryDataManager(dataManager);
        }
    }
}

// ❌ 避免：延迟初始化导致的空指针
class BadPlayer extends Entity {
    initialize(): void {
        // 可能在某些组件已经尝试访问数据时才添加
        this.addDataManager("PlayerData", {});
    }
}
```

### 2. 数据访问

```typescript
// ✅ 好的做法：安全的数据访问
class PlayerComponent extends Component {
    private dataManager: DataManager<PlayerData> | null = null;
    
    initialize(): void {
        this.dataManager = this.owner.getDataManager("PlayerData") as DataManager<PlayerData>;
        
        if (!this.dataManager) {
            console.error("PlayerComponent: 无法获取PlayerData");
            return;
        }
        
        // 设置数据监听
        this.setupDataListeners();
    }
    
    private setupDataListeners(): void {
        if (!this.dataManager) return;
        
        this.dataManager.onPropertyChanged("level", (oldValue, newValue) => {
            this.onLevelChanged(oldValue, newValue);
        });
    }
    
    public getPlayerLevel(): number {
        return this.dataManager?.get("level") ?? 1;
    }
    
    public addGold(amount: number): boolean {
        if (!this.dataManager) return false;
        
        return this.dataManager.addValue("gold", amount);
    }
}

// ❌ 避免：不安全的访问
class BadPlayerComponent extends Component {
    public getPlayerLevel(): number {
        // 没有空值检查，可能导致运行时错误
        return this.owner.getDataManager("PlayerData")!.get("level");
    }
}
```

### 3. 批量操作

```typescript
// ✅ 好的做法：使用批量操作
class InventoryComponent extends Component {
    public levelUp(): void {
        const dataManager = this.getDataManager();
        if (!dataManager) return;
        
        // 批量更新多个属性
        dataManager.setMultiple({
            level: dataManager.get("level") + 1,
            experience: 0,
            gold: dataManager.get("gold") + 100,
            maxHealth: dataManager.get("maxHealth") + 10
        });
    }
    
    private getDataManager(): DataManager<PlayerData> | null {
        return this.owner.getDataManager("PlayerData") as DataManager<PlayerData>;
    }
}

// ❌ 避免：多次单独操作
class BadInventoryComponent extends Component {
    public levelUp(): void {
        const dataManager = this.getDataManager();
        if (!dataManager) return;
        
        // 每次操作都会触发事件和验证
        dataManager.set("level", dataManager.get("level") + 1);
        dataManager.set("experience", 0);
        dataManager.set("gold", dataManager.get("gold") + 100);
        dataManager.set("maxHealth", dataManager.get("maxHealth") + 10);
    }
}
```

## 🎭 事件处理最佳实践

### 1. 事件监听

```typescript
// ✅ 好的做法：结构化的事件处理
class HealthComponent extends Component {
    private dataManager: DataManager<UnitData> | null = null;
    private eventSubscriptions: Array<() => void> = [];
    
    initialize(): void {
        this.dataManager = this.owner.getDataManager("UnitData") as DataManager<UnitData>;
        if (!this.dataManager) return;
        
        // 注册事件监听器并保存取消订阅的方法
        this.dataManager.onPropertyChanged("health", this.onHealthChanged.bind(this));
        this.dataManager.onPropertyChanged("maxHealth", this.onMaxHealthChanged.bind(this));
    }
    
    destroy(): void {
        // 清理事件监听器
        this.eventSubscriptions.forEach(unsubscribe => unsubscribe());
        this.eventSubscriptions = [];
    }
    
    private onHealthChanged(oldValue: number, newValue: number): void {
        if (newValue <= 0) {
            this.handleDeath();
        } else if (newValue < oldValue) {
            this.handleDamage(oldValue - newValue);
        } else {
            this.handleHealing(newValue - oldValue);
        }
    }
    
    private onMaxHealthChanged(oldValue: number, newValue: number): void {
        // 确保当前生命值不超过最大值
        const currentHealth = this.dataManager!.get("health");
        if (currentHealth > newValue) {
            this.dataManager!.set("health", newValue);
        }
    }
}
```

### 2. 事件传播

```typescript
// ✅ 好的做法：合理的事件传播
class CombatComponent extends Component {
    private onDamageDealt(damage: number, target: Entity): void {
        // 触发Entity级别的事件
        this.owner.emit("combat.damageDealt", {
            damage,
            target,
            source: this.owner
        });
        
        // 可以被其他组件监听
    }
    
    private onDamageReceived(damage: number, source: Entity): void {
        this.owner.emit("combat.damageReceived", {
            damage,
            source,
            target: this.owner
        });
    }
}
```

## 🚀 性能优化最佳实践

### 1. 缓存策略

```typescript
// ✅ 好的做法：智能缓存
class AttributeComponent extends Component {
    private cachedTotalDamage: number | null = null;
    private cacheInvalidated: boolean = true;
    
    initialize(): void {
        const dataManager = this.getDataManager();
        if (!dataManager) return;
        
        // 监听影响计算的属性变化
        dataManager.onPropertyChanged("baseDamage", () => {
            this.invalidateCache();
        });
        
        dataManager.onPropertyChanged("damageBonus", () => {
            this.invalidateCache();
        });
    }
    
    public getTotalDamage(): number {
        if (this.cacheInvalidated || this.cachedTotalDamage === null) {
            this.cachedTotalDamage = this.calculateTotalDamage();
            this.cacheInvalidated = false;
        }
        
        return this.cachedTotalDamage;
    }
    
    private calculateTotalDamage(): number {
        const dataManager = this.getDataManager();
        if (!dataManager) return 0;
        
        const baseDamage = dataManager.get("baseDamage");
        const damageBonus = dataManager.get("damageBonus");
        
        return baseDamage + damageBonus;
    }
    
    private invalidateCache(): void {
        this.cacheInvalidated = true;
    }
}
```

### 2. 减少不必要的操作

```typescript
// ✅ 好的做法：条件检查
class RegenerationComponent extends Component {
    update(deltaTime: number): void {
        const dataManager = this.getDataManager();
        if (!dataManager) return;
        
        const currentHealth = dataManager.get("health");
        const maxHealth = dataManager.get("maxHealth");
        
        // 只在需要时进行恢复
        if (currentHealth < maxHealth && currentHealth > 0) {
            const regenRate = dataManager.get("healthRegen");
            const newHealth = Math.min(maxHealth, currentHealth + regenRate * deltaTime);
            
            // 只在值真正改变时更新
            if (Math.abs(newHealth - currentHealth) > 0.01) {
                dataManager.set("health", newHealth);
            }
        }
    }
}

// ❌ 避免：无条件的频繁更新
class BadRegenerationComponent extends Component {
    update(deltaTime: number): void {
        const dataManager = this.getDataManager();
        if (!dataManager) return;
        
        // 每帧都更新，即使不需要
        const currentHealth = dataManager.get("health");
        dataManager.set("health", currentHealth + 0.1 * deltaTime);
    }
}
```

## 🔧 调试和测试最佳实践

### 1. 调试信息

```typescript
// ✅ 好的做法：丰富的调试信息
class DebugComponent extends Component {
    public getDebugInfo(): Record<string, any> {
        const dataManager = this.getDataManager();
        if (!dataManager) return {};
        
        return {
            componentType: this.constructor.name,
            entityId: this.owner.getId(),
            dataSnapshot: dataManager.getData(),
            schemaInfo: {
                name: dataManager.getSchemaName(),
                version: dataManager.getSchema().version
            },
            validationResult: dataManager.validateData()
        };
    }
    
    public logState(): void {
        console.log(`[${this.constructor.name}]`, this.getDebugInfo());
    }
}
```

### 2. 单元测试

```typescript
// ✅ 好的做法：可测试的组件设计
class TestableComponent extends Component {
    // 提供测试友好的接口
    public setDataManagerForTesting(dataManager: DataManager<any>): void {
        // 仅用于测试
        (this as any).dataManager = dataManager;
    }
    
    public getDataManagerForTesting(): DataManager<any> | null {
        return (this as any).dataManager;
    }
    
    // 将复杂逻辑提取为可测试的纯函数
    public static calculateDamage(baseDamage: number, multiplier: number): number {
        return Math.floor(baseDamage * multiplier);
    }
}
```

## ⚠️ 常见陷阱和避免方法

### 1. 内存泄漏

```typescript
// ❌ 问题：忘记清理事件监听器
class LeakyComponent extends Component {
    initialize(): void {
        const dataManager = this.getDataManager();
        dataManager?.onPropertyChanged("health", (old, new_) => {
            // 这个监听器永远不会被清理
        });
    }
}

// ✅ 解决：正确清理资源
class CleanComponent extends Component {
    private cleanupFunctions: Array<() => void> = [];
    
    initialize(): void {
        const dataManager = this.getDataManager();
        if (!dataManager) return;
        
        const cleanup = dataManager.onPropertyChanged("health", (old, new_) => {
            // 处理逻辑
        });
        
        this.cleanupFunctions.push(cleanup);
    }
    
    destroy(): void {
        this.cleanupFunctions.forEach(cleanup => cleanup());
        this.cleanupFunctions = [];
    }
}
```

### 2. 循环依赖

```typescript
// ❌ 问题：组件间的循环依赖
class BadComponentA extends Component {
    update(): void {
        const componentB = this.owner.getComponent(BadComponentB);
        componentB?.doSomething();
    }
}

class BadComponentB extends Component {
    doSomething(): void {
        const componentA = this.owner.getComponent(BadComponentA);
        componentA?.update();  // 循环调用
    }
}

// ✅ 解决：通过事件解耦
class GoodComponentA extends Component {
    update(): void {
        this.owner.emit("componentA.updated", { data: "some data" });
    }
}

class GoodComponentB extends Component {
    initialize(): void {
        this.owner.on("componentA.updated", (data) => {
            this.handleComponentAUpdate(data);
        });
    }
    
    private handleComponentAUpdate(data: any): void {
        // 处理更新
    }
}
```

### 3. 数据不一致

```typescript
// ❌ 问题：直接修改数据对象
class BadDataAccess extends Component {
    modifyData(): void {
        const dataManager = this.getDataManager();
        const data = dataManager?.getData();
        
        if (data) {
            // 直接修改返回的对象，绕过了验证和事件
            (data as any).health = -100;
        }
    }
}

// ✅ 解决：始终通过DataManager接口操作
class GoodDataAccess extends Component {
    modifyData(): void {
        const dataManager = this.getDataManager();
        if (!dataManager) return;
        
        // 通过正确的接口修改数据
        dataManager.set("health", 0);
    }
}
```

## 📊 性能监控

```typescript
// 性能监控工具
class DataManagerProfiler {
    private static operationCounts: Map<string, number> = new Map();
    private static operationTimes: Map<string, number> = new Map();
    
    static profileOperation<T>(operationName: string, operation: () => T): T {
        const startTime = performance.now();
        
        try {
            const result = operation();
            return result;
        } finally {
            const endTime = performance.now();
            const duration = endTime - startTime;
            
            // 记录操作次数
            const count = this.operationCounts.get(operationName) || 0;
            this.operationCounts.set(operationName, count + 1);
            
            // 记录总耗时
            const totalTime = this.operationTimes.get(operationName) || 0;
            this.operationTimes.set(operationName, totalTime + duration);
        }
    }
    
    static getReport(): Record<string, any> {
        const report: Record<string, any> = {};
        
        for (const [operation, count] of this.operationCounts) {
            const totalTime = this.operationTimes.get(operation) || 0;
            report[operation] = {
                count,
                totalTime: totalTime.toFixed(2) + 'ms',
                averageTime: (totalTime / count).toFixed(2) + 'ms'
            };
        }
        
        return report;
    }
}
```

## 🎯 总结

### 核心要点
1. **明确职责分离**：DataManager 管理数据，Component 处理逻辑
2. **类型安全优先**：充分利用 TypeScript 的类型系统
3. **事件驱动架构**：通过数据变更事件驱动业务逻辑
4. **性能意识**：合理使用缓存，避免不必要的操作
5. **资源管理**：正确清理事件监听器和其他资源

### 开发流程
1. 设计数据接口和 Schema
2. 注册 Schema 到 SchemaRegistry
3. 在 Entity 中添加 DataManager
4. 在 Component 中安全访问和操作数据
5. 通过事件处理数据变更
6. 添加适当的调试和监控

遵循这些最佳实践，可以构建出高性能、可维护、类型安全的游戏系统。