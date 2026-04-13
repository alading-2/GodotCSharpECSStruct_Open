using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 属性测试模块。
/// <para>
/// 提供一个运行时调试面板，用于按分类查看并直接编辑实体的 Data 属性。
/// 每个属性条目都由独立场景资产承载，模块只负责绑定数据和刷新列表。
/// </para>
/// </summary>
public partial class AttributeTestModule : TestModuleBase
{
    /// <summary>单个分类页的缓存结构，保存分类标题和该分类下的可编辑 DataMeta 列表。</summary>
    private sealed record CategoryEntry(string Title, List<DataMeta> Metas);

    /// <summary>Feature 调试服务，用于临时 Modifier 的挂载、读取与清理。</summary>
    private readonly FeatureDebugService _featureDebugService = new();

    /// <summary>所有可编辑分类的缓存列表。</summary>
    private readonly List<CategoryEntry> _categories = new();

    [Export] private PackedScene? _editorRowScene;

    /// <summary>左侧分类列表。</summary>
    private ItemList _categoryList = null!;

    /// <summary>右侧编辑器容器。</summary>
    private VBoxContainer _editorContainer = null!;

    /// <summary>顶部实体提示文字。</summary>
    private Label _entityHintLabel = null!;

    /// <summary>当前订阅了 Data 变化事件的实体。</summary>
    private IEntity? _subscribedEntity;

    /// <summary>模块在 TestSystem 下拉框中的显示名称。</summary>
    internal override string DisplayName => "属性测试";

    /// <summary>
    /// 模块初始化。
    /// </summary>
    internal override void Initialize(ITestModuleHost host)
    {
        base.Initialize(host);
        BuildCategoryData();
        CacheUiNodes();
        PopulateCategoryList();
        if (_categories.Count > 0)
        {
            _categoryList.Select(0);
        }

        RequestRefresh();
    }

    /// <summary>
    /// 选中实体变化时，先解除旧订阅，再绑定新实体并请求刷新。
    /// </summary>
    internal override void OnSelectedEntityChanged(IEntity? entity)
    {
        UnsubscribeEntityEvents();
        base.OnSelectedEntityChanged(entity);
        if (Visible)
        {
            SubscribeEntityEvents();
        }

        RequestRefresh();
    }

    /// <summary>
    /// 模块被切换到前台时，恢复订阅并刷新。
    /// </summary>
    internal override void OnActivated()
    {
        SubscribeEntityEvents();
        RequestRefresh();
    }

    /// <summary>
    /// 模块离开前台时，取消订阅。
    /// </summary>
    internal override void OnDeactivated()
    {
        UnsubscribeEntityEvents();
    }

    /// <summary>
    /// 刷新当前显示内容。
    /// </summary>
    internal override void Refresh()
    {
        ClearEditorRows();

        if (selectedEntity is not Node entityNode)
        {
            _entityHintLabel.Text = "请先选择一个实体";
            return;
        }

        var entityName = selectedEntity.Data.Get<string>(DataKey.Name.Key);
        if (string.IsNullOrWhiteSpace(entityName))
        {
            entityName = entityNode.Name.ToString();
        }

        _entityHintLabel.Text = $"当前实体：{entityName} ({entityNode.GetType().Name})";

        if (_categories.Count == 0)
        {
            return;
        }

        var selectedItems = _categoryList.GetSelectedItems();
        var selectedIndex = selectedItems.Length > 0 ? selectedItems[0] : 0;
        var categoryIndex = Mathf.Clamp(selectedIndex, 0, _categories.Count - 1);
        var category = _categories[categoryIndex];

        foreach (var meta in category.Metas)
        {
            var row = InstantiateScene<AttributeEditorRow>(_editorRowScene, nameof(AttributeEditorRow));
            row.Bind(
                selectedEntity, // entity
                meta, // meta
                _featureDebugService, // featureDebugService
                ApplyMetaValue, // onValueCommitted
                message => _entityHintLabel.Text = message, // onStatusChanged
                RequestRefresh // onRefreshRequested
            );
            _editorContainer.AddChild(row);
        }
    }

    /// <summary>
    /// 收集所有可编辑分类的数据元信息。
    /// </summary>
    private void BuildCategoryData()
    {
        AddCategory("生命", DataCategory_Attribute.Health);
        AddCategory("魔法", DataCategory_Attribute.Mana);
        AddCategory("攻击", DataCategory_Attribute.Attack);
        AddCategory("防御", DataCategory_Attribute.Defense);
        AddCategory("技能", DataCategory_Attribute.Skill);
        AddCategory("移动", DataCategory_Attribute.Movement);
        AddCategory("闪避", DataCategory_Attribute.Dodge);
        AddCategory("暴击", DataCategory_Attribute.Crit);
        AddCategory("资源", DataCategory_Attribute.Resource);
        AddCategory("状态", DataCategory_Unit.State);
        AddCategory("恢复控制", DataCategory_Unit.Recovery);
    }

    /// <summary>
    /// 将某个分类中可编辑的 DataMeta 收集进缓存。
    /// </summary>
    private void AddCategory(string title, Enum category)
    {
        var metas = DataRegistry.GetCachedMetaByCategory(category)
            .Where(IsEditableMeta)
            .OrderBy(meta => meta.DisplayName)
            .ToList();

        if (metas.Count == 0)
        {
            return;
        }

        _categories.Add(new CategoryEntry(title, metas));
    }

    /// <summary>
    /// 判断某个元数据是否适合在运行时编辑。
    /// </summary>
    private static bool IsEditableMeta(DataMeta meta)
    {
        if (meta.IsComputed)
        {
            return false;
        }

        return meta.IsBoolean || meta.IsNumeric || meta.IsEnum || meta.IsString || meta.HasOptions;
    }

    private void CacheUiNodes()
    {
        MouseFilter = Control.MouseFilterEnum.Stop;
        _entityHintLabel = GetNode<Label>("EntityHintLabel");
        _categoryList = GetNode<ItemList>("Split/CategoryList");
        _editorContainer = GetNode<VBoxContainer>("Split/EditorScroll/EditorContainer");
        _categoryList.ItemSelected += OnCategorySelected;
    }

    private void PopulateCategoryList()
    {
        _categoryList.Clear();
        foreach (var category in _categories)
        {
            _categoryList.AddItem(category.Title);
        }
    }

    /// <summary>
    /// 左侧分类切换回调。
    /// </summary>
    private void OnCategorySelected(long index)
    {
        RequestRefresh();
    }

    /// <summary>
    /// 把 UI 中编辑后的值写回实体 Data。
    /// </summary>
    private void ApplyMetaValue(DataMeta meta, object value)
    {
        if (selectedEntity == null)
        {
            return;
        }

        if (meta.Key == DataKey.CurrentHp)
        {
            var maxHp = selectedEntity.Data.Get<float>(DataKey.FinalHp);
            value = Mathf.Clamp(Convert.ToSingle(value), 0f, maxHp);
        }
        else if (meta.Key == DataKey.CurrentMana)
        {
            var maxMana = selectedEntity.Data.Get<float>(DataKey.FinalMana);
            value = Mathf.Clamp(Convert.ToSingle(value), 0f, maxMana);
        }

        if (meta.IsInteger)
        {
            selectedEntity.Data.Set(meta.Key, Convert.ToInt32(value));
        }
        else if (meta.IsFloatingPoint)
        {
            selectedEntity.Data.Set(meta.Key, Convert.ToSingle(value));
        }
        else if (meta.IsBoolean)
        {
            selectedEntity.Data.Set(meta.Key, Convert.ToBoolean(value));
        }
        else if (meta.IsString)
        {
            selectedEntity.Data.Set(meta.Key, Convert.ToString(value) ?? string.Empty);
        }
        else
        {
            selectedEntity.Data.Set(meta.Key, value);
        }

        ClampCurrentResourceIfNeeded(meta.Key);
        RequestRefresh();
    }

    /// <summary>
    /// 当最大生命 / 最大魔法相关字段变化时，保证当前值不超过新的上限。
    /// </summary>
    private void ClampCurrentResourceIfNeeded(string key)
    {
        if (selectedEntity == null)
        {
            return;
        }

        if (key == DataKey.BaseHp || key == DataKey.HpBonus)
        {
            var currentHp = selectedEntity.Data.Get<float>(DataKey.CurrentHp);
            var maxHp = selectedEntity.Data.Get<float>(DataKey.FinalHp);
            if (currentHp > maxHp)
            {
                selectedEntity.Data.Set(DataKey.CurrentHp, maxHp);
            }
        }

        if (key == DataKey.BaseMana || key == DataKey.ManaBonus)
        {
            var currentMana = selectedEntity.Data.Get<float>(DataKey.CurrentMana);
            var maxMana = selectedEntity.Data.Get<float>(DataKey.FinalMana);
            if (currentMana > maxMana)
            {
                selectedEntity.Data.Set(DataKey.CurrentMana, maxMana);
            }
        }
    }

    /// <summary>
    /// 订阅当前实体的 Data 变化事件。
    /// </summary>
    private void SubscribeEntityEvents()
    {
        if (selectedEntity == null || ReferenceEquals(selectedEntity, _subscribedEntity))
        {
            return;
        }

        _subscribedEntity = selectedEntity;
        _subscribedEntity.Events.On<GameEventType.Data.PropertyChangedEventData>(
            GameEventType.Data.PropertyChanged,
            OnEntityDataChanged
        );
    }

    /// <summary>
    /// 取消当前实体的 Data 变化订阅。
    /// </summary>
    private void UnsubscribeEntityEvents()
    {
        if (_subscribedEntity == null)
        {
            return;
        }

        _subscribedEntity.Events.Off<GameEventType.Data.PropertyChangedEventData>(
            GameEventType.Data.PropertyChanged,
            OnEntityDataChanged
        );
        _subscribedEntity = null;
    }

    /// <summary>
    /// 数据变化后的统一刷新回调。
    /// </summary>
    private void OnEntityDataChanged(GameEventType.Data.PropertyChangedEventData evt)
    {
        if (!Visible)
        {
            return;
        }

        RequestRefresh();
    }

    /// <summary>
    /// 清理右侧编辑器中的旧行。
    /// </summary>
    private void ClearEditorRows()
    {
        foreach (var child in _editorContainer.GetChildren())
        {
            child.QueueFree();
        }
    }

    private static T InstantiateScene<T>(PackedScene? scene, string sceneName) where T : Node
    {
        if (scene == null)
        {
            throw new InvalidOperationException($"属性测试场景未配置: {sceneName}");
        }

        var instance = scene.Instantiate<T>();
        if (instance == null)
        {
            throw new InvalidOperationException($"属性测试场景实例化失败: {sceneName}");
        }

        return instance;
    }
}
