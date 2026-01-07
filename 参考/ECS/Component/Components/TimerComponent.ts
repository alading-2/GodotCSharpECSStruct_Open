/** @noSelfInFile **/

import { Entity } from "../..";
import { Logger } from "../../../base/object/工具/logger";
import { Component } from "../Component";
import { Timer } from "../../../base/object/工具/Timer/Timer";
import { TimerManager } from "../../../base/object/工具/Timer/TimerManager";

const logger = Logger.createLogger("TimerComponent");

/**
 * 计时器组件属性
 */
interface TimerComponentProps {
    // 是否自动清理已完成的计时器
    autoCleanup?: boolean;
    // 默认标签
    defaultTag?: string;
}

/**
 * 计时器组件 - 轻量级计时器管理包装器
 * 委托所有逻辑到 TimerManager，符合 ECS 架构原则
 */
export class TimerComponent extends Component<TimerComponentProps> {
    // 组件类型名称
    protected static readonly TYPE: string = "TimerComponent";

    // 计时器ID存储（只存储引用，不存储逻辑）
    private timerIds: Set<string> = new Set();

    // TimerManager实例
    private timerManager: TimerManager;

    /**
     * 构造函数
     * @param owner 所属游戏对象
     * @param props 组件属性
     */
    constructor(owner: Entity, props?: TimerComponentProps) {
        super(owner, {
            autoCleanup: true,
            ...props
        });
        this.timerManager = TimerManager.getInstance();
    }

    /**
     * 初始化组件
     */
    initialize(): void {
        // 无需特殊初始化
    }

    // /**
    //  * 更新组件
    //  * 注意：计时器逻辑现在由 TimerManager 统一处理，此方法保留用于兼容性
    //  * @param deltaTime 时间增量
    //  */
    // update(deltaTime: number): void {
    //     // TimerManager 通过全局中心计时器处理所有计时器逻辑
    //     // 此组件不再需要手动更新计时器

    //     // 可选：清理已失效的计时器ID引用
    //     if (this.props.autoCleanup) {
    //         const invalidIds: string[] = [];
    //         for (const id of this.timerIds) {
    //             if (!this.timerManager.getTimerInfo(id)) {
    //                 invalidIds.push(id);
    //             }
    //         }
    //         for (const id of invalidIds) {
    //             this.timerIds.delete(id);
    //         }
    //     }
    // }

    /**
     * 销毁组件
     */
    destroy(): void {
        if (this.props.autoCleanup) {
            this.clearAllTimers();
        }
    }

    /**
     * 添加延迟执行计时器
     * @param duration 延迟时间（秒）
     * @param callback 回调函数
     * @param repeat 是否重复执行
     * @returns 计时器ID
     */
    private setTimeout(duration: number, callback: () => void, repeat: boolean): string {
        const id = this.timerManager.createTimer({
            duration,
            callback,
            repeat,
            entityID: this.owner.getId(),
            tag: this.props.defaultTag,
        });

        this.timerIds.add(id);
        return id;
    }

    /**
     * 添加周期执行计时器
     * @param interval 间隔时间（秒）
     * @param callback 回调函数
     * @returns 计时器ID
     */
    CreateTimer(interval: number, callback: () => void): string {
        return this.setTimeout(interval, callback, true);
    }

    /**
     * 添加延迟执行计时器
     * @param time 延迟时间（秒）
     * @param callback 回调函数
     * @returns 计时器ID
     */
    RunLater(time: number, callback: () => void): string {
        return this.setTimeout(time, callback, false);
    }

    /**
     * 循环计时器，指定次数后停止
     * @param interval 间隔时间（秒）
     * @param count 运行次数
     * @param callback 回调函数，参数为第几次运行
     * @param end 结束回调
     * @param immediate 是否立即执行
     * @returns 计时器ID
     */
    CycleTimer(
        interval: number,
        count: number,
        callback: (n: number) => void,
        end?: () => void,
        immediate: boolean = false
    ): string {
        const id = this.timerManager.createCycleTimer(
            interval,
            count,
            callback,
            end,
            immediate,
            this.owner.getId(),
            this.props.defaultTag
        );

        this.timerIds.add(id);
        return id;
    }

    /**
     * 倒计时功能
     * @param duration 持续时间（秒）
     * @param interval 间隔时间（秒）
     * @param callback 回调函数，参数为已经过去的时间和进度callback: (time: number, progress: number) => void,
     * @param end 结束回调
     * @returns 计时器ID
     */
    CountDown(
        duration: number,
        interval: number,
        callback: (time: number, progress: number) => void,

        end?: () => void
    ): string {
        const id = this.timerManager.createCountDown(
            duration,
            interval,
            callback,
            end,
            this.owner.getId(),
            this.props.defaultTag
        );

        this.timerIds.add(id);
        return id;
    }



    /**
     * 添加帧更新计时器，在duration内播放完动画，每帧调用onUpdate，动画完成后调用onComplete
     * @param duration 总时长（秒）
     * @param onUpdate 每帧更新回调，参数为动画进度（0-1）
     * @param onComplete 动画完成回调
     * @returns 计时器ID
     */
    animate(duration: number, onUpdate: (progress: number) => void, onComplete?: () => void): string {
        let startTime = Timer.second;
        let animationId: string;

        // 创建高频率更新计时器来模拟帧更新
        animationId = this.timerManager.createTimer({
            duration: 0.01, // 每0.01秒更新一次
            repeat: true,
            entityID: this.owner.getId(),
            tag: this.props.defaultTag,
            callback: () => {
                const elapsed = Timer.second - startTime;   //单位：秒
                const progress = Math.min(1, elapsed / duration);

                onUpdate(progress);

                // 动画完成
                if (progress >= 1) {
                    this.removeTimer(animationId);
                    if (onComplete) {
                        onComplete();
                    }
                }
            }
        });

        this.timerIds.add(animationId);
        return animationId;
    }

    /**
     * 取消计时器
     * @param id 计时器ID
     * @returns 是否成功取消
     */
    removeTimer(id: string): boolean {
        if (this.timerIds.has(id)) {
            this.timerIds.delete(id);
            return this.timerManager.destroyTimer(id);
        }
        return false;
    }

    /**
     * 暂停计时器
     * @param id 计时器ID
     * @returns 是否成功暂停
     */
    pauseTimer(id: string): boolean {
        return this.timerManager.pauseTimer(id);
    }

    /**
     * 恢复计时器
     * @param id 计时器ID
     * @returns 是否成功恢复
     */
    resumeTimer(id: string): boolean {
        return this.timerManager.resumeTimer(id);
    }

    /**
     * 清除所有计时器
     */
    clearAllTimers(): void {
        for (const id of this.timerIds) {
            this.timerManager.destroyTimer(id);
        }
        this.timerIds.clear();
    }

    /**
     * 暂停所有计时器
     */
    pauseAllTimers(): void {
        for (const id of this.timerIds) {
            this.timerManager.pauseTimer(id);
        }
    }

    /**
     * 恢复所有计时器
     */
    resumeAllTimers(): void {
        for (const id of this.timerIds) {
            this.timerManager.resumeTimer(id);
        }
    }

    /**
     * 获取活跃计时器数量
     */
    getTimerCount(): number {
        return this.timerIds.size;
    }

    /**
     * 获取所有计时器ID
     * @returns 计时器ID数组
     */
    getTimerIds(): string[] {
        return Array.from(this.timerIds);
    }

    /**
     * 获取计时器详细信息
     * @param id 计时器ID
     * @returns 计时器句柄信息
     */
    getTimerInfo(id: string) {
        return this.timerManager.getTimerInfo(id);
    }

    /**
     * 获取计时器剩余时间（秒）
     * @param id 计时器ID
     * @returns 剩余时间，如果计时器不存在则返回0
     */
    getTimerRemainingTime(id: string): number {
        if (!this.timerIds.has(id)) {
            logger.warn(`尝试获取不属于此组件的计时器剩余时间: ${id}`);
            return 0;
        }
        return this.timerManager.getTimerRemainingTime(id);
    }

    /**
     * 获取计时器进度（0-1）
     * @param id 计时器ID
     * @returns 进度值，如果计时器不存在则返回1
     */
    getTimerProgress(id: string): number {
        if (!this.timerIds.has(id)) {
            logger.warn(`尝试获取不属于此组件的计时器进度: ${id}`);
            return 1;
        }
        return this.timerManager.getTimerProgress(id);
    }

    /**
     * 获取计时器已运行时间（秒）
     * @param id 计时器ID
     * @returns 已运行时间，如果计时器不存在则返回0
     */
    getTimerElapsedTime(id: string): number {
        if (!this.timerIds.has(id)) {
            logger.warn(`尝试获取不属于此组件的计时器已运行时间: ${id}`);
            return 0;
        }
        return this.timerManager.getTimerElapsedTime(id);
    }

    /**
     * 获取所有计时器的剩余时间信息
     * @returns 计时器时间信息数组
     */
    getAllTimersTimeInfo(): Array<{ id: string, remainingTime: number, progress: number, elapsedTime: number }> {
        const result: Array<{ id: string, remainingTime: number, progress: number, elapsedTime: number }> = [];

        for (const id of this.timerIds) {
            const timerInfo = this.timerManager.getTimerInfo(id);
            if (timerInfo) {
                result.push({
                    id,
                    remainingTime: timerInfo.timer.getRemainingTime(),
                    progress: timerInfo.timer.getProgress(),
                    elapsedTime: timerInfo.timer.getElapsedTime()
                });
            }
        }

        return result;
    }

}
