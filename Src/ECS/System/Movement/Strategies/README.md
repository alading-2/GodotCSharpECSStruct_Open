# 绉诲姩绛栫暐绯荤粺鏂囨。

## 姒傝堪

绉诲姩绛栫暐绯荤粺鏄?ECS 鏋舵瀯涓殑鏍稿績缁勪欢锛岃礋璐ｅ疄鐜板悇绉嶅鏉傜殑瀹炰綋杩愬姩妯″紡銆傛瘡涓瓥鐣ラ兘鏄?`IMovementStrategy` 鎺ュ彛鐨勫叿浣撳疄鐜帮紝閫氳繃 `MovementStrategyRegistry` 缁熶竴绠＄悊銆?
## 鏋舵瀯璁捐

### 鏍稿績鎺ュ彛
```csharp
public interface IMovementStrategy
{
    void OnEnter(IEntity entity, Data data, MovementParams @params);
    MovementUpdateResult Update(IEntity entity, Data data, float delta, MovementParams @params);
}
```

### 鍙傛暟绯荤粺
- **MovementParams**: 绉诲姩鍙傛暟瀹瑰櫒锛屽寘鍚墍鏈夌瓥鐣ラ渶瑕佺殑閰嶇疆
- **杩愯鏃剁粺璁?*: ElapsedTime, TraveledDistance锛堢敱璋冨害鍣ㄧ淮鎶わ級
- **绛栫暐鐘舵€?*: 绉佹湁瀛楁瀛樺偍锛堝 _baseDirection, _currentAngle锛?
## 绛栫暐鍒嗙被

### 馃寠 娉㈠姩绫?(Wave/)
#### SineWaveStrategy - 姝ｅ鸡娉㈠墠杩?**鐢ㄩ€?*: 铔囧舰瀛愬脊銆佹尝娴兘閲忔潫銆佽閬块鍒ょ殑鎽嗗姩椋炶鐗?
**鏁板鍘熺悊**:
```
浣嶇疆(t) = 鍩哄噯鏂瑰悜 脳 鍓嶈繘閫熷害 脳 t + 鍨傜洿鏂瑰悜 脳 鎸箙 脳 sin(2蟺 脳 棰戠巼 脳 t + 鐩镐綅)
```

**鍏抽敭鍙傛暟**:
- `ActionSpeed`: 鍓嶈繘閫熷害锛堝儚绱?绉掞級
- `WaveAmplitude`: 妯悜鎸箙鍩虹鍊硷紙鍍忕礌锛?- `WaveAmplitudeScalarDriver`: 鎸箙鍔ㄦ€侀┍鍔紙鍙€夛級
- `WaveFrequency`: 娉㈠姩棰戠巼鍩虹鍊硷紙鍛ㄦ湡/绉掞級
- `WaveFrequencyScalarDriver`: 棰戠巼鍔ㄦ€侀┍鍔紙鍙€夛級
- `WavePhase`: 鍒濆鐩镐綅锛堝害锛?
**鎶€鏈壒鐐?*:
- OnEnter 閿佸畾鍩哄噯鏂瑰悜锛岄槻姝㈡尝鍔ㄥ垎閲忔薄鏌?- 澧為噺璁＄畻娉曪細`sin(new) - sin(old)` 閬垮厤绱Н璇樊
- 鍨傜洿鍚戦噺璁＄畻锛歚(-Y, X)` 瀹炵幇椤烘椂閽?0搴︽棆杞?
---

### 馃搱 鏇茬嚎绫?(Curve/)
#### BezierCurveStrategy - 璐濆灏旀洸绾跨Щ鍔?**鐢ㄩ€?*: 澶嶆潅寮归亾璺緞銆佸钩婊戣建杩瑰姩鐢汇€佺簿纭矾寰勬帶鍒?
**鏁板鍘熺悊**:
- **De Casteljau 绠楁硶**: 鏁板€肩ǔ瀹氱殑璐濆灏旀洸绾挎眰鍊?- **寮ч暱鍙傛暟鍖?*: 瀹炵幇鐪熸鐨勫寑閫熺Щ鍔?- **鍙傛暟鍩?*: t 鈭?[0, 1]

**鍏抽敭鍙傛暟**:
- `MaxDuration`: 绉诲姩鎬绘椂闀匡紙绉掞紝**蹇呴』 > 0**锛屾绛栫暐涓嶆敮鎸?-1锛?- `BezierPoints`: 鎺у埗鐐规暟缁勶紙鍚捣鐐瑰拰缁堢偣锛屾帹鑽愶級锛涜嫢鏈彁渚涘垯浠?`TargetPoint` 闄嶇骇鐩寸嚎
- `BezierUniformSpeed`: 鏄惁鍚敤寮ч暱鍙傛暟鍖栧寑閫熸ā寮?
**鎶€鏈壒鐐?*:
- 鎺у埗鐐瑰厠闅嗭細閬垮厤姹℃煋鍘熷鏁版嵁
- 璧风偣淇锛歄nEnter 鏃跺皢绗?涓帶鍒剁偣璁句负瀹炰綋褰撳墠浣嶇疆
- LUT 鑷€傚簲鍒嗘锛歚Math.Max(16, 闃舵暟 * 8)`锛堜笁闃?24锛屼簲闃?40锛夛紝鏇夸唬鍥哄畾 64 鐨勮繃搴﹀紑閿€

---

### 馃幆 鎶涘皠鐗╃被 (Projectile/)
#### BoomerangStrategy - 回旋镖移动
**用途**: 回旋镖武器、往返巡逻、回收型投射物
**阶段逻辑**:
1. **去程**: 从发射点到 `TargetPoint` 走上半椭圆
2. **暂停**: 命中终点后可选停顿 `BoomerangPauseTime`
3. **返程**: 从折返点到 `TargetNode` 当前坐标走下半椭圆
4. **完成**: 回到宿主或发射点后结束
**关键参数**:
- `TargetPoint`: 去程终点
- `TargetNode`: 返程跟随目标，通常为宿主
- `BoomerangPauseTime`: 折返点停顿时长
- `BoomerangReturnSpeedMultiplier`: 返程速度倍率
- `BoomerangArcHeight`: 弧线高度，0 = 自动按弦长推导
- `BoomerangIsClockwise`: 回旋偏移方向
- `ReachDistance`: 到达判定阈值
**技术特点**:
- 双半椭圆参数化：去程和返程分别采样半椭圆，不再是“直线出去再直线回来”
- 返程动态追踪：每帧重新采样宿主当前位置，宿主失效时回退到发射起点
- 朝向取曲线切线：视觉会顺着弧线转向，而不是硬朝目标点

---

### 鈿?鍐查攱绫?(Charge/)
#### ChargeStrategy - 鐩寸嚎鍐查攱
**鐢ㄩ€?*: 绐佽繘鏀诲嚮銆佺洿绾垮啿鍒恒€佸揩閫熸帴杩戠洰鏍?
**鏍稿績鐗规€?*:
- **杩借釜妯″紡**: 瀹炴椂鏇存柊鐩爣鏂瑰悜
- **閿佸畾妯″紡**: OnEnter 鏃朵竴娆℃€ч噰鏍锋柟鍚?- **鍔犻€熷害鏀寔**: 鍖€鍔犻€熻繍鍔?
---

### 馃攧 鐜粫绫?(Orbit/)
#### OrbitStrategy - 鐜粫杩愬姩
**鐢ㄩ€?*: 鎶ゅ崼鍗槦銆佺幆缁曟敾鍑汇€佽灪鏃嬭繍鍔?
**杩愬姩瀛﹀弬鏁?*:
- **瑙掕繍鍔?*: OrbitAngularSpeed, OrbitAngularAcceleration
- **寰勫悜杩愬姩**: OrbitRadius锛堝垵濮嬪€硷級+ OrbitRadiusScalarDriver锛堥€熷害/鍔犻€熷害/杈圭晫/瑙﹁竟绛栫暐鍧囩敱姝ょ粺涓€鎻忚堪锛?- **鍑犱綍鍙傛暟**: OrbitRadius, OrbitCenter

**鏁板姒傚康**:
- **鍚戝績鍔犻€熷害**: a_c = 蠅虏 脳 r
- **绾块€熷害**: v = 蠅 脳 r
- **铻烘棆杩愬姩**: 瑙掕繍鍔?+ 寰勫悜杩愬姩鐨勫悎鎴?
---

### 馃幃 鎺у埗绫?(Base/)
#### AIControlledStrategy - AI鎺у埗
#### PlayerInputStrategy - 鐜╁杈撳叆
#### AttachToHostStrategy - 闄勭潃瀹夸富

---

## 鏁板涓庣墿鐞嗗熀纭€

### 鍧愭爣绯荤粺
- **Godot 2D**: 宸︿笂瑙掑師鐐癸紝X鍚戝彸涓烘锛孻鍚戜笅涓烘
- **瑙掑害绯荤粺**: 0掳鍚戝彸锛?0掳鍚戜笅锛屾鍊奸『鏃堕拡
- **瑙掑害杞崲**: `寮у害 = 瑙掑害 脳 蟺 / 180`

### 鍚戦噺杩愮畻
```csharp
// 鍗曚綅鍚戦噺
direction = vector.Normalized();

// 鍨傜洿鍚戦噺锛堥『鏃堕拡90搴︼級
perp = new Vector2(-direction.Y, direction.X);

// 浣嶇Щ涓庨€熷害
velocity = displacement / time;
displacement = velocity * time;
```

### 姝ｅ鸡娉㈠姩
```csharp
// 鏍囧噯姝ｅ鸡鍑芥暟
y = A 脳 sin(2蟺 脳 f 脳 t + 蠁)

// 澧為噺璁＄畻锛堥伩鍏嶇疮绉宸級
deltaY = sin(t + 螖t) - sin(t)
```

### 鏁板€肩ǔ瀹氭€?- **闃查櫎闆?*: `Mathf.Max(delta, 0.001f)`
- **鍚戦噺闀垮害妫€鏌?*: `LengthSquared()` 閬垮厤寮€鏂?- **闃堝€煎垽鏂?*: `> 0.001f` 澶勭悊娴偣绮惧害

## 浣跨敤鎸囧崡

### 鍩烘湰鐢ㄦ硶
```csharp
entity.Events.Emit(GameEventType.Unit.MovementStarted,
    new GameEventType.Unit.MovementStartedEventData(MoveMode.SineWave, new MovementParams
    {
        Mode = MoveMode.SineWave,
        ActionSpeed = 400f,
        WaveAmplitude = 60f,
        WaveFrequency = 2f,
        MaxDistance = 1000f,
        DestroyOnComplete = true,
    }));
```

### 绛栫暐閫夋嫨
| 鍦烘櫙 | 鎺ㄨ崘绛栫暐 | 鍏抽敭鍙傛暟 |
|------|----------|----------|
| 铔囧舰瀛愬脊 | SineWave | WaveAmplitude, WaveFrequency |
| 澶嶆潅璺緞 | BezierCurve | BezierPoints, MaxDuration锛堝繀椤?> 0锛?|
| 寰€杩旀敾鍑?| Boomerang | TargetPoint, TargetNode, BoomerangPauseTime, BoomerangReturnSpeedMultiplier, BoomerangArcHeight, BoomerangIsClockwise |
| 绐佽繘鏀诲嚮 | Charge | ActionSpeed, Acceleration |
| 鎶ゅ崼鍗槦 | Orbit | OrbitRadius, OrbitAngularSpeed |

### 鎬ц兘鑰冭檻
- **OnEnter**: 涓€娆℃€у垵濮嬪寲锛岄伩鍏嶆瘡甯ч噸澶嶈绠?- **澧為噺璁＄畻**: 浣跨敤宸垎鑰岄潪绉垎锛屽噺灏戠疮绉宸?- **瀵硅薄澶嶇敤**: 绛栫暐瀹炰緥澶嶇敤锛岄伩鍏嶉绻?GC
- **鏁板€间紭鍖?*: 浣跨敤 `LengthSquared()` 閬垮厤涓嶅繀瑕佺殑寮€鏂硅繍绠?
## 鎵╁睍寮€鍙?
### 鏂板绛栫暐姝ラ
1. 瀹炵幇 `IMovementStrategy` 鎺ュ彛
2. 娣诲姞 `[ModuleInitializer]` 闈欐€佹敞鍐屾柟娉?3. 鍦?`MovementParams` 涓坊鍔犳墍闇€鍙傛暟
4. 鏇存柊 `MoveMode` 鏋氫妇
5. 缂栧啓鍗曞厓娴嬭瘯

### 浠ｇ爜瑙勮寖
- 浣跨敤涓枃娉ㄩ噴
- 閬靛惊鐜版湁鍛藉悕绾﹀畾
- 娣诲姞璇︾粏鐨?XML 鏂囨。
- 澶勭悊杈圭晫鎯呭喌鍜屾暟鍊肩ǔ瀹氭€?
## 璋冭瘯涓庝紭鍖?
### 甯歌闂
1. **鏂瑰悜璺冲彉**: 妫€鏌?OnEnter 鏄惁姝ｇ‘閿佸畾鍩哄噯鏂瑰悜
2. **绱Н璇樊**: 纭繚浣跨敤澧為噺璁＄畻鑰岄潪绉垎
3. **鏁板€间笉绋冲畾**: 娣诲姞闃堝€兼鏌ュ拰闃查櫎闆朵繚鎶?4. **鎬ц兘闂**: 閬垮厤鍦?Update 涓繘琛岄噸閲忕骇璁＄畻

### 璋冭瘯宸ュ叿
- **鍙鍖栬建杩?*: 缁樺埗绉诲姩璺緞
- **鍙傛暟鐩戞帶**: 瀹炴椂鏄剧ず鍏抽敭鍙傛暟
- **鎬ц兘鍒嗘瀽**: 鐩戞帶甯х巼鍜?GC 鍘嬪姏

---

*鏈枃妗ｉ殢浠ｇ爜鏇存柊鎸佺画缁存姢锛屾渶鍚庢洿鏂版椂闂? 2026-03-28*

### 閫氱敤鏍囬噺椹卞姩锛圫calarDriver锛?
閫傜敤浜庘€滃悓涓€杩愬姩绛栫暐鍐呴儴锛屾煇涓爣閲忓弬鏁颁細鎸佺画鍙樺寲鈥濈殑鍦烘櫙锛屼緥濡傦細

- Orbit 鍗婂緞鍦ㄥ尯闂村唴寰€杩?- Wave 鎸箙閫愭笎鍙樺ぇ鎴栭€愭笎鏀舵暃
- Wave 棰戠巼瑙﹁竟鍚庡弽鍚戝苟鎸?`BounceDecay` 琛板噺

鎺ㄨ崘妯″紡锛氫繚鐣欏熀纭€鍊煎瓧娈碉紝鍐嶆寕杞藉搴?`ScalarDriverParams?`銆?
- `null` = 涓嶅惎鐢ㄩ┍鍔紝淇濇寔鍩虹鍊煎父閲忎笉鍙橈紱闈?`null` 鏃跺啀鐢辩瓥鐣ュ疄渚嬬鏈夋寔鏈?`ScalarDriverState`銆?- 鏇村畬鏁寸殑鑱岃矗璇存槑銆佹棩蹇椾笂涓嬫枃鍜岃竟鐣屾ā寮忚涔夎 `../ScalarDriver/README.md`銆?
```csharp
new MovementParams
{
    Mode = MoveMode.SineWave,
    WaveAmplitude = 20f,
    WaveAmplitudeScalarDriver = new ScalarDriverParams
    {
        Enabled = true,
        Velocity = 40f,
        Min = 20f,
        Max = 80f,
        MinResponse = new ScalarBoundaryResponse { Mode = ScalarBoundMode.PingPong },
        MaxResponse = new ScalarBoundaryResponse
        {
            Mode = ScalarBoundMode.PingPong,
            BounceDecay = 0.8f,
        },
    },
}
```

