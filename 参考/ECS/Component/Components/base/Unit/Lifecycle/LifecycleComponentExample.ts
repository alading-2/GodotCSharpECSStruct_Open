/**
 * LifecycleComponent 和 RecoveryComponent 使用示例
 * 
 * 本文件展示了如何正确使用重构后的生命周期组件和恢复组件，
 * 体现了现代游戏架构的组件化设计和单一职责原则。
 * 
 * @author 游戏架构师
 * @version 1.0.0
 */

import { LifecycleComponent, LifecycleComponentProps, DeathType } from "./LifecycleComponent";
import { RecoveryComponent, RecoveryComponentProps } from "../RecoveryComponent";
import { Entity } from "../../../../..";
import { EventTypes } from "../../../../../types/EventTypes";


/**
 * 示例1：创建普通单位
 * 
 * 普通单位具有基础的生命周期管理和恢复功能
 */
export function createNormalUnit(entity: Entity): void {
    // 配置生命周期组件
    const lifecycleProps: LifecycleComponentProps = {
        maxLifeTime: 120,           // 2分钟后自动死亡
        canRevive: false,           // 普通单位不能复活
        reviveTime: 0,
        invulnerabilityDuration: 0
    };

    // 配置恢复组件
    const recoveryProps: RecoveryComponentProps = {
        recoveryInterval: 1.0,      // 每1秒执行一次恢复
        autoStart: true             // 自动开始恢复
    };

    // 添加组件到实体
    const lifecycle = entity.addComponent(LifecycleComponent, lifecycleProps);
    const recovery = entity.addComponent(RecoveryComponent, recoveryProps);

    // 设置恢复速率（通过属性数据）
    entity.data.attr.set("基础生命恢复", 2);      // 每秒恢复2点生命值
    entity.data.attr.set("基础魔法恢复", 1);      // 每秒恢复1点魔法值

    console.log("普通单位创建完成，具有基础生命周期和恢复功能");
}

/**
 * 示例2：创建英雄单位
 * 
 * 英雄单位具有复活能力和更强的恢复功能
 */
export function createHeroUnit(entity: Entity): void {
    // 英雄生命周期配置
    const heroLifecycleProps: LifecycleComponentProps = {
        maxLifeTime: -1,            // 永不自然死亡
        canRevive: true,            // 英雄可以复活
        reviveTime: 30,             // 复活需要30秒
        invulnerabilityDuration: 5  // 复活后5秒无敌
    };

    // 英雄恢复配置
    const heroRecoveryProps: RecoveryComponentProps = {
        recoveryInterval: 0.5,      // 更频繁的恢复检查
        autoStart: true
    };

    const lifecycle = entity.addComponent(LifecycleComponent, heroLifecycleProps);
    const recovery = entity.addComponent(RecoveryComponent, heroRecoveryProps);

    // 设置英雄恢复速率（通过属性数据）
    entity.data.attr.set("基础生命恢复", 10);     // 英雄恢复速度更快
    entity.data.attr.set("基础魔法恢复", 5);      // 魔法恢复也更快
    entity.data.attr.set("百分比生命恢复", 1);    // 每秒恢复1%最大生命值
    entity.data.attr.set("百分比魔法恢复", 2);    // 每秒恢复2%最大魔法值

    // 监听英雄死亡事件
    entity.on(EventTypes.UNIT_DEATH, (data) => {
        console.log(`英雄死亡，将在${heroLifecycleProps.reviveTime}秒后复活`);
    });

    // 监听英雄复活事件
    entity.on(EventTypes.UNIT_REVIVED, (data) => {
        console.log("英雄复活完成，获得临时无敌状态");
    });

    console.log("英雄单位创建完成，具有复活和强化恢复功能");
}

/**
 * 示例3：创建召唤单位
 * 
 * 召唤单位有时间限制，不能复活，但有快速恢复
 */
export function createSummonUnit(entity: Entity): void {
    // 召唤单位生命周期配置
    const summonLifecycleProps: LifecycleComponentProps = {
        maxLifeTime: 60,            // 1分钟后自动消失
        canRevive: false,           // 召唤单位不能复活
        reviveTime: 0,
        invulnerabilityDuration: 2  // 召唤后短暂无敌
    };

    // 召唤单位恢复配置
    const summonRecoveryProps: RecoveryComponentProps = {
        recoveryInterval: 0.5,      // 频繁恢复
        autoStart: true
    };

    const lifecycle = entity.addComponent(LifecycleComponent, summonLifecycleProps);
    const recovery = entity.addComponent(RecoveryComponent, summonRecoveryProps);

    // 设置召唤单位恢复速率（通过属性数据）
    entity.data.attr.set("基础生命恢复", 5);      // 快速恢复
    entity.data.attr.set("基础魔法恢复", 3);      // 快速魔法恢复

    // 监听召唤单位即将消失
    const remainingTime = lifecycle.getRemainingLifeTime();
    if (remainingTime > 0 && remainingTime <= 10) {
        console.log(`召唤单位将在${remainingTime}秒后消失`);
    }

    console.log("召唤单位创建完成，具有时间限制和快速恢复");
}

/**
 * 示例4：动态恢复管理
 * 
 * 展示如何根据游戏状态动态调整恢复速率
 */
export function setupDynamicRecovery(entity: Entity): void {
    const recovery = entity.getComponent(RecoveryComponent);
    if (!recovery) {
        console.error("实体没有恢复组件");
        return;
    }

    // 根据单位等级调整恢复速率
    const level = entity.data.attr?.get("等级") || 1;
    entity.data.attr.set("基础生命恢复", level * 2);    // 每级增加2点生命恢复
    entity.data.attr.set("基础魔法恢复", level * 1.5);  // 每级增加1.5点魔法恢复

    // 高级单位增加百分比恢复
    if (level >= 10) {
        entity.data.attr.set("百分比生命恢复", 1);  // 每秒恢复1%最大生命值
        entity.data.attr.set("百分比魔法恢复", 1);  // 每秒恢复1%最大魔法值
    }

    // 战斗状态管理
    entity.on(EventTypes.UNIT_ENTER_COMBAT, () => {
        entity.data.attr.set("基础生命恢复", 1);    // 战斗中降低恢复速率
        entity.data.attr.set("基础魔法恢复", 0.5);  // 战斗中降低魔法恢复
        entity.data.attr.set("百分比生命恢复", 0);   // 战斗中取消百分比恢复
        entity.data.attr.set("百分比魔法恢复", 0);   // 战斗中取消百分比恢复
        console.log("进入战斗，恢复速率降低");
    });

    entity.on(EventTypes.UNIT_LEAVE_COMBAT, () => {
        const level = entity.data.attr?.get("等级") || 1;
        entity.data.attr.set("基础生命恢复", level * 2);    // 脱战后恢复正常速率
        entity.data.attr.set("基础魔法恢复", level * 1.5);  // 脱战后恢复正常速率

        // 高级单位恢复百分比恢复
        if (level >= 10) {
            entity.data.attr.set("百分比生命恢复", 1);
            entity.data.attr.set("百分比魔法恢复", 1);
        }
        console.log("脱离战斗，恢复速率恢复正常");
    });

    // 根据生命值百分比调整恢复
    const checkHealthPercent = () => {
        const currentHp = entity.data.unit?.get("当前生命值") || 0;
        const maxHp = entity.data.attr?.get("最终生命值") || 100;
        const hpPercent = currentHp / maxHp;

        if (hpPercent < 0.3) {
            entity.data.attr.set("基础生命恢复", 20);     // 低血量时加速恢复
            entity.data.attr.set("百分比生命恢复", 5);    // 低血量时额外百分比恢复
            console.log("生命值过低，启动紧急恢复模式");
        } else if (hpPercent > 0.8) {
            const level = entity.data.attr?.get("等级") || 1;
            entity.data.attr.set("基础生命恢复", level * 2);    // 恢复正常速率
            entity.data.attr.set("百分比生命恢复", level >= 10 ? 1 : 0);  // 高级单位保持百分比恢复
        }
    };

    // 定期检查生命值百分比
    setInterval(checkHealthPercent, 5000);  // 每5秒检查一次
}

/**
 * 示例5：手动恢复控制
 * 
 * 展示如何手动控制恢复过程和使用治疗技能
 */
export function manualRecoveryControl(entity: Entity): void {
    const recovery = entity.getComponent(RecoveryComponent);
    const lifecycle = entity.getComponent(LifecycleComponent);

    if (!recovery || !lifecycle) {
        console.error("实体缺少必要的组件");
        return;
    }

    // 暂停自动恢复
    recovery.pauseRecovery();
    console.log("暂停自动恢复");

    // 使用治疗技能
    const healingSkill = () => {
        if (lifecycle.isAlive()) {
            recovery.heal(50);  // 立即恢复50点生命值
            console.log("使用治疗技能，恢复50点生命值");
        }
    };

    // 使用魔法恢复技能
    const manaRestoreSkill = () => {
        if (lifecycle.isAlive()) {
            recovery.restoreMana(30);  // 立即恢复30点魔法值
            console.log("使用魔法恢复技能，恢复30点魔法值");
        }
    };

    // 模拟技能使用
    setTimeout(healingSkill, 2000);     // 2秒后使用治疗
    setTimeout(manaRestoreSkill, 4000); // 4秒后使用魔法恢复

    // 6秒后恢复自动恢复
    setTimeout(() => {
        recovery.resumeRecovery();
        console.log("恢复自动恢复");
    }, 6000);
}

/**
 * 示例6：组件生命周期管理
 * 
 * 展示如何正确管理组件的生命周期
 */
export function componentLifecycleManagement(entity: Entity): void {
    // 创建组件
    const lifecycle = entity.addComponent(LifecycleComponent, {
        maxLifeTime: 30,
        canRevive: false
    });

    const recovery = entity.addComponent(RecoveryComponent, {
        recoveryInterval: 1.0,
        autoStart: true
    });
    // 通过属性数据设置恢复速率（新方式）
    entity.data.attr.set("基础生命恢复", 5);
    entity.data.attr.set("基础魔法恢复", 3);

    // 监听组件事件
    entity.on(EventTypes.UNIT_DEATH, () => {
        console.log("单位死亡，停止恢复");
        recovery.stopRecovery();
    });

    // 在适当的时候移除组件
    setTimeout(() => {
        if (lifecycle.isDead()) {
            entity.removeComponent(RecoveryComponent);
            entity.removeComponent(LifecycleComponent);
            console.log("单位已死亡，移除相关组件");
        }
    }, 35000);  // 35秒后检查并清理
}

/**
 * 示例7：错误处理和边界情况
 * 
 * 展示如何处理各种边界情况和错误
 */
export function errorHandlingExample(entity: Entity): void {
    try {
        // 检查实体是否有必要的数据管理器
        if (!entity.data.unit || !entity.data.attr) {
            throw new Error("实体缺少必要的数据管理器");
        }

        // 创建组件
        const lifecycle = entity.addComponent(LifecycleComponent, {
            maxLifeTime: 60,
            canRevive: true,
            reviveTime: 10
        });

        const recovery = entity.addComponent(RecoveryComponent, {
            recoveryInterval: 1.0,
            autoStart: true
        });
        entity.data.attr.set("基础生命恢复", 5);
        entity.data.attr.set("基础魔法恢复", 3);

        // 安全的组件操作
        const safeHeal = (amount: number) => {
            if (lifecycle.isAlive() && amount > 0) {
                recovery.heal(amount);
            } else {
                console.warn("无法治疗：单位已死亡或治疗量无效");
            }
        };

        const safeKill = (deathType: DeathType = DeathType.NORMAL) => {
            if (lifecycle.isAlive()) {
                lifecycle.kill(deathType);
            } else {
                console.warn("无法杀死：单位已经死亡");
            }
        };

        const safeRevive = () => {
            if (lifecycle.isDead() && lifecycle.canRevive) {
                lifecycle.revive();
            } else {
                console.warn("无法复活：单位未死亡或不能复活");
            }
        };

        // 使用安全的操作方法
        safeHeal(25);
        setTimeout(() => safeKill(DeathType.NORMAL), 5000);
        setTimeout(() => safeRevive(), 10000);

    } catch (error) {
        console.error("组件创建失败:", error);
    }
}

// 导出所有示例函数
export {
    createNormalUnit,
    createHeroUnit,
    createSummonUnit,
    setupDynamicRecovery,
    manualRecoveryControl,
    componentLifecycleManagement,
    errorHandlingExample
};