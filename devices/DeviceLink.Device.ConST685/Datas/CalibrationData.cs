using System;
using System.Collections.Generic;

namespace DeviceLink.Device.ConST685
{
    /// <summary>
    /// 校准数据信息（对应 Xmas11 CalibrationData 结构）
    /// </summary>
    public class CalibrationData
    {
        /// <summary>
        /// 数据ID
        /// </summary>
        public string ID { get; set; } = string.Empty;

        /// <summary>
        /// 数据键
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// 校准点个数
        /// </summary>
        public int PointCount { get; set; }

        /// <summary>
        /// 标准值列表
        /// </summary>
        public List<double> StandardList { get; set; } = new();

        /// <summary>
        /// 标准值单位
        /// </summary>
        public string StandardUnit { get; set; } = string.Empty;

        /// <summary>
        /// 校准点列表
        /// </summary>
        public List<double> CalPointList { get; set; } = new();

        /// <summary>
        /// 校准点单位
        /// </summary>
        public string CalPointUnit { get; set; } = string.Empty;

        /// <summary>
        /// 校准年份
        /// </summary>
        public int Year { get; set; }

        /// <summary>
        /// 校准月份
        /// </summary>
        public int Month { get; set; }

        /// <summary>
        /// 校准日
        /// </summary>
        public int Day { get; set; }

        /// <summary>
        /// 校准日期
        /// </summary>
        public DateTime CalDate
        {
            get => new DateTime(Year, Month, Day);
            set
            {
                Year = value.Year;
                Month = value.Month;
                Day = value.Day;
            }
        }

        /// <summary>
        /// 单位 ID（兼容旧接口）
        /// </summary>
        public int UnitId { get; set; }

        /// <summary>
        /// 校准点个数（兼容旧接口）
        /// </summary>
        public int CalibrationPointNum
        {
            get => PointCount;
            set => PointCount = value;
        }

        /// <summary>
        /// 数据获取状态
        /// </summary>
        public string DataStatus { get; set; } = string.Empty;

        /// <summary>
        /// 是否成功获取校准数据
        /// </summary>
        public bool IsGetCalDataPass { get; set; }

        /// <summary>
        /// 标准值列表（逗号分隔，兼容旧接口）
        /// </summary>
        public string StandardValues
        {
            get => string.Join(",", StandardList);
            set
            {
                StandardList.Clear();
                if (!string.IsNullOrEmpty(value))
                {
                    foreach (var s in value.Split(','))
                    {
                        if (double.TryParse(s.Trim(), out var v))
                            StandardList.Add(v);
                    }
                }
            }
        }

        /// <summary>
        /// 校准点列表（逗号分隔，兼容旧接口）
        /// </summary>
        public string CalibrationPoints
        {
            get => string.Join(",", CalPointList);
            set
            {
                CalPointList.Clear();
                if (!string.IsNullOrEmpty(value))
                {
                    foreach (var s in value.Split(','))
                    {
                        if (double.TryParse(s.Trim(), out var v))
                            CalPointList.Add(v);
                    }
                }
            }
        }

        // ============================================================
        // Xmas11 兼容属性（与 TAUBase.GetCalibrationData 返回数据结构完全一致）
        // ============================================================

        /// <summary>
        /// 标准值列表（Xmas11 兼容 — RefValueList）
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public List<double> RefValueList
        {
            get => StandardList;
            set => StandardList = value ?? new List<double>();
        }

        /// <summary>
        /// 校准点列表（Xmas11 兼容 — CalibrationPointList）
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public List<double> CalibrationPointList
        {
            get => CalPointList;
            set => CalPointList = value ?? new List<double>();
        }

        /// <summary>
        /// 校准点个数（Xmas11 兼容 — CalibrationPointCount）
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public int CalibrationPointCount
        {
            get => PointCount;
            set => PointCount = value;
        }

        /// <summary>
        /// 校准日期时间（Xmas11 兼容 — CalibrationDateTime）
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public DateTime CalibrationDateTime
        {
            get => CalDate;
            set => CalDate = value;
        }

        /// <summary>
        /// 当前单位 ID（Xmas11 兼容 — CurrentUnitId，对应 Xmas11 中 CurrentUnit 的 UnitID）
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public int CurrentUnitId
        {
            get => UnitId;
            set => UnitId = value;
        }

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid => IsGetCalDataPass && StandardList.Count > 0;

        /// <inheritdoc/>
        public override string ToString() =>
            $"ID={ID}, Key={Key}, Points={PointCount}, StandardUnit={StandardUnit}, CalPointUnit={CalPointUnit}, Date={CalDate:yyyy-MM-dd}";
    }
}
