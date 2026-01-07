/** @noSelfInFile **/

import { Component } from "../../../Component";
import { Entity } from "../../../../Entity/Entity";
import { Logger } from "../../../../../base/object/工具/logger";
import { Position } from "../../../../../base/math/Position";
import { Vector } from "../../../../../base/math/Vector";
import { MyMath } from "../../../../../base/math/MyMath";
import { UnitComponent } from "../Unit/UnitComponent";
import { PlayerComponent } from "../../PlayerComponent";
import { EventTypes } from "../../../../types/EventTypes";

const logger = Logger.createLogger("EffectComponent");

/**
 * 特效组件属性接口
 */
interface EffectComponentProps {
    /** 特效模型路径 */
    path: string;
    /** 初始位置 */
    position?: Position;
    /** 附加目标单位 */
    unitComp?: UnitComponent;
    /** 附加点名称 */
    attach?: string;
    /** 缩放大小 */
    size?: number;
    /** 持续时间，-1表示永久 */
    life?: number;
    /** 玩家（用于特效显示控制） */
    playerComp?: PlayerComponent;
    /** 播放速度 */
    speed?: number;
}

/**
 * 特效组件 - 管理War3特效的简化ECS实现
 * 
 * 核心职责：
 * 1. 特效生命周期管理（创建、销毁）
 * 2. 特效属性控制（位置、旋转、缩放、颜色等）
 * 3. 特效附加和绑定管理
 * 4. 动画播放控制
 * 
 * 设计原则：
 * - 简化优先：直接操作War3句柄，减少数据层
 * - 功能完整：保留Effect.ts的核心功能
 * - 性能优化：支持特效显示开关
 */
export class EffectComponent extends Component<EffectComponentProps> {
    // 组件类型名称
    protected static readonly TYPE: string = "EffectComponent";

    // War3 Effecthandle
    private effectHandle: any = null;

    // 特效属性
    /** 特效模型路径 */
    private path: string;

    /** 特效位置 */
    private _position: Position = new Position(0, 0, 0);

    /** 特效缩放大小 */
    private _size: number = 1;

    /** 特效播放速度 */
    private _speed: number = 1;

    /** Z轴旋转角度 */
    private _rotateZ: number = 0;

    /** X轴旋转角度 */
    private _rotateX: number = 0;

    /** Y轴旋转角度 */
    private _rotateY: number = 0;

    /** 特效透明度 */
    private _alpha: number = 255;

    /** 是否显示特效 */
    private _isVisible: boolean = true;

    /** 附加的单位组件引用 */
    private attachedUnit: UnitComponent | null = null;

    /** 附加点名称 */
    private attachPoint: string | null = null;

    // 生命周期定时器
    private lifetimeTimerId: string;

    // 生命周期（内部管理，不依赖 DataManager 全量字段）
    private duration: number = -1;


    /**
     * 获取组件类型
     */
    static getType(): string {
        return EffectComponent.TYPE;
    }

    /**
     * 构造函数
     */
    constructor(owner: Entity, props: EffectComponentProps) {
        super(owner, props);
    }

    /**
     * 初始化组件
     */
    initialize(): void {
        // 特效路径
        this.path = this.props.path;

        // 设置初始位置
        if (this.props.position) {
            this._position = this.props.position;
        }
        // 附着单位
        if (this.props.unitComp) {
            this.attachedUnit = this.props.unitComp;
        }
        // 附加点
        if (this.props.attach) {
            this.attachPoint = this.props.attach;
        }

        // 设置初始属性
        if (this.props.size !== undefined) {
            this._size = this.props.size;
        }
        if (this.props.speed !== undefined) {
            this._speed = this.props.speed;
        }

        // 检查是否需要显示特效（性能优化）
        this._isVisible = true;
        if (this.props.playerComp) {
            // 通过玩家实体对象访问数据
            // PlayerComponent 暴露 getOwnerEntity 或提供 getPlayerData 的情况下更优，这里通过组件所在实体访问
            const playerData = this.props.playerComp.getOwner().data.player;
            if (playerData && playerData.get("关闭特效")) {
                this._isVisible = false;
            }
        }

        // 创建特效
        this.createEffect();

        // 设置生命周期计时器
        this.life = this.props.life;

        // 设置事件监听
        this.setupEventListeners();

        logger.debug(`特效组件已初始化，模型路径: ${this.path}`);
    }

    /**
     * 销毁组件
     */
    destroy(): void {
        if (this.isDestroyed) return;

        // 移除生命周期定时器
        if (this.lifetimeTimerId) {
            this.owner.component.timer.removeTimer(this.lifetimeTimerId);
            this.lifetimeTimerId = null as any;
        }

        // 销毁War3特效
        if (this.effectHandle) {
            jassdbg.handle_unref(this.effectHandle);
            jasscj.DestroyEffect(this.effectHandle);
            this.effectHandle = null;
        }

        // 清理引用
        this.attachedUnit = null;

        // 发送销毁事件（使用渲染通用事件）
        this.owner.emit(EventTypes.EFFECT_REMOVED, { effectId: this.owner.getId() } as any);

        super.destroy();

        logger.debug(`特效组件已销毁，实体ID: ${this.owner.getId()}`);
    }


    /**
     * 创建War3特效
     */
    private createEffect(): void {
        if (!this._isVisible) {
            logger.debug("特效可见性已禁用，跳过War3特效创建");
            return;
        }

        if (this.attachedUnit && this.attachPoint) {
            // 创建附加到单位的特效
            const unitHandle = this.attachedUnit.getUnitHandle();
            this.effectHandle = jasscj.AddSpecialEffectTarget(this.path, unitHandle, this.attachPoint);

        } else {
            // 创建位置特效
            const position = this.position;
            this.effectHandle = jasscj.AddSpecialEffect(this.path, position.X, position.Y);

            // 设置Z轴位置
            if (position.Z !== 0) {
                jassjapi.EXSetEffectZ(this.effectHandle, position.Z);
            }
        }

        if (this.effectHandle) {
            jassdbg.handle_ref(this.effectHandle);
            logger.debug(`War3特效已创建: ${this.path}`);
        } else {
            logger.error(`创建War3特效失败: ${this.path}`);
        }
    }


    /**
     * 设置事件监听
     */
    private setupEventListeners(): void {

    }


    // ========== 生命周期属性 ==========

    /**
     * 设置特效生命周期（参考Effect.ts的life setter）
     */
    set life(duration: number) {
        const timerComp = this.owner.component.timer;
        if (duration > 0) {
            this.duration = duration;

            // 清理现有定时器
            if (this.lifetimeTimerId) {
                timerComp.removeTimer(this.lifetimeTimerId);
            }

            // 设置新的定时器
            this.lifetimeTimerId = timerComp.RunLater(duration, () => {
                this.owner.destroy();
            });
            logger.debug(`特效生命周期已设置为 ${duration} 秒`);
        }
    }

    /**
     * 获取特效总生命周期
     */
    get life(): number {
        return this.duration ?? -1;
    }

    /**
     * 获取特效剩余生命周期
     */
    get remainingTime(): number {
        return this.owner.component.timer.getTimerRemainingTime(this.lifetimeTimerId);
    }

    // ========== 属性访问器 ==========
    //动画速度
    set speed(n: number) {
        if (this._isVisible) {
            jassjapi.EXSetEffectSpeed(this.effectHandle, n);
        }
        this._speed = n;
    }
    get speed() {
        return this._speed;
    }
    //大小
    set size(s: number) {
        if (this._isVisible) {
            jassjapi.EXSetEffectSize(this.effectHandle, s);
        }
        this._size = s;
    }
    get size() {
        return this._size;
    }
    //位置
    set position(pt: Position) {
        if (this._isVisible) {
            jassjapi.DzSetEffectPos(this.effectHandle, pt.X, pt.Y, 0);
            jassjapi.EXSetEffectZ(this.effectHandle, pt.Z + pt.terrianZ);
        }
        this._position.X = pt.X;
        this._position.Y = pt.Y;
        this._position.Z = pt.Z;
    }
    get position(): Position {
        return this._position;
    }

    //绑定特效，KKAPI
    DzBindEffect(unitComp: UnitComponent, attachpoint: string) {
        jassjapi.DzBindEffect(unitComp.getUnitHandle(), attachpoint, this.effectHandle);
    }
    //解除绑定
    DzUnbindEffect() {
        jassjapi.DzUnbindEffect(this.effectHandle);
    }

    //透明度
    set alpha(n: number) {
        this._alpha = n;
        if (this._isVisible) {
            jassjapi.DzSetEffectVertexAlpha(this.effectHandle, n);
        }
    }
    get alpha() {
        return this._alpha;
    }

    set model(path: string) {
        if (this._isVisible) {
            jassjapi.DzSetEffectModel(this.effectHandle, path);
        }
    }

    //颜色
    /**
     * 设置特效颜色
     * @param red 红
     * @param green 绿
     * @param blue 蓝
     * @param alpha 透明度
     */
    SetColor(red: number, green: number, blue: number, alpha?: number) {
        jassjapi.DzSetEffectVertexColor(
            this.effectHandle,
            jassjapi.DzGetColor(red, green, blue, alpha ?? 255)
        );
    }

    //设置特效播放动画KKAPI
    Action(act: string, add?: string) {
        if (add) {
            jassjapi.DzPlayEffectAnimation(this.effectHandle, act, add);
        } else {
            jassjapi.DzPlayEffectAnimation(this.effectHandle, act, "");
        }
    }

    //缩放
    set scaleX(x: number) {
        if (this._isVisible) {
            jassjapi.EXEffectMatScale(this.effectHandle, x, 1, 1);
        }
    }
    set scaleY(y: number) {
        if (this._isVisible) {
            jassjapi.EXEffectMatScale(this.effectHandle, 1, y, 1);
        }
    }
    set scaleZ(z: number) {
        if (this._isVisible) {
            jassjapi.EXEffectMatScale(this.effectHandle, 1, 1, z);
        }
    }

    /**
     * 设置特效朝向
     */
    set vector(v: Vector) {
        let angle_z = v.angle;
        //特效z轴面向90度方向，y轴旋转90度是向右的，所以方向反了，加-号
        let angle_zx = (-MyMath.atan(v.Z, MyMath.Pow(v.X * v.X + v.Y * v.Y, 0.5)) + 360) % 360;
        this.rotateY = angle_zx;
        this.rotateZ = angle_z;
    }

    //旋转角度
    get rotateZ() {
        return this._rotateZ;
    }
    set rotateZ(n: number) {
        let a = n;
        n = n - this._rotateZ;
        if (this._isVisible) {
            jassjapi.EXEffectMatRotateZ(this.effectHandle, n);
        }
        this._rotateZ = a;
    }
    //x轴向右，y轴向前，z轴向上
    //特效z轴面向90度方向，z轴旋转90度是向后的，即y轴负方向
    get rotateX() {
        return this._rotateX;
    }
    set rotateX(n: number) {
        let a = n;
        n = n - this._rotateX;
        if (this._isVisible) {
            jassjapi.EXEffectMatRotateX(this.effectHandle, n);
        }
        this._rotateX = a;
    }

    //x轴向右，y轴向前，z轴向上
    //特效z轴面向90度方向，y轴旋转90度是向右的，即x轴正方向
    get rotateY() {
        return this._rotateY;
    }
    set rotateY(n: number) {
        let a = n;
        n = n - this._rotateY;
        if (this._isVisible) {
            jassjapi.EXEffectMatRotateY(this.effectHandle, n);
        }
        this._rotateY = a;
    }

    // ====================== 公共API ======================
    /**
     * 获取特效句柄
     */
    getEffectHandle(): any {
        return this.effectHandle;
    }

}