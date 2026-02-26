using Godot;
using System;

/// <summary>
/// 数据初始化组件
/// 
/// 核心职责：
/// 1. 在实体生成后，负责将 "静态配置属性" 同步初始化为 "运行时动态属性"。
/// 2. 解决例如：配置了 MaxHp 但 CurrentHp 为空，导致生成即死的问题。
/// </summary>
public partial class DataInitComponent : Node, IComponent
{
    private static readonly Log _log = new(nameof(DataInitComponent));

    private IEntity? _entity;
    private Data? _data;

    // ================= IComponent 实现 =================

    public void OnComponentRegistered(Node entity)
    {
        if (entity is IEntity iEntity)
        {
            _entity = iEntity;
            _data = iEntity.Data;

            InitializeData();
        }
    }

    public void OnComponentUnregistered()
    {
        _entity = null;
        _data = null;
    }



    // ================= 核心逻辑 =================

    /// <summary>
    /// 执行数据初始化规则
    /// </summary>
    private void InitializeData()
    {
        if (_data == null) return;
        // 规则 1: 初始化当前血量
        _data.Set(DataKey.CurrentHp, _data.Get<float>(DataKey.FinalHp));

    }
}
