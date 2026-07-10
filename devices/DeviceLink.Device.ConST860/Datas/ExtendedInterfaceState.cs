namespace DeviceLink.Device.ConST860;

/// <summary>
/// 扩展接口状态
/// </summary>
public class ExtendedInterfaceState
{
    /// <summary>
    /// CPS状态
    /// </summary>
    public bool Cps { get; set; }

    /// <summary>
    /// DRV1状态
    /// </summary>
    public bool Drv1 { get; set; }

    /// <summary>
    /// DRV2状态
    /// </summary>
    public bool Drv2 { get; set; }

    /// <summary>
    /// DO1状态
    /// </summary>
    public bool Do1 { get; set; }

    /// <summary>
    /// DO2状态
    /// </summary>
    public bool Do2 { get; set; }

    /// <summary>
    /// DO3状态
    /// </summary>
    public bool Do3 { get; set; }

    /// <summary>
    /// DC24状态
    /// </summary>
    public bool Dc24 { get; set; }

    /// <summary>
    /// Switch状态
    /// </summary>
    public bool Switch { get; set; }

    public override string ToString()
    {
        return $"CPS={Cps}, DRV1={Drv1}, DRV2={Drv2}, DO1={Do1}, DO2={Do2}, DO3={Do3}, DC24={Dc24}, Switch={Switch}";
    }
}
