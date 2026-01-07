/** @noSelfInFile **/

import { Component } from "../Component";
import { Entity } from "../../Entity";
import { DataManager } from "../DataManager";
import { Logger } from "../../../base/object/工具/logger";
import { Camera } from "../../../base/war3/Camera";
import { Timer } from "../../../base/object/工具/Timer";
import { EventTypes } from "../../types/EventTypes";
import { PLAYER_SCHEMA } from "../../Schema/PlayerSchema";
import type { Inte_PlayerSchema } from "../../Schema/PlayerSchema";

const logger = Logger.createLogger("PlayerComponent");

/**
 * 玩家组件属性接口
 */
interface PlayerComponentProps {
    enabled?: boolean;
    playerHandle?: any;
    playerId?: number;
    playerName?: string;
    isLocal?: boolean;
    updateInterval?: number; // 更新间隔（毫秒）
    enableCamera?: boolean;
    enableResourceTracking?: boolean;
    enableStatusTracking?: boolean;
}

/**
 * 位置接口
 */
interface Position {
    x: number;
    y: number;
    z?: number;
}

/**
 * 玩家组件
 * 基于ECS框架管理War3玩家的核心功能
 * 使用Schema系统进行数据管理
 */
export class PlayerComponent extends Component<PlayerComponentProps> {
    // 组件类型标识
    protected static readonly TYPE: string = "PlayerComponent";

    // ======================核心依赖====================================================
    private playerDataManager: DataManager<Inte_PlayerSchema> | null = null;
    private camera: Camera | null = null;
    private updateTimer: any = null;

    // ======================War3句柄====================================================
    private player: any = null;

    // ======================缓存数据====================================================
    private lastUpdateTime: number = 0;
    private isDirty: boolean = false;

    // ======================默认属性====================================================
    private readonly defaultProps: Required<PlayerComponentProps> = {
        enabled: true,
        playerHandle: null,
        playerId: -1,
        playerName: "",
        isLocal: false,
        updateInterval: 100, // 100ms更新间隔
        enableCamera: true,
        enableResourceTracking: true,
        enableStatusTracking: true
    };

    /**
     * 获取组件类型
     */
    static getType(): string {
        return PlayerComponent.TYPE;
    }

    /**
     * 构造函数
     */
    constructor(owner: Entity, props?: PlayerComponentProps) {
        super(owner, { ...props });

        // 合并默认属性
        this.props = { ...this.defaultProps, ...props };

        // 设置War3玩家句柄
        this.player = this.props.playerHandle;
    }

    /**
     * 初始化组件
     */
    initialize(): void {
        if (this.initialized) {
            return;
        }

        try {
            // 获取或创建PlayerDataManager
            this.playerDataManager = this.owner.getDataManager<Inte_PlayerSchema>("Inte_Player") ||
                this.owner.addDataManager<Inte_PlayerSchema>("Inte_Player", PLAYER_SCHEMA);

            if (!this.playerDataManager) {
                throw new Error("Failed to initialize PlayerDataManager");
            }

            // 初始化默认数据
            this.initializePlayerData();

            // 设置数据监听器
            this.setupDataListeners();

            // 设置玩家属性
            this.setupPlayerProperties();

            // 初始化子系统
            if (this.props.enableCamera) {
                this.initializeCamera();
            }

            // 设置事件监听
            this.setupEventListeners();

            // 启动更新循环
            this.startUpdateLoop();

            super.initialize();
            logger.debug(`PlayerComponent initialized for player ${this.getPlayerId()}`);
        } catch (error) {
            logger.error(`Failed to initialize PlayerComponent: ${error}`);
            throw error;
        }
    }

    /**
     * 更新组件
     */
    update(deltaTime: number): void {
        if (!this.initialized || !this.props.enabled) {
            return;
        }

        // 基于时间间隔的更新策略
        const currentTime = GetGameTimeOfDay();
        if (currentTime - this.lastUpdateTime < this.props.updateInterval! / 1000) {
            return;
        }

        try {
            // 更新资源状态
            if (this.props.enableResourceTracking) {
                this.updateResources();
            }

            // 更新玩家状态
            if (this.props.enableStatusTracking) {
                this.updatePlayerStatus();
            }

            // 更新镜头
            if (this.camera) {
                this.camera.update(deltaTime);
            }

            this.lastUpdateTime = currentTime;
        } catch (error) {
            logger.error(`PlayerComponent update failed: ${error}`);
        }
    }

    /**
     * 销毁组件
     */
    destroy(): void {
        try {
            // 停止更新循环
            this.stopUpdateLoop();

            // 清理镜头
            if (this.camera) {
                this.camera.destroy();
                this.camera = null;
            }

            // 清理数据管理器引用
            this.playerDataManager = null;
            this.player = null;

            super.destroy();
            logger.debug(`PlayerComponent destroyed for player ${this.getPlayerId()}`);
        } catch (error) {
            logger.error(`Failed to destroy PlayerComponent: ${error}`);
        }
    }

    /**
     * 初始化玩家数据
     */
    private initializePlayerData(): void {
        if (!this.playerDataManager) {
            throw new Error("PlayerDataManager is required for data initialization");
        }

        try {
            // 使用批量设置初始化默认数据
            this.playerDataManager.setMultiple({
                "玩家ID": this.props.playerId || -1,
                "玩家名称": this.props.playerName || "",
                "是否本地玩家": this.props.isLocal || false,
                "是否在线": true,
                "是否失败": false,
                "是否胜利": false,
                "金币": 0,
                "木材": 0,
                "人口使用": 0,
                "人口上限": 0
            });

            logger.debug("PlayerComponent data initialized");
        } catch (error) {
            logger.error(`Failed to initialize player data: ${error}`);
            throw error;
        }
    }

    /**
     * 设置数据监听器
     */
    private setupDataListeners(): void {
        if (!this.playerDataManager) return;

        // 监听资源变化
        this.playerDataManager.onPropertyChanged("金币", (newValue, oldValue) => {
            this.onResourceChanged({ type: "gold", newValue, oldValue });
        });

        this.playerDataManager.onPropertyChanged("木材", (newValue, oldValue) => {
            this.onResourceChanged({ type: "lumber", newValue, oldValue });
        });

        this.playerDataManager.onPropertyChanged("人口使用", (newValue, oldValue) => {
            this.onResourceChanged({ type: "foodUsed", newValue, oldValue });
        });

        this.playerDataManager.onPropertyChanged("人口上限", (newValue, oldValue) => {
            this.onResourceChanged({ type: "foodCap", newValue, oldValue });
        });

        // 监听状态变化
        this.playerDataManager.onPropertyChanged("是否失败", (newValue, oldValue) => {
            this.onStatusChanged({ type: "defeated", newValue, oldValue });
        });

        this.playerDataManager.onPropertyChanged("是否胜利", (newValue, oldValue) => {
            this.onStatusChanged({ type: "victorious", newValue, oldValue });
        });

        this.playerDataManager.onPropertyChanged("是否在线", (newValue, oldValue) => {
            this.onStatusChanged({ type: "online", newValue, oldValue });
        });
    }

    /**
      * 启动更新循环
      */
    private startUpdateLoop(): void {
        if (this.updateTimer) {
            return;
        }

        // 使用Timer.RunLater创建重复定时器
        const interval = this.props.updateInterval! / 1000;
        const runUpdate = () => {
            if (this.initialized && this.props.enabled) {
                this.update(interval);
                this.updateTimer = Timer.RunLater(interval, runUpdate);
            }
        };
        this.updateTimer = Timer.RunLater(interval, runUpdate);
    }

    /**
     * 停止更新循环
     */
    private stopUpdateLoop(): void {
        if (this.updateTimer) {
            // Timer.RunLater返回的定时器会自动清理
            this.updateTimer = null;
        }
    }

    /**
     * 更新镜头
     */
    private updateCamera(): void {
        if (!this.camera || !this.isLocal) {
            return;
        }

        // 这里可以添加镜头相关的更新逻辑
        // 例如：跟随单位、边界检查等
    }

    /**
     * 设置玩家基础属性
     */
    private setupPlayerProperties(): void {
        if (!this.player) {
            logger.error("PlayerComponent: 玩家句柄为空");
            return;
        }

        // 获取玩家数据组件
        const playerData = this.owner.data.player;
        if (!playerData) {
            logger.error("PlayerComponent: 未找到玩家数据组件");
            return;
        }

        try {
            // 获取玩家真实名称
            const realName = jasscj.GetPlayerName(this.player);
            if (realName && realName !== "") {
                this.props.playerName = realName;
            }

            // 检查是否为本地玩家
            this.props.isLocal = jasscj.GetLocalPlayer() === this.player;

            // 获取玩家ID
            this.props.playerId = jasscj.GetPlayerId(this.player);

            // 设置基础属性到数据组件
            playerData.set("玩家ID", this.props.playerId);
            playerData.set("玩家名称", this.props.playerName);
            playerData.set("是否本地玩家", this.props.isLocal);
            playerData.set("是否在线", true);
            playerData.set("是否失败", false);
            playerData.set("是否胜利", false);

            // 初始化资源
            this.initializeResources();

            logger.debug(`PlayerComponent properties set for player ${this.props.playerId}`);
        } catch (error) {
            logger.error(`Failed to setup player properties: ${error}`);
            throw error;
        }
    }

    /**
     * 初始化镜头
     */
    private initializeCamera(): void {
        if (this.props.isLocal) {
            this.camera = new Camera(this.owner as any);

            // 获取玩家数据组件
            const playerData = this.owner.data.player;
            if (playerData) {
                playerData.set("镜头", this.camera);
            }
        }
    }

    /**
     * 初始化资源
     */
    private initializeResources(): void {
        if (!this.player) {
            logger.error("PlayerComponent: 无法初始化资源，玩家句柄为空");
            return;
        }

        try {
            // 获取当前资源状态
            const gold = jasscj.GetPlayerState(this.player, jasscj.PLAYER_STATE_RESOURCE_GOLD);
            const lumber = jasscj.GetPlayerState(this.player, jasscj.PLAYER_STATE_RESOURCE_LUMBER);
            const foodUsed = jasscj.GetPlayerState(this.player, jasscj.PLAYER_STATE_RESOURCE_FOOD_USED);
            const foodCap = jasscj.GetPlayerState(this.player, jasscj.PLAYER_STATE_RESOURCE_FOOD_CAP);

            // 获取玩家数据组件
            const playerData = this.owner.data.player;
            if (playerData) {
                // 设置到数据组件
                playerData.set("金币", gold);
                playerData.set("木材", lumber);
                playerData.set("人口使用", foodUsed);
                playerData.set("人口上限", foodCap);

                logger.debug(`PlayerComponent resources initialized: Gold=${gold}, Lumber=${lumber}`);
            }
        } catch (error) {
            logger.error(`Failed to initialize resources: ${error}`);
        }
    }

    /**
     * 设置事件监听器
     */
    private setupEventListeners(): void {
        // 监听资源变化
        this.owner.on(EventTypes.PLAYER_RESOURCE_CHANGED, (data) => {
            this.onResourceChanged(data);
        });

        // 监听玩家状态变化
        this.owner.on(EventTypes.PLAYER_STATUS_CHANGED, (data) => {
            this.onStatusChanged(data);
        });

        // 监听属性变化
        this.owner.on(EventTypes.DATA_PROPERTY_CHANGED, (data) => {
            this.onPropertyChanged(data);
        });
    }

    /**
     * 更新资源状态
     */
    private updateResources(): void {
        if (!this.player) {
            return;
        }

        try {
            const newGold = jasscj.GetPlayerState(this.player, jasscj.PLAYER_STATE_RESOURCE_GOLD);
            const newLumber = jasscj.GetPlayerState(this.player, jasscj.PLAYER_STATE_RESOURCE_LUMBER);
            const newFoodUsed = jasscj.GetPlayerState(this.player, jasscj.PLAYER_STATE_RESOURCE_FOOD_USED);
            const newFoodCap = jasscj.GetPlayerState(this.player, jasscj.PLAYER_STATE_RESOURCE_FOOD_CAP);

            // 获取玩家数据组件
            const playerData = this.owner.data.player;
            if (!playerData) return;

            // 获取当前值
            const currentGold = playerData.get("金币") || 0;
            const currentLumber = playerData.get("木材") || 0;
            const currentFoodUsed = playerData.get("人口使用") || 0;
            const currentFoodCap = playerData.get("人口上限") || 0;

            // 检查资源变化
            if (newGold !== currentGold) {
                playerData.set("金币", newGold);
                this.owner.emit(EventTypes.PLAYER_GOLD_CHANGED, { oldValue: currentGold, newValue: newGold });
                this.isDirty = true;
            }

            if (newLumber !== currentLumber) {
                playerData.set("木材", newLumber);
                this.owner.emit(EventTypes.PLAYER_LUMBER_CHANGED, { oldValue: currentLumber, newValue: newLumber });
                this.isDirty = true;
            }

            if (newFoodUsed !== currentFoodUsed) {
                playerData.set("人口使用", newFoodUsed);
                this.owner.emit(EventTypes.PLAYER_FOOD_USED_CHANGED, { oldValue: currentFoodUsed, newValue: newFoodUsed });
                this.isDirty = true;
            }

            if (newFoodCap !== currentFoodCap) {
                playerData.set("人口上限", newFoodCap);
                this.owner.emit(EventTypes.PLAYER_FOOD_CAP_CHANGED, { oldValue: currentFoodCap, newValue: newFoodCap });
                this.isDirty = true;
            }
        } catch (error) {
            logger.error(`Failed to update resources: ${error}`);
        }
    }

    /**
     * 更新玩家状态
     */
    private updatePlayerStatus(): void {
        if (!this.player) {
            return;
        }

        try {
            // 获取玩家数据组件
            const playerData = this.owner.data.player;
            if (!playerData) return;

            // 获取当前状态
            const currentOnlineStatus = playerData.get("是否在线") || false;
            const currentDefeatedStatus = playerData.get("是否失败") || false;

            // 检查玩家是否还在线
            const newOnlineStatus = jasscj.GetPlayerSlotState(this.player) === jasscj.PLAYER_SLOT_STATE_PLAYING;
            if (newOnlineStatus !== currentOnlineStatus) {
                playerData.set("是否在线", newOnlineStatus);
                this.owner.emit(EventTypes.PLAYER_ONLINE_STATUS_CHANGED, { isOnline: newOnlineStatus });
                this.isDirty = true;
            }

            // 检查玩家是否失败
            const newDefeatedStatus = jasscj.GetPlayerState(this.player, jasscj.PLAYER_STATE_OBSERVER) === 1;
            if (newDefeatedStatus !== currentDefeatedStatus) {
                playerData.set("是否失败", newDefeatedStatus);
                if (newDefeatedStatus) {
                    this.owner.emit(EventTypes.PLAYER_DEFEATED, { player: this.owner });
                }
                this.isDirty = true;
            }
        } catch (error) {
            logger.error(`Failed to update player status: ${error}`);
        }
    }

    /**
     * 资源变化处理
     */
    private onResourceChanged(data: any): void {
        logger.debug(`Player ${this.playerId} resource changed:`, data);
    }

    /**
     * 状态变化处理
     */
    private onStatusChanged(data: any): void {
        logger.debug(`Player ${this.playerId} status changed:`, data);
    }

    /**
     * 属性变化处理
     */
    private onPropertyChanged(data: any): void {
        // 处理特定属性变化
        switch (data.key) {
            case "玩家等级":
                this.owner.emit(EventTypes.PLAYER_LEVEL_CHANGED, {
                    oldLevel: data.oldValue,
                    newLevel: data.newValue,
                    player: this.owner
                });
                break;
            case "经验值":
                this.owner.emit(EventTypes.PLAYER_EXPERIENCE_CHANGED, {
                    oldExp: data.oldValue,
                    newExp: data.newValue,
                    player: this.owner
                });
                break;
        }
    }

    // ==================== 公共API方法 ====================

    /**
     * 获取War3玩家句柄
     */
    getPlayerHandle(): any {
        return this.player;
    }

    /**
     * 获取玩家ID
     */
    getPlayerId(): number {
        return this.props.playerId || -1;
    }

    /**
     * 获取玩家名称
     */
    getPlayerName(): string {
        return this.props.playerName || "";
    }

    /**
     * 是否为本地玩家
     */
    isLocalPlayer(): boolean {
        return this.props.isLocal || false;
    }

    /**
     * 获取镜头控制器
     */
    getCamera(): Camera | null {
        return this.camera;
    }

    /**
     * 添加金币
     */
    addGold(amount: number): void {
        if (!this.player) return;

        const playerData = this.owner.data.player;
        if (!playerData) return;

        const currentGold = playerData.get("金币") || 0;
        const newGold = currentGold + amount;

        jasscj.SetPlayerState(this.player, jasscj.PLAYER_STATE_RESOURCE_GOLD, newGold);
        playerData.set("金币", newGold);
    }

    /**
     * 添加木材
     */
    addLumber(amount: number): void {
        if (!this.player) return;

        const playerData = this.owner.data.player;
        if (!playerData) return;

        const currentLumber = playerData.get("木材") || 0;
        const newLumber = currentLumber + amount;

        jasscj.SetPlayerState(this.player, jasscj.PLAYER_STATE_RESOURCE_LUMBER, newLumber);
        playerData.set("木材", newLumber);
    }

    /**
     * 设置玩家失败
     */
    setDefeated(): void {
        if (!this.player) return;

        try {
            jasscj.CustomDefeatBJ(this.player, "失败");

            // 获取玩家数据组件
            const playerData = this.owner.data.player;
            if (playerData) {
                playerData.set("是否失败", true);
                playerData.set("是否在线", false);
            }

            this.owner.emit(EventTypes.PLAYER_DEFEATED, { player: this.owner });
            logger.debug(`Player ${this.getPlayerId()} set as defeated`);
        } catch (error) {
            logger.error(`Failed to set player defeated: ${error}`);
        }
    }

    /**
     * 设置玩家胜利
     */
    setVictorious(): void {
        if (!this.player) return;

        try {
            jasscj.CustomVictoryBJ(this.player, true, true);

            // 获取玩家数据组件
            const playerData = this.owner.data.player;
            if (playerData) {
                playerData.set("是否胜利", true);
            }

            this.owner.emit(EventTypes.PLAYER_VICTORY, { player: this.owner });
            logger.debug(`Player ${this.getPlayerId()} set as victorious`);
        } catch (error) {
            logger.error(`Failed to set player victorious: ${error}`);
        }
    }

    /**
     * 发送消息给玩家
     */
    sendMessage(message: string, duration: number = 10): void {
        if (!this.player || !this.props.isLocal) return;

        try {
            jasscj.DisplayTextToPlayer(this.player, 0, 0, message);
            logger.debug(`Message sent to player ${this.getPlayerId()}: ${message}`);
        } catch (error) {
            logger.error(`Failed to send message to player: ${error}`);
        }
    }

    /**
     * 播放声音给玩家
     */
    playSound(soundPath: string): void {
        if (!this.player || !this.props.isLocal) return;

        try {
            const sound = jasscj.CreateSound(soundPath, false, false, false, 10, 10, "");
            if (sound) {
                jasscj.StartSound(sound);
                logger.debug(`Sound played for player ${this.getPlayerId()}: ${soundPath}`);
            }
        } catch (error) {
            logger.error(`Failed to play sound: ${error}`);
        }
    }

    /**
     * 检查玩家是否拥有单位
     */
    ownsUnit(unitHandle: any): boolean {
        if (!this.player || !unitHandle) return false;

        try {
            return jasscj.GetOwningPlayer(unitHandle) === this.player;
        } catch (error) {
            logger.error(`Failed to check unit ownership: ${error}`);
            return false;
        }
    }

    /**
     * 获取玩家颜色
     */
    getPlayerColor(): number {
        if (!this.player) return 0;

        try {
            return jasscj.GetPlayerColor(this.player);
        } catch (error) {
            logger.error(`Failed to get player color: ${error}`);
            return 0;
        }
    }

    /**
     * 设置玩家联盟状态
     */
    setAlliance(otherPlayer: any, allianceType: number, flag: boolean): void {
        if (!this.player || !otherPlayer) return;

        try {
            jasscj.SetPlayerAllianceStateBJ(this.player, otherPlayer, allianceType);
            logger.debug(`Alliance set for player ${this.getPlayerId()}: type=${allianceType}, flag=${flag}`);
        } catch (error) {
            logger.error(`Failed to set alliance: ${error}`);
        }
    }

    // ======================扩展功能方法====================================================

    /**
     * 获取玩家数据
     */
    getPlayerData(): any {
        if (!this.playerDataManager) return null;

        return this.playerDataManager.getAllData();
    }

    /**
     * 设置玩家属性
     */
    setPlayerProperty(key: string, value: any): boolean {
        if (!this.playerDataManager) return false;

        try {
            this.playerDataManager.set(key as keyof Inte_PlayerSchema, value);
            return true;
        } catch (error) {
            logger.error(`Failed to set player property ${key}: ${error}`);
            return false;
        }
    }

    /**
     * 获取玩家属性
     */
    getPlayerProperty(key: string): any {
        if (!this.playerDataManager) return null;

        return this.playerDataManager.get(key as keyof Inte_PlayerSchema);
    }

    /**
     * 检查玩家是否在线
     */
    isOnline(): boolean {
        return this.getPlayerProperty("是否在线") || false;
    }

    /**
     * 检查玩家是否失败
     */
    isDefeated(): boolean {
        return this.getPlayerProperty("是否失败") || false;
    }

    /**
     * 检查玩家是否胜利
     */
    isVictorious(): boolean {
        return this.getPlayerProperty("是否胜利") || false;
    }

    /**
     * 获取玩家资源
     */
    getResources(): { gold: number; lumber: number; foodUsed: number; foodCap: number } {
        return {
            gold: this.getPlayerProperty("金币") || 0,
            lumber: this.getPlayerProperty("木材") || 0,
            foodUsed: this.getPlayerProperty("人口使用") || 0,
            foodCap: this.getPlayerProperty("人口上限") || 0
        };
    }

    /**
     * 设置玩家资源
     */
    setResources(resources: { gold?: number; lumber?: number; foodCap?: number }): void {
        if (!this.player) return;

        try {
            if (resources.gold !== undefined) {
                jasscj.SetPlayerState(this.player, jasscj.PLAYER_STATE_RESOURCE_GOLD, resources.gold);
                this.setPlayerProperty("金币", resources.gold);
            }

            if (resources.lumber !== undefined) {
                jasscj.SetPlayerState(this.player, jasscj.PLAYER_STATE_RESOURCE_LUMBER, resources.lumber);
                this.setPlayerProperty("木材", resources.lumber);
            }

            if (resources.foodCap !== undefined) {
                jasscj.SetPlayerState(this.player, jasscj.PLAYER_STATE_RESOURCE_FOOD_CAP, resources.foodCap);
                this.setPlayerProperty("人口上限", resources.foodCap);
            }
        } catch (error) {
            logger.error(`Failed to set player resources: ${error}`);
        }
    }
}
