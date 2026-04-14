## 列头中文映射表
## key: C# 属性名（原始字段名，小写匹配）
## value: 显示的中文标签
## 自动从 Data/Data/ 下 C# 配置类的 <summary> 注释同步
const LABELS: Dictionary = {
	# ========== 分组名称 ==========
	"基础信息": "基础信息",
	"视觉": "视觉",
	"生命属性": "生命属性",
	"攻击属性": "攻击属性",
	"防御属性": "防御属性",
	"移动属性": "移动属性",
	"玩家专有": "玩家专有",
	"敌人专有": "敌人专有",
	"ai 配置": "AI 配置",
	"spawn rule": "生成规则",
	"技能类型": "技能类型",
	"消耗与冷却": "消耗与冷却",
	"充能系统": "充能系统",
	"目标选择": "目标选择",
	"链式效果": "链式效果",
	"视觉与表现": "视觉与表现",
	"伤害效果": "伤害效果",
	"属性修改器": "属性修改器",

	# ========== 通用 ==========
	"resource_path": "资源路径",

	# ===== UnitConfig =====
	# 基础信息
	"name": "单位名称",
	"team": "所属队伍",
	"deathtype": "死亡类型",
	# 视觉
	"visualscenepath": "视觉场景路径",
	"healthbarheight": "血条显示高度",
	# 生命属性
	"basehp": "基础生命值",
	"basehpregen": "生命回复/秒",
	"lifesteal": "吸血比例(%)",
	# 攻击属性
	"baseattack": "基础攻击力",
	"baseattackspeed": "攻击速度",
	"attackrange": "攻击距离",
	"critrate": "暴击率(%)",
	"critdamage": "暴击伤害(%)",
	"penetration": "护甲穿透",
	# 防御属性
	"basedefense": "基础防御力",
	"dmgreduction": "伤害减免(%)",
	"damagereduction": "伤害减免(%)",
	# 移动属性
	"movespeed": "移动速度",
	"dodgechance": "闪避率(%)",

	# ===== PlayerConfig =====
	"basemana": "基础法力值",
	"basemanaregen": "法力回复/秒",
	"pickuprange": "拾取范围",
	"baseskilldamage": "基础技能伤害",
	"cooldownreduction": "冷却缩减(%)",

	# ===== EnemyConfig =====
	"expreward": "击杀经验奖励",
	"detectionrange": "AI检测范围",
	"isenablespawnrule": "启用生成规则",
	"spawnstrategy": "生成位置策略",
	"spawnminwave": "起始波次",
	"spawnmaxwave": "截止波次",
	"spawninterval": "生成间隔(秒)",
	"spawnmaxcountperwave": "单波最大数量",
	"singlespawncount": "单次生成数量",
	"singlespawnvariance": "数量波动值",
	"spawnstartdelay": "首次生成延迟",
	"spawnweight": "生成权重",

	# ===== TargetingIndicatorConfig =====
	"isshowhealthbar": "显示血条",
	"isinvulnerable": "是否无敌",

	# ===== AbilityConfig =====
	"featuregroupid": "技能分组ID",
	"description": "技能描述",
	"abilityicon": "技能图标",
	"abilitylevel": "当前级别",
	"abilitymaxlevel": "最大级别",
	"entitytype": "实体类型",
	"abilitytype": "技能类型",
	"abilitytriggermode": "触发模式",
	"abilitycosttype": "消耗类型",
	"abilitycostamount": "消耗数值",
	"abilitycooldown": "冷却时间(秒)",
	"isabilityusescharges": "启用充能",
	"abilitymaxcharges": "最大充能层",
	"abilitychargetime": "充能时间(秒)",
	"abilitytargetselection": "目标选择",
	"abilitytargetgeometry": "目标形状",
	"abilitytargetteamfilter": "目标阵营",
	"targetsorting": "目标排序",
	"abilitycastrange": "施法距离",
	"abilityeffectradius": "效果半径",
	"abilityeffectlength": "效果长度",
	"abilityeffectwidth": "效果宽度",
	"abilitymaxtargets": "最大目标数量",
	"effectscene": "表现特效",
	"projectilescene": "投射物场景",
	"abilitydamage": "技能伤害",

	# ===== ChainAbilityConfig =====
	"chaincount": "链式弹跳次数",
	"chainrange": "链式弹跳范围",
	"chaindelay": "弹跳延时(秒)",
	"chaindmgdecay": "伤害衰减(%)",
	"lineeffectscene": "连线特效",

	# ===== FeatureDefinition =====
	"category": "Feature分类",
	"enabled": "是否启用",
	"modifiers": "属性修改器列表",

	# ===== FeatureModifierEntry =====
	"datakeyname": "目标DataKey",
	"modifiertype": "修改器类型",
	"value": "修改值",
	"priority": "优先级",
}


static func get_label(property_name: String) -> String:
	var key := property_name.to_lower()
	if LABELS.has(key):
		return LABELS[key]
	return property_name.capitalize()
