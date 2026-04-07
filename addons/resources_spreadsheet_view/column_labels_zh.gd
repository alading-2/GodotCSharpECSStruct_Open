## 列头中文映射表
## key: C# 属性名（原始字段名，小写匹配）
## value: 显示的中文标签
const LABELS: Dictionary = {
	# 分组名称
	"基础信息": "基础信息",
	"技能类型": "技能类型",
	"消耗与冷却": "消耗与冷却",
	"充能系统": "充能系统",
	"目标选择": "目标选择",
	"链式效果": "链式效果",
	"伤害效果": "伤害效果",
	"视觉": "视觉",
	"生命属性": "生命属性",
	"攻击属性": "攻击属性",
	"防御属性": "防御属性",
	"移动属性": "移动属性",
	"敌人专有": "敌人专有",
	"ai 配置": "AI 配置",
	"spawn rule": "生成规则",
	"玩家专有": "玩家专有",
	
	# 通用
	"resource_path": "资源路径",

	# UnitConfig - 基础信息
	"name": "名称",
	"team": "队伍",
	"deathtype": "死亡类型",

	# UnitConfig - 视觉
	"visualscenepath": "视觉场景",
	"healthbarheight": "血条高度",

	# UnitConfig - 生命属性
	"basehp": "基础生命值",
	"basehpregen": "生命回复/秒",
	"lifesteal": "吸血比例(%)",

	# UnitConfig - 攻击属性
	"baseattack": "基础攻击力",
	"baseattackspeed": "攻击速度",
	"attackrange": "攻击距离",
	"critrate": "暴击率(%)",
	"critdamage": "暴击伤害(%)",
	"penetration": "护甲穿透",

	# UnitConfig - 防御属性
	"basedefense": "基础防御力",
	"damagereduction": "伤害减免(%)",

	# UnitConfig - 移动属性
	"movespeed": "移动速度",
	"dodgechance": "闪避率(%)",

	# EnemyConfig - 敌人专有
	"expreward": "经验奖励",
	"detectionrange": "检测范围",

	# EnemyConfig - Spawn Rule
	"isenablespawnrule": "启用生成规则",
	"spawnstrategy": "生成策略",
	"spawnminwave": "起始波次",
	"spawnmaxwave": "截止波次",
	"spawninterval": "生成间隔(秒)",
	"spawnmaxcountperwave": "单波最大数量",
	"singlespawncount": "单次生成数量",
	"singlespawnvariance": "数量波动值",
	"spawnstartdelay": "首次生成延迟",
	"spawnweight": "生成权重",

	# PlayerConfig - 玩家专有
	"basemana": "基础法力值",
	"currentmana": "当前法力值",
	"basemanaregen": "法力回复/秒",
	"pickuprange": "拾取范围",
	"baseskilldamage": "基础技能伤害",
	"cooldownreduction": "冷却缩减(%)",

	# TargetingIndicatorConfig
	"isshowHealthbar": "显示血条",
	"isinvulnerable": "是否无敌",
	"isshowhealthbar": "显示血条",

	# AbilityConfig - 基础信息
	"description": "描述",
	"abilityicon": "技能图标",
	"abilitylevel": "技能等级",
	"abilitymaxlevel": "最大等级",

	# AbilityConfig - 技能类型
	"entitytype": "实体类型",
	"abilitytype": "技能类型",
	"abilitytriggermode": "触发模式",

	# AbilityConfig - 消耗与冷却
	"abilitycosttype": "消耗类型",
	"abilitycostamount": "消耗数值",
	"abilitycooldown": "冷却时间(秒)",

	# AbilityConfig - 充能系统
	"isabilityusescharges": "启用充能",
	"abilitymaxcharges": "最大充能层",
	"abilityChargetime": "充能时间(秒)",
	"abilitychargetime": "充能时间(秒)",

	# AbilityConfig - 目标选择
	"abilitytargetselection": "目标选择",
	"abilitytargetgeometry": "目标形状",
	"abilitytargetteamfilter": "目标阵营",
	"TargetSorting": "目标排序",
	"abilitycastrange": "施法距离",
	"abilityeffectradius": "效果半径",
	"abilityeffectlength": "效果长度",
	"abilityeffectwidth": "效果宽度",
	"abilityangle": "效果角度",

	# AbilityConfig - 链式效果
	"abilitychaincount": "链式弹跳次数",
	"abilitychainrange": "链式弹跳范围",
	"abilitymaxtargets": "最大目标数量",

	# AbilityConfig - 伤害效果
	"BaseSkillDamage": "技能伤害",
}


static func get_label(property_name: String) -> String:
	var key := property_name.to_lower()
	if LABELS.has(key):
		return LABELS[key]
	return property_name.capitalize()
