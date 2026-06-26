namespace DeviceLink.Device.ConST171A
{
    /// <summary>
    /// FOC 状态 —— DIAGnostic:FOC? 返回值
    /// 格式：0,0（前级FOC状态,增压FOC状态；0=正常,1=错误）
    /// </summary>
    public class FocState
    {
        /// <summary>前级 FOC 是否正常（true=正常, false=错误）</summary>
        public bool PreStageOk { get; set; } = true;

        /// <summary>增压 FOC 是否正常（true=正常, false=错误）</summary>
        public bool BoostOk { get; set; } = true;

        /// <summary>是否全部正常</summary>
        public bool IsAllOk => PreStageOk && BoostOk;

        /// <inheritdoc/>
        public override string ToString() =>
            $"前级FOC={(PreStageOk ? "正常" : "错误")}, 增压FOC={(BoostOk ? "正常" : "错误")}";
    }
}
