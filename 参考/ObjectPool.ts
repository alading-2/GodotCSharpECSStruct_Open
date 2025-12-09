/** @noSelfInFile **/


import { MyMath } from "../../math/MyMath";
import { Logger } from "./logger";
import { Timer } from "./Timer/Timer";


const logger = Logger.createLogger("ObjectPool");


/**
 * 可池化对象接口 - 实现此接口的对象可以被对象池管理
 */
export interface IPoolable {
  /**
   * 重置对象状态，准备复用
   */
  reset?(): void;


  /**
   * 对象被从池中取出时调用
   */
  onAcquire?(): void;


  /**
   * 对象被归还到池中时调用
   */
  onRelease?(): void;
}


/**
 * 对象池配置接口
 */
export interface ObjectPoolConfig {
  /** 池的最大容量，超过此容量的对象将被丢弃 */
  maxSize?: number;
  /** 池的初始容量，预先创建的对象数量 */
  initialSize?: number;
  /** 是否启用统计信息收集 */
  enableStats?: boolean;
  /** 池的名称，用于调试和统计 */
  name?: string;
  /** 自动清理间隔（秒），0表示不自动清理 */
  autoCleanupInterval?: number;
  /** 清理时保留的最小对象数量 */
  minRetainSize?: number;
}


/**
 * 对象池统计信息
 */
export interface PoolStats {
  /** 池名称 */
  name: string;
  /** 当前池中对象数量 */
  poolSize: number;
  /** 总创建次数 */
  totalCreated: number;
  /** 总获取次数 */
  totalAcquired: number;
  /** 总归还次数 */
  totalReleased: number;
  /** 总丢弃次数（超过最大容量） */
  totalDiscarded: number;
  /** 缓存命中率 */
  hitRate: number;
  /** 最大池大小记录 */
  maxPoolSize: number;
}


/**
 * 通用对象池类 - 用于管理对象的复用，减少GC压力
 * 
 * @example
 * ``typescript
 * // 创建对象池
 * const bulletPool = new ObjectPool(() => new Bullet(), {
 *   maxSize: 100,
 *   initialSize: 10,
 *   name: "BulletPool"
 * });
 * 
 * // 获取对象
 * const bullet = bulletPool.acquire();
 * 
 * // 使用对象...
 * 
 * // 归还对象
 * bulletPool.release(bullet);
 * ``
 */
export class ObjectPool<T = any> {
  /** 对象池数组,用于存储可重用的对象 */
  private pool: T[] = [];


  /** 创建新对象的工厂函数 */
  private readonly createFn: () => T;


  /** 对象池配置信息,包含了所有必需的配置项 */
  private readonly config: ObjectPoolConfig;


  /** 对象池运行时统计信息 */
  private stats: PoolStats;


  /** 自动清理定时器实例 */
  private cleanupTimer?: Timer; // Timer类型


  /**
   * 创建对象池
   * @param createFn 对象创建函数
   * @param config 池配置
   */
  constructor(createFn: () => T, config: ObjectPoolConfig = {}) {
    this.createFn = createFn;
    this.config = {
      initialSize: config.initialSize ?? 100, // 初始化创建的对象数量
      enableStats: config.enableStats ?? true,  // 是否启用统计信息收集
      name: config.name ?? Pool_${os.time()},  // 池的名称
      autoCleanupInterval: config.autoCleanupInterval ?? 0, // 自动清理间隔（秒），0表示不自动清理
    };


    // 初始化统计信息
    this.stats = {
      name: this.config.name,
      poolSize: 0,
      totalCreated: 0,
      totalAcquired: 0,
      totalReleased: 0,
      totalDiscarded: 0,
      hitRate: 0,
      maxPoolSize: 0
    };


    // 预创建对象
    this.prewarm();


    // 设置自动清理
    if (this.config.autoCleanupInterval > 0) {
      this.setupAutoCleanup();
    }


    // 注册到全局池管理器
    ObjectPoolManager.registerPool(this);
  }


  /**
   * 预热池 - 预先创建指定数量的对象
   */
  private prewarm(): void {
    for (let i = 0; i < this.config.initialSize; i++) {
      const obj = this.createFn();
      if (this.config.enableStats) {
        this.stats.totalCreated++;
      }
      this.pool.push(obj);
    }
    this.updatePoolStats();
  }


  /**
   * 设置自动清理定时器
   */
  private setupAutoCleanup(): void {
    this.cleanupTimer = Timer.Create(this.config.autoCleanupInterval, true, () => {
      this.cleanup();
    });
  }


  /**
   * 从池中获取对象
   * @returns 对象实例
   */
  acquire(): T {
    let obj: T;


    if (this.pool.length > 0) {
      // 从池中取出对象
      obj = this.pool.pop()!;
      if (this.config.enableStats) {
        this.stats.totalAcquired++;
      }
    } else {
      // 池为空，创建新对象
      obj = this.createFn();
      if (this.config.enableStats) {
        this.stats.totalCreated++;
        this.stats.totalAcquired++;
      }
    }


    // 调用对象的获取回调（如果实现了IPoolable接口）
    if (obj && typeof (obj as any).onAcquire === 'function') {
      (obj as IPoolable).onAcquire();
    }


    this.updatePoolStats();
    return obj;
  }


  /**
   * 将对象归还到池中
   * @param obj 要归还的对象
   */
  release(obj: T): void {
    if (!obj) {
      logger.warn([${this.config.name}] 尝试归还null对象);
      return;
    }


    // 调用对象的归还回调（如果实现了IPoolable接口）
    if (obj && typeof (obj as any).onRelease === 'function') {
      (obj as IPoolable).onRelease();
    }


    // 重置对象状态（如果实现了IPoolable接口）
    if (obj && typeof (obj as any).reset === 'function') {
      (obj as IPoolable).reset();
    }


    // 检查池容量
    if (this.config?.maxSize && (this.pool.length >= this.config.maxSize)) {
      // 池已满，丢弃对象
      if (this.config.enableStats) {
        this.stats.totalDiscarded++;
      }
      return;
    }


    // 归还到池中
    this.pool.push(obj);
    if (this.config.enableStats) {
      this.stats.totalReleased++;
    }


    this.updatePoolStats();
  }


  /**
   * 批量获取对象
   * @param count 获取数量
   * @returns 对象数组
   */
  acquireMultiple(count: number): T[] {
    const objects: T[] = [];
    for (let i = 0; i < count; i++) {
      objects.push(this.acquire());
    }
    return objects;
  }


  /**
   * 批量归还对象
   * @param objects 要归还的对象数组
   */
  releaseMultiple(objects: T[]): void {
    for (const obj of objects) {
      this.release(obj);
    }
  }


  /**
   * 清理池中的对象，保留最小数量
   */
  cleanup(): void {
    if (!this.config.minRetainSize) {
      return
    }
    const targetSize = MyMath.Max([this.config.minRetainSize, 0]);
    const removeCount = MyMath.Max([0, this.pool.length - targetSize]);


    if (removeCount > 0) {
      this.pool.splice(targetSize);
      logger.info([${this.config.name}] 清理了 ${removeCount} 个对象，当前池大小: ${this.pool.length});
    }


    this.updatePoolStats();
  }


  /**
   * 清空池中所有对象
   */
  clear(): void {
    this.pool = [];
    this.updatePoolStats();
    logger.info([${this.config.name}] 池已清空);
  }


  /**
   * 销毁对象池
   */
  destroy(): void {
    // 清理定时器
    if (this.cleanupTimer) {
      this.cleanupTimer.Null();
      this.cleanupTimer = null;
    }


    // 清空池
    this.clear();


    // 从全局管理器中注销
    ObjectPoolManager.unregisterPool(this);


    logger.info([${this.config.name}] 对象池已销毁);
  }


  /**
   * 更新池统计信息
   */
  private updatePoolStats(): void {
    if (!this.config.enableStats) return;


    this.stats.poolSize = this.pool.length;
    this.stats.maxPoolSize = MyMath.Max([this.stats.maxPoolSize, this.pool.length]);


    // 计算命中率
    if (this.stats.totalAcquired > 0) {
      const hits = this.stats.totalAcquired - this.stats.totalCreated + this.config.initialSize;
      this.stats.hitRate = MyMath.Max([0, hits / this.stats.totalAcquired]);
    }
  }


  /**
   * 获取池统计信息
   */
  getStats(): PoolStats {
    return { ...this.stats };
  }


  /**
   * 获取当前池大小
   */
  get size(): number {
    return this.pool.length;
  }


  /**
   * 获取池名称
   */
  get name(): string {
    return this.config.name;
  }


  /**
   * 检查池是否为空
   */
  get isEmpty(): boolean {
    return this.pool.length === 0;
  }


  /**
   * 检查池是否已满
   */
  get isFull(): boolean {
    if (!this.config.maxSize) {
      return false;
    }
    return this.pool.length >= this.config.maxSize;
  }


}


/**
 * 全局对象池管理器 - 管理所有对象池的生命周期和统计
 */
export class ObjectPoolManager {
  private static pools: Map<string, ObjectPool> = new Map();
  private static globalStats = {
    totalPools: 0,  // 总对象池数量
    totalObjects: 0,  // 总对象数量
    totalMemoryUsage: 0 // 总内存占用
  };


  /**
   * 注册对象池
   */
  static registerPool(pool: ObjectPool): void {
    this.pools.set(pool.name, pool);
    this.globalStats.totalPools++;
    logger.info(对象池 [${pool.name}] 已注册);
  }


  /**
   * 注销对象池
   */
  static unregisterPool(pool: ObjectPool): void {
    if (this.pools.delete(pool.name)) {
      this.globalStats.totalPools--;
      logger.info(对象池 [${pool.name}] 已注销);
    }
  }


  /**
   * 获取指定名称的对象池
   */
  static getPool(name: string): ObjectPool | undefined {
    return this.pools.get(name);
  }


  /**
   * 获取所有对象池的统计信息
   */
  static getAllStats(): { [poolName: string]: PoolStats } {
    const allStats: { [poolName: string]: PoolStats } = {};
    for (const [name, pool] of this.pools) {
      allStats[name] = pool.getStats();
    }
    return allStats;
  }


  /**
   * 清理所有对象池
   */
  static cleanupAll(): void {
    for (const pool of this.pools.values()) {
      pool.cleanup();
    }
    logger.info("所有对象池清理完成");
  }


  /**
   * 获取全局统计信息
   */
  static getGlobalStats() {
    let totalObjects = 0;
    for (const pool of this.pools.values()) {
      totalObjects += pool.size;
    }


    return {
      ...this.globalStats,
      totalObjects,
      activePools: this.pools.size
    };
  }


  /**
   * 销毁所有对象池
   */
  static destroyAll(): void {
    for (const pool of this.pools.values()) {
      pool.destroy();
    }
    this.pools.clear();
    this.globalStats = {
      totalPools: 0,
      totalObjects: 0,
      totalMemoryUsage: 0
    };
    logger.info("所有对象池已销毁");
  }
}