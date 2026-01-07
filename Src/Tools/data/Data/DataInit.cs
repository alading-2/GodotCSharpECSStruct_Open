using Godot;
using System.Linq;
using System.Runtime.CompilerServices;

/// <summary>
/// Data 系统初始化
/// 负责 DataRegistry 的业务元数据和计算逻辑注册
/// </summary>
public partial class DataInit : Node
{
    private static readonly Log _log = new("DataInit");

    [ModuleInitializer]
    public static void Initialize()
    {
        AutoLoad.Register("DataInit", "res://Src/Tools/Data/DataInit.cs", AutoLoad.Priority.Core);
    }

    public override void _EnterTree()
    {
        InitializeDataRegistry();
    }

    private void InitializeDataRegistry()
    {
        _log.Info("开始注册全局 Data 元数据...");
        AttributeDataRegister.Register();


        var keyCount = DataRegistry.GetAllKeys().Count();
        _log.Success($"DataRegistry 业务数据注册完成：共 {keyCount} 个键");
    }
}
