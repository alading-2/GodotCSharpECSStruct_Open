import Logger from "../../base/object/工具/logger";
import { System } from "./System";

const logger = Logger.createLogger("SystemManager");

/**
 * System管理器
 * 
 * 负责管理所有System的生命周期和执行顺序，确保系统按照正确的优先级更新。
 * 遵循现代ECS架构，SystemManager是驱动整个游戏世界逻辑更新的核心。
 */
export class SystemManager {
    private static instance: SystemManager;

    private systems: System[] = [];
    private isRunning: boolean = false;

    private constructor() {
        logger.debug("系统管理器已创建");
    }
    public static getInstance(): SystemManager {
        if (!SystemManager.instance) {
            SystemManager.instance = new SystemManager();
        }
        return SystemManager.instance;
    }

    /**
     * 注册并初始化一个System
     * @param system 要注册的系统实例
     */
    public registerAndInitSystem(system: System): void {
        const systemName = system.getSystemName();
        if (this.systems.some(s => s.getSystemName() === systemName)) {
            logger.warn(`系统已注册: ${systemName}`);
            return;
        }

        this.systems.push(system);
        system.initialize();
        logger.info(`系统已注册并初始化: ${systemName}`);
    }

    /**
     * 注销并销毁一个System
     * @param systemName 要注销的系统名称
     */
    public unregisterSystem(systemName: string): void {
        const index = this.systems.findIndex(s => s.getSystemName() === systemName);
        if (index === -1) {
            logger.warn(`系统未找到: ${systemName}`);
            return;
        }

        const [system] = this.systems.splice(index, 1);
        system.destroy();
        logger.info(`系统已注销: ${systemName}`);
    }

    /**
     * 启动所有系统的更新循环
     */
    public start(): void {
        this.isRunning = true;
        logger.info("系统管理器已启动");
    }

    /**
     * 停止所有系统的更新循环
     */
    public stop(): void {
        this.isRunning = false;
        logger.info("系统管理器已停止");
    }

    /**
     * 更新所有已注册且正在运行的系统
     */
    // public updateAll(): void {
    //     if (!this.isRunning) {
    //         return;
    //     }

    //     for (const system of this.systems) {
    //         system.update();
    //     }
    // }

    /**
     * 销毁所有已注册的系统
     */
    public destroyAll(): void {
        for (const system of this.systems) {
            system.destroy();
        }
        this.systems = [];
        this.isRunning = false;
        logger.info("所有系统已销毁");
    }

    /**
     * 根据优先级对系统进行排序
     */
    private sortSystems(): void {
        this.systems.sort((a, b) => b.getPriority() - a.getPriority());
        const order = this.systems.map(s => s.getSystemName()).join(', ');
        logger.debug(`系统执行顺序已更新: ${order}`);
    }

    /**
     * 按名称获取系统实例
     * @param systemName 系统名称
     * @returns 系统实例或undefined
     */
    public getSystem<T extends System>(systemName: string): T | undefined {
        return this.systems.find(s => s.getSystemName() === systemName) as T | undefined;
    }

    /**
     * 获取所有已注册的系统实例
     * @returns 系统实例数组
     */
    public getAllSystems(): System[] {
        return [...this.systems];
    }
}