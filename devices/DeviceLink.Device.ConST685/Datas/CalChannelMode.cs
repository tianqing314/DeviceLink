namespace DeviceLink.Device.ConST685
{
    /// <summary>
    /// 校准项模式
    /// </summary>
    public struct CalChannelMode
    {

        /// <summary>
        /// 静态构造函数
        /// </summary>
        static CalChannelMode()
        {
            _REF1 = new CalChannelMode(10, "REF1", "01");
            _REF2 = new CalChannelMode(20, "REF2", "02");

            _CH1_01A = new CalChannelMode(1, "CH1-01A", "101");
            _CH1_02A = new CalChannelMode(2, "CH1-02A", "102");
            _CH1_03A = new CalChannelMode(3, "CH1-03A", "103");
            _CH1_04A = new CalChannelMode(4, "CH1-04A", "104");
            _CH1_05A = new CalChannelMode(5, "CH1-05A", "105");
            _CH1_06A = new CalChannelMode(6, "CH1-06A", "106");
            _CH1_07A = new CalChannelMode(7, "CH1-07A", "107");
            _CH1_08A = new CalChannelMode(8, "CH1-08A", "108");
            _CH1_09A = new CalChannelMode(9, "CH1-09A", "109");
            _CH1_10A = new CalChannelMode(10, "CH1-10A", "110");

            _CH1_01B = new CalChannelMode(11, "CH1-01B", "111");
            _CH1_02B = new CalChannelMode(12, "CH1-02B", "112");
            _CH1_03B = new CalChannelMode(13, "CH1-03B", "113");
            _CH1_04B = new CalChannelMode(14, "CH1-04B", "114");
            _CH1_05B = new CalChannelMode(15, "CH1-05B", "115");
            _CH1_06B = new CalChannelMode(16, "CH1-06B", "116");
            _CH1_07B = new CalChannelMode(17, "CH1-07B", "117");
            _CH1_08B = new CalChannelMode(18, "CH1-08B", "118");
            _CH1_09B = new CalChannelMode(19, "CH1-09B", "119");
            _CH1_10B = new CalChannelMode(20, "CH1-10B", "120");


            _CH2_01A = new CalChannelMode(1, "CH2-01A", "201");
            _CH2_02A = new CalChannelMode(2, "CH2-02A", "202");
            _CH2_03A = new CalChannelMode(3, "CH2-03A", "203");
            _CH2_04A = new CalChannelMode(4, "CH2-04A", "204");
            _CH2_05A = new CalChannelMode(5, "CH2-05A", "205");
            _CH2_06A = new CalChannelMode(6, "CH2-06A", "206");
            _CH2_07A = new CalChannelMode(7, "CH2-07A", "207");
            _CH2_08A = new CalChannelMode(8, "CH2-08A", "208");
            _CH2_09A = new CalChannelMode(9, "CH2-09A", "209");
            _CH2_10A = new CalChannelMode(10, "CH2-10A", "210");

            _CH2_01B = new CalChannelMode(11, "CH2-01B", "211");
            _CH2_02B = new CalChannelMode(12, "CH2-02B", "212");
            _CH2_03B = new CalChannelMode(13, "CH2-03B", "213");
            _CH2_04B = new CalChannelMode(14, "CH2-04B", "214");
            _CH2_05B = new CalChannelMode(15, "CH2-05B", "215");
            _CH2_06B = new CalChannelMode(16, "CH2-06B", "216");
            _CH2_07B = new CalChannelMode(17, "CH2-07B", "217");
            _CH2_08B = new CalChannelMode(18, "CH2-08B", "218");
            _CH2_09B = new CalChannelMode(19, "CH2-09B", "219");
            _CH2_10B = new CalChannelMode(20, "CH2-10B", "220");

            _CH1_01 = new CalChannelMode(1, "CH1-01", "101");
            _CH1_02 = new CalChannelMode(2, "CH1-02", "102");
            _CH1_03 = new CalChannelMode(3, "CH1-03", "103");
            _CH1_04 = new CalChannelMode(4, "CH1-04", "104");
            _CH1_05 = new CalChannelMode(5, "CH1-05", "105");
            _CH1_06 = new CalChannelMode(6, "CH1-06", "106");
            _CH1_07 = new CalChannelMode(7, "CH1-07", "107");
            _CH1_08 = new CalChannelMode(8, "CH1-08", "108");
            _CH1_09 = new CalChannelMode(9, "CH1-09", "109");
            _CH1_10 = new CalChannelMode(10, "CH1-10", "110");

            _CH2_01 = new CalChannelMode(1, "CH2-01", "201");
            _CH2_02 = new CalChannelMode(2, "CH2-02", "202");
            _CH2_03 = new CalChannelMode(3, "CH2-03", "203");
            _CH2_04 = new CalChannelMode(4, "CH2-04", "204");
            _CH2_05 = new CalChannelMode(5, "CH2-05", "205");
            _CH2_06 = new CalChannelMode(6, "CH2-06", "206");
            _CH2_07 = new CalChannelMode(7, "CH2-07", "207");
            _CH2_08 = new CalChannelMode(8, "CH2-08", "208");
            _CH2_09 = new CalChannelMode(9, "CH2-09", "209");
            _CH2_10 = new CalChannelMode(10, "CH2-10", "210");

            _Unkonw = new CalChannelMode(500, "Unknow", "00");
        }

        /// <summary>
        /// 私有构造函数
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="modeID"></param>
        private CalChannelMode(int id, string name, string modeID)
        {
            _ID = id;
            _Name = name;
            _ModeID = modeID;
        }

        int _ID;
        string _Name;
        string _ModeID;
        static CalChannelMode _REF1;
        static CalChannelMode _REF2;
        static CalChannelMode _Unkonw;

        static CalChannelMode _CH1_01A;
        static CalChannelMode _CH1_02A;
        static CalChannelMode _CH1_03A;
        static CalChannelMode _CH1_04A;
        static CalChannelMode _CH1_05A;
        static CalChannelMode _CH1_06A;
        static CalChannelMode _CH1_07A;
        static CalChannelMode _CH1_08A;
        static CalChannelMode _CH1_09A;
        static CalChannelMode _CH1_10A;

        static CalChannelMode _CH1_01B;
        static CalChannelMode _CH1_02B;
        static CalChannelMode _CH1_03B;
        static CalChannelMode _CH1_04B;
        static CalChannelMode _CH1_05B;
        static CalChannelMode _CH1_06B;
        static CalChannelMode _CH1_07B;
        static CalChannelMode _CH1_08B;
        static CalChannelMode _CH1_09B;
        static CalChannelMode _CH1_10B;

        static CalChannelMode _CH2_01A;
        static CalChannelMode _CH2_02A;
        static CalChannelMode _CH2_03A;
        static CalChannelMode _CH2_04A;
        static CalChannelMode _CH2_05A;
        static CalChannelMode _CH2_06A;
        static CalChannelMode _CH2_07A;
        static CalChannelMode _CH2_08A;
        static CalChannelMode _CH2_09A;
        static CalChannelMode _CH2_10A;

        static CalChannelMode _CH2_01B;
        static CalChannelMode _CH2_02B;
        static CalChannelMode _CH2_03B;
        static CalChannelMode _CH2_04B;
        static CalChannelMode _CH2_05B;
        static CalChannelMode _CH2_06B;
        static CalChannelMode _CH2_07B;
        static CalChannelMode _CH2_08B;
        static CalChannelMode _CH2_09B;
        static CalChannelMode _CH2_10B;

        static CalChannelMode _CH1_01;
        static CalChannelMode _CH1_02;
        static CalChannelMode _CH1_03;
        static CalChannelMode _CH1_04;
        static CalChannelMode _CH1_05;
        static CalChannelMode _CH1_06;
        static CalChannelMode _CH1_07;
        static CalChannelMode _CH1_08;
        static CalChannelMode _CH1_09;
        static CalChannelMode _CH1_10;

        static CalChannelMode _CH2_01;
        static CalChannelMode _CH2_02;
        static CalChannelMode _CH2_03;
        static CalChannelMode _CH2_04;
        static CalChannelMode _CH2_05;
        static CalChannelMode _CH2_06;
        static CalChannelMode _CH2_07;
        static CalChannelMode _CH2_08;
        static CalChannelMode _CH2_09;
        static CalChannelMode _CH2_10;

        /// <summary>
        /// 编号
        /// </summary>
        public int ID
        {
            get
            {
                return _ID;
            }

            set
            {
                _ID = value;
            }
        }
        /// <summary>
        /// 名称
        /// </summary>
        public string Name
        {
            get
            {
                return _Name;
            }

            set
            {
                _Name = value;
            }
        }
        /// <summary>
        /// 模型ID
        /// </summary>
        public string ModeID
        {
            get
            {
                return _ModeID;
            }

            set
            {
                _ModeID = value;
            }
        }
        /// <summary>
        /// 前面板通道1
        /// </summary>
        public static CalChannelMode REF1
        {
            get
            {
                return _REF1;
            }

            set
            {
                _REF1 = value;
            }
        }
        /// <summary>
        /// 前面板通道2
        /// </summary>
        public static CalChannelMode REF2
        {
            get
            {
                return _REF2;
            }

            set
            {
                _REF2 = value;
            }
        }

        /// <summary>
        /// 未知通道
        /// </summary>
        public static CalChannelMode Unkonw
        {
            get
            {
                return _Unkonw;
            }

            set
            {
                _Unkonw = value;
            }
        }

        /// <summary>
        /// 内嵌盒-一次盒-01A
        /// </summary>
        public static CalChannelMode CH1_01A
        {
            get
            {
                return _CH1_01A;
            }

            set
            {
                _CH1_01A = value;
            }
        }

        /// <summary>
        /// 内嵌盒-一次盒-02A
        /// </summary>
        public static CalChannelMode CH1_02A
        {
            get
            {
                return _CH1_02A;
            }

            set
            {
                _CH1_02A = value;
            }
        }

        /// <summary>
        /// 内嵌盒-一次盒-03A
        /// </summary>
        public static CalChannelMode CH1_03A
        {
            get
            {
                return _CH1_03A;
            }

            set
            {
                _CH1_03A = value;
            }
        }

        /// <summary>
        /// 内嵌盒-一次盒-04A
        /// </summary>
        public static CalChannelMode CH1_04A
        {
            get
            {
                return _CH1_04A;
            }

            set
            {
                _CH1_04A = value;
            }
        }

        /// <summary>
        /// 内嵌盒-一次盒-05A
        /// </summary>
        public static CalChannelMode CH1_05A
        {
            get
            {
                return _CH1_05A;
            }

            set
            {
                _CH1_05A = value;
            }
        }

        /// <summary>
        /// 内嵌盒-一次盒-06A
        /// </summary>
        public static CalChannelMode CH1_06A
        {
            get
            {
                return _CH1_06A;
            }

            set
            {
                _CH1_06A = value;
            }
        }

        /// <summary>
        /// 内嵌盒-一次盒-07A
        /// </summary>
        public static CalChannelMode CH1_07A
        {
            get
            {
                return _CH1_07A;
            }

            set
            {
                _CH1_07A = value;
            }
        }

        /// <summary>
        /// 内嵌盒-一次盒-08A
        /// </summary>
        public static CalChannelMode CH1_08A
        {
            get
            {
                return _CH1_08A;
            }

            set
            {
                _CH1_08A = value;
            }
        }

        /// <summary>
        /// 内嵌盒-一次盒-09A
        /// </summary>
        public static CalChannelMode CH1_09A
        {
            get
            {
                return _CH1_09A;
            }

            set
            {
                _CH1_09A = value;
            }
        }

        /// <summary>
        /// 内嵌盒-一次盒-10A
        /// </summary>
        public static CalChannelMode CH1_10A
        {
            get
            {
                return _CH1_10A;
            }

            set
            {
                _CH1_10A = value;
            }
        }

        /// <summary>
        /// 内嵌盒-一次盒-01B
        /// </summary>
        public static CalChannelMode CH1_01B
        {
            get
            {
                return _CH1_01B;
            }

            set
            {
                _CH1_01B = value;
            }
        }

        /// <summary>
        /// 内嵌盒-一次盒-02B
        /// </summary>
        public static CalChannelMode CH1_02B
        {
            get
            {
                return _CH1_02B;
            }

            set
            {
                _CH1_02B = value;
            }
        }

        /// <summary>
        /// 内嵌盒-一次盒-03B
        /// </summary>
        public static CalChannelMode CH1_03B
        {
            get
            {
                return _CH1_03B;
            }

            set
            {
                _CH1_03B = value;
            }
        }

        /// <summary>
        /// 内嵌盒-一次盒-04B
        /// </summary>
        public static CalChannelMode CH1_04B
        {
            get
            {
                return _CH1_04B;
            }

            set
            {
                _CH1_04B = value;
            }
        }

        /// <summary>
        /// 内嵌盒-一次盒-05B
        /// </summary>
        public static CalChannelMode CH1_05B
        {
            get
            {
                return _CH1_05B;
            }

            set
            {
                _CH1_05B = value;
            }
        }

        /// <summary>
        /// 内嵌盒-一次盒-06B
        /// </summary>
        public static CalChannelMode CH1_06B
        {
            get
            {
                return _CH1_06B;
            }

            set
            {
                _CH1_06B = value;
            }
        }

        /// <summary>
        /// 内嵌盒-一次盒-07B
        /// </summary>
        public static CalChannelMode CH1_07B
        {
            get
            {
                return _CH1_07B;
            }

            set
            {
                _CH1_07B = value;
            }
        }

        /// <summary>
        /// 内嵌盒-一次盒-08B
        /// </summary>
        public static CalChannelMode CH1_08B
        {
            get
            {
                return _CH1_08B;
            }

            set
            {
                _CH1_08B = value;
            }
        }

        /// <summary>
        /// 内嵌盒-一次盒-09B
        /// </summary>
        public static CalChannelMode CH1_09B
        {
            get
            {
                return _CH1_09B;
            }

            set
            {
                _CH1_09B = value;
            }
        }

        /// <summary>
        /// 内嵌盒-一次盒-10B
        /// </summary>
        public static CalChannelMode CH1_10B
        {
            get
            {
                return _CH1_10B;
            }

            set
            {
                _CH1_10B = value;
            }
        }

        /// <summary>
        /// 外接盒-一次盒-01A
        /// </summary>
        public static CalChannelMode CH2_01A
        {
            get
            {
                return _CH2_01A;
            }

            set
            {
                _CH2_01A = value;
            }
        }

        /// <summary>
        /// 外接盒-一次盒-02A
        /// </summary>
        public static CalChannelMode CH2_02A
        {
            get
            {
                return _CH2_02A;
            }

            set
            {
                _CH2_02A = value;
            }
        }

        /// <summary>
        /// 外接盒-一次盒-03A
        /// </summary>
        public static CalChannelMode CH2_03A
        {
            get
            {
                return _CH2_03A;
            }

            set
            {
                _CH2_03A = value;
            }
        }

        /// <summary>
        /// 外接盒-一次盒-04A
        /// </summary>
        public static CalChannelMode CH2_04A
        {
            get
            {
                return _CH2_04A;
            }

            set
            {
                _CH2_04A = value;
            }
        }

        /// <summary>
        /// 外接盒-一次盒-05A
        /// </summary>
        public static CalChannelMode CH2_05A
        {
            get
            {
                return _CH2_05A;
            }

            set
            {
                _CH2_05A = value;
            }
        }

        /// <summary>
        /// 外接盒-一次盒-06A
        /// </summary>
        public static CalChannelMode CH2_06A
        {
            get
            {
                return _CH2_06A;
            }

            set
            {
                _CH2_06A = value;
            }
        }

        /// <summary>
        /// 外接盒-一次盒-07A
        /// </summary>
        public static CalChannelMode CH2_07A
        {
            get
            {
                return _CH2_07A;
            }

            set
            {
                _CH2_07A = value;
            }
        }

        /// <summary>
        /// 外接盒-一次盒-08A
        /// </summary>
        public static CalChannelMode CH2_08A
        {
            get
            {
                return _CH2_08A;
            }

            set
            {
                _CH2_08A = value;
            }
        }

        /// <summary>
        /// 外接盒-一次盒-09A
        /// </summary>
        public static CalChannelMode CH2_09A
        {
            get
            {
                return _CH2_09A;
            }

            set
            {
                _CH2_09A = value;
            }
        }

        /// <summary>
        /// 外接盒-一次盒-10A
        /// </summary>
        public static CalChannelMode CH2_10A
        {
            get
            {
                return _CH2_10A;
            }

            set
            {
                _CH2_10A = value;
            }
        }

        /// <summary>
        /// 外接盒-一次盒-01B
        /// </summary>
        public static CalChannelMode CH2_01B
        {
            get
            {
                return _CH2_01B;
            }

            set
            {
                _CH2_01B = value;
            }
        }

        /// <summary>
        /// 外接盒-一次盒-02B
        /// </summary>
        public static CalChannelMode CH2_02B
        {
            get
            {
                return _CH2_02B;
            }

            set
            {
                _CH2_02B = value;
            }
        }

        /// <summary>
        /// 外接盒-一次盒-03B
        /// </summary>
        public static CalChannelMode CH2_03B
        {
            get
            {
                return _CH2_03B;
            }

            set
            {
                _CH2_03B = value;
            }
        }

        /// <summary>
        /// 外接盒-一次盒-04B
        /// </summary>
        public static CalChannelMode CH2_04B
        {
            get
            {
                return _CH2_04B;
            }

            set
            {
                _CH2_04B = value;
            }
        }

        /// <summary>
        /// 外接盒-一次盒-05B
        /// </summary>
        public static CalChannelMode CH2_05B
        {
            get
            {
                return _CH2_05B;
            }

            set
            {
                _CH2_05B = value;
            }
        }

        /// <summary>
        /// 外接盒-一次盒-06B
        /// </summary>
        public static CalChannelMode CH2_06B
        {
            get
            {
                return _CH2_06B;
            }

            set
            {
                _CH2_06B = value;
            }
        }

        /// <summary>
        /// 外接盒-一次盒-07B
        /// </summary>
        public static CalChannelMode CH2_07B
        {
            get
            {
                return _CH2_07B;
            }

            set
            {
                _CH2_07B = value;
            }
        }

        /// <summary>
        /// 外接盒-一次盒-08B
        /// </summary>
        public static CalChannelMode CH2_08B
        {
            get
            {
                return _CH2_08B;
            }

            set
            {
                _CH2_08B = value;
            }
        }

        /// <summary>
        /// 外接盒-一次盒-09B
        /// </summary>
        public static CalChannelMode CH2_09B
        {
            get
            {
                return _CH2_09B;
            }

            set
            {
                _CH2_09B = value;
            }
        }

        /// <summary>
        /// 外接盒-一次盒-10B
        /// </summary>
        public static CalChannelMode CH2_10B
        {
            get
            {
                return _CH2_10B;
            }

            set
            {
                _CH2_10B = value;
            }
        }

        /// <summary>
        /// 内嵌盒-二次盒-01
        /// </summary>
        public static CalChannelMode CH1_01
        {
            get
            {
                return _CH1_01;
            }

            set
            {
                _CH1_01 = value;
            }
        }

        /// <summary>
        /// 内嵌盒-二次盒-02
        /// </summary>
        public static CalChannelMode CH1_02
        {
            get
            {
                return _CH1_02;
            }

            set
            {
                _CH1_02 = value;
            }
        }

        /// <summary>
        /// 内嵌盒-二次盒-03
        /// </summary>
        public static CalChannelMode CH1_03
        {
            get
            {
                return _CH1_03;
            }

            set
            {
                _CH1_03 = value;
            }
        }

        /// <summary>
        /// 内嵌盒-二次盒-04
        /// </summary>
        public static CalChannelMode CH1_04
        {
            get
            {
                return _CH1_04;
            }

            set
            {
                _CH1_04 = value;
            }
        }

        /// <summary>
        /// 内嵌盒-二次盒-05
        /// </summary>
        public static CalChannelMode CH1_05
        {
            get
            {
                return _CH1_05;
            }

            set
            {
                _CH1_05 = value;
            }
        }

        /// <summary>
        /// 内嵌盒-二次盒-06
        /// </summary>
        public static CalChannelMode CH1_06
        {
            get
            {
                return _CH1_06;
            }

            set
            {
                _CH1_06 = value;
            }
        }

        /// <summary>
        /// 内嵌盒-二次盒-07
        /// </summary>
        public static CalChannelMode CH1_07
        {
            get
            {
                return _CH1_07;
            }

            set
            {
                _CH1_07 = value;
            }
        }

        /// <summary>
        /// 内嵌盒-二次盒-08
        /// </summary>
        public static CalChannelMode CH1_08
        {
            get
            {
                return _CH1_08;
            }

            set
            {
                _CH1_08 = value;
            }
        }

        /// <summary>
        /// 内嵌盒-二次盒-09
        /// </summary>
        public static CalChannelMode CH1_09
        {
            get
            {
                return _CH1_09;
            }

            set
            {
                _CH1_09 = value;
            }
        }

        /// <summary>
        /// 内嵌盒-二次盒-10
        /// </summary>
        public static CalChannelMode CH1_10
        {
            get
            {
                return _CH1_10;
            }

            set
            {
                _CH1_10 = value;
            }
        }

        /// <summary>
        /// 外接盒-二次盒-01
        /// </summary>
        public static CalChannelMode CH2_01
        {
            get
            {
                return _CH2_01;
            }

            set
            {
                _CH2_01 = value;
            }
        }

        /// <summary>
        /// 外接盒-二次盒-02
        /// </summary>
        public static CalChannelMode CH2_02
        {
            get
            {
                return _CH2_02;
            }

            set
            {
                _CH2_02 = value;
            }
        }

        /// <summary>
        /// 外接盒-二次盒-03
        /// </summary>
        public static CalChannelMode CH2_03
        {
            get
            {
                return _CH2_03;
            }

            set
            {
                _CH2_03 = value;
            }
        }

        /// <summary>
        /// 外接盒-二次盒-04
        /// </summary>
        public static CalChannelMode CH2_04
        {
            get
            {
                return _CH2_04;
            }

            set
            {
                _CH2_04 = value;
            }
        }

        /// <summary>
        /// 外接盒-二次盒-05
        /// </summary>
        public static CalChannelMode CH2_05
        {
            get
            {
                return _CH2_05;
            }

            set
            {
                _CH2_05 = value;
            }
        }

        /// <summary>
        /// 外接盒-二次盒-06
        /// </summary>
        public static CalChannelMode CH2_06
        {
            get
            {
                return _CH2_06;
            }

            set
            {
                _CH2_06 = value;
            }
        }

        /// <summary>
        /// 外接盒-二次盒-07
        /// </summary>
        public static CalChannelMode CH2_07
        {
            get
            {
                return _CH2_07;
            }

            set
            {
                _CH2_07 = value;
            }
        }

        /// <summary>
        /// 外接盒-二次盒-08
        /// </summary>
        public static CalChannelMode CH2_08
        {
            get
            {
                return _CH2_08;
            }

            set
            {
                _CH2_08 = value;
            }
        }

        /// <summary>
        /// 外接盒-二次盒-09
        /// </summary>
        public static CalChannelMode CH2_09
        {
            get
            {
                return _CH2_09;
            }

            set
            {
                _CH2_09 = value;
            }
        }

        /// <summary>
        /// 外接盒-二次盒-10
        /// </summary>
        public static CalChannelMode CH2_10
        {
            get
            {
                return _CH2_10;
            }

            set
            {
                _CH2_10 = value;
            }
        }

        /// <summary>
        /// 重写判等方法
        /// </summary>
        /// <param name="c1"></param>
        /// <param name="c2"></param>
        /// <returns></returns>
        public static bool operator ==(CalChannelMode c1, CalChannelMode c2)
        {
            if (((c1 as object) != null) && ((c2 as object) != null))
            {
                return c1.ID == c2.ID;
            }
            else if (((c1 as object) == null) && ((c2 as object) == null))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 重写不等方法
        /// </summary>
        /// <param name="c1"></param>
        /// <param name="c2"></param>
        /// <returns></returns>
        public static bool operator !=(CalChannelMode c1, CalChannelMode c2)
        {
            if (((c1 as object) != null) && ((c2 as object) != null))
            {
                return c1.ID != c2.ID;
            }
            else if (((c1 as object) == null) && ((c2 as object) == null))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// 转换方法
        /// </summary>
        /// <param name="ChannlName">通道名</param>
        /// <returns></returns>
        public static CalChannelMode Parse(string ChannlName)
        {
            if (ChannlName == REF1.Name)
                return REF1;
            else if (ChannlName == REF2.Name)
                return REF2;
            else if (ChannlName == CH1_01A.Name)
                return CH1_01A;
            else if (ChannlName == CH1_02A.Name)
                return CH1_02A;
            else if (ChannlName == CH1_03A.Name)
                return CH1_03A;
            else if (ChannlName == CH1_04A.Name)
                return CH1_04A;
            else if (ChannlName == CH1_05A.Name)
                return CH1_05A;
            else if (ChannlName == CH1_06A.Name)
                return CH1_06A;
            else if (ChannlName == CH1_07A.Name)
                return CH1_07A;
            else if (ChannlName == CH1_08A.Name)
                return CH1_08A;
            else if (ChannlName == CH1_09A.Name)
                return CH1_09A;
            else if (ChannlName == CH1_10A.Name)
                return CH1_10A;
            else if (ChannlName == CH1_01B.Name)
                return CH1_01B;
            else if (ChannlName == CH1_02B.Name)
                return CH1_02B;
            else if (ChannlName == CH1_03B.Name)
                return CH1_03B;
            else if (ChannlName == CH1_04B.Name)
                return CH1_04B;
            else if (ChannlName == CH1_05B.Name)
                return CH1_05B;
            else if (ChannlName == CH1_06B.Name)
                return CH1_06B;
            else if (ChannlName == CH1_07B.Name)
                return CH1_07B;
            else if (ChannlName == CH1_08B.Name)
                return CH1_08B;
            else if (ChannlName == CH1_09B.Name)
                return CH1_09B;
            else if (ChannlName == CH1_10B.Name)
                return CH1_10B;
            else if (ChannlName == CH2_01A.Name)
                return CH2_01A;
            else if (ChannlName == CH2_02A.Name)
                return CH2_02A;
            else if (ChannlName == CH2_03A.Name)
                return CH2_03A;
            else if (ChannlName == CH2_04A.Name)
                return CH2_04A;
            else if (ChannlName == CH2_05A.Name)
                return CH2_05A;
            else if (ChannlName == CH2_06A.Name)
                return CH2_06A;
            else if (ChannlName == CH2_07A.Name)
                return CH2_07A;
            else if (ChannlName == CH2_08A.Name)
                return CH2_08A;
            else if (ChannlName == CH2_09A.Name)
                return CH2_09A;
            else if (ChannlName == CH2_10A.Name)
                return CH2_10A;
            else if (ChannlName == CH2_01B.Name)
                return CH2_01B;
            else if (ChannlName == CH2_02B.Name)
                return CH2_02B;
            else if (ChannlName == CH2_03B.Name)
                return CH2_03B;
            else if (ChannlName == CH2_04B.Name)
                return CH2_04B;
            else if (ChannlName == CH2_05B.Name)
                return CH2_05B;
            else if (ChannlName == CH2_06B.Name)
                return CH2_06B;
            else if (ChannlName == CH2_07B.Name)
                return CH2_07B;
            else if (ChannlName == CH2_08B.Name)
                return CH2_08B;
            else if (ChannlName == CH2_09B.Name)
                return CH2_09B;
            else if (ChannlName == CH2_10B.Name)
                return CH2_10B;
            else if (ChannlName == CH1_01.Name)
                return CH1_01;
            else if (ChannlName == CH1_02.Name)
                return CH1_02;
            else if (ChannlName == CH1_03.Name)
                return CH1_03;
            else if (ChannlName == CH1_04.Name)
                return CH1_04;
            else if (ChannlName == CH1_05.Name)
                return CH1_05;
            else if (ChannlName == CH1_06.Name)
                return CH1_06;
            else if (ChannlName == CH1_07.Name)
                return CH1_07;
            else if (ChannlName == CH1_08.Name)
                return CH1_08;
            else if (ChannlName == CH1_09.Name)
                return CH1_09;
            else if (ChannlName == CH1_10.Name)
                return CH1_10;
            else if (ChannlName == CH2_01.Name)
                return CH2_01;
            else if (ChannlName == CH2_02.Name)
                return CH2_02;
            else if (ChannlName == CH2_03.Name)
                return CH2_03;
            else if (ChannlName == CH2_04.Name)
                return CH2_04;
            else if (ChannlName == CH2_05.Name)
                return CH2_05;
            else if (ChannlName == CH2_06.Name)
                return CH2_06;
            else if (ChannlName == CH2_07.Name)
                return CH2_07;
            else if (ChannlName == CH2_08.Name)
                return CH2_08;
            else if (ChannlName == CH2_09.Name)
                return CH2_09;
            else if (ChannlName == CH2_10.Name)
                return CH2_10;
            else
                return Unkonw;
        }

        /// <summary>
        /// 转换通道
        /// </summary>
        /// <param name="id">ID</param>
        /// <param name="isembeddedbox">是否为内嵌盒</param>
        /// <returns></returns>
        public static CalChannelMode Parse(int id, bool isembeddedbox)
        {
            if (isembeddedbox)
            {
                if (id == 1)
                    return CH1_01A;
                else if (id == 2)
                    return CH1_02A;
                else if (id == 3)
                    return CH1_03A;
                else if (id == 4)
                    return CH1_04A;
                else if (id == 5)
                    return CH1_05A;
                else if (id == 6)
                    return CH1_06A;
                else if (id == 7)
                    return CH1_07A;
                else if (id == 8)
                    return CH1_08A;
                else if (id == 9)
                    return CH1_09A;
                else if (id == 10)
                    return CH1_10A;
                else if (id == 11)
                    return CH1_01B;
                else if (id == 12)
                    return CH1_02B;
                else if (id == 13)
                    return CH1_03B;
                else if (id == 14)
                    return CH1_04B;
                else if (id == 15)
                    return CH1_05B;
                else if (id == 16)
                    return CH1_06B;
                else if (id == 17)
                    return CH1_07B;
                else if (id == 18)
                    return CH1_08B;
                else if (id == 19)
                    return CH1_09B;
                else if (id == 20)
                    return CH1_10B;
                else
                    return Unkonw;
            }
            else
            {
                if (id == 1)
                    return CH2_01A;
                else if (id == 2)
                    return CH2_02A;
                else if (id == 3)
                    return CH2_03A;
                else if (id == 4)
                    return CH2_04A;
                else if (id == 5)
                    return CH2_05A;
                else if (id == 6)
                    return CH2_06A;
                else if (id == 7)
                    return CH2_07A;
                else if (id == 8)
                    return CH2_08A;
                else if (id == 9)
                    return CH2_09A;
                else if (id == 10)
                    return CH2_10A;
                else if (id == 11)
                    return CH2_01B;
                else if (id == 12)
                    return CH2_02B;
                else if (id == 13)
                    return CH2_03B;
                else if (id == 14)
                    return CH2_04B;
                else if (id == 15)
                    return CH2_05B;
                else if (id == 16)
                    return CH2_06B;
                else if (id == 17)
                    return CH2_07B;
                else if (id == 18)
                    return CH2_08B;
                else if (id == 19)
                    return CH2_09B;
                else if (id == 20)
                    return CH2_10B;
                else
                    return Unkonw;
            }
        }

        /// <summary>
        /// 转换RTD通道
        /// </summary>
        /// <param name="id">ID</param>
        /// <param name="isembeddedbox">是否为内嵌盒</param>
        /// <returns></returns>
        public static CalChannelMode ParseRTDChannel(int id, bool isembeddedbox)
        {
            if (isembeddedbox)
            {
                if (id == 1)
                    return CH1_01;
                else if (id == 2)
                    return CH1_02;
                else if (id == 3)
                    return CH1_03;
                else if (id == 4)
                    return CH1_04;
                else if (id == 5)
                    return CH1_05;
                else if (id == 6)
                    return CH1_06;
                else if (id == 7)
                    return CH1_07;
                else if (id == 8)
                    return CH1_08;
                else if (id == 9)
                    return CH1_09;
                else if (id == 10)
                    return CH1_10;
                else
                    return Unkonw;
            }
            else
            {
                if (id == 1)
                    return CH2_01;
                else if (id == 2)
                    return CH2_02;
                else if (id == 3)
                    return CH2_03;
                else if (id == 4)
                    return CH2_04;
                else if (id == 5)
                    return CH2_05;
                else if (id == 6)
                    return CH2_06;
                else if (id == 7)
                    return CH2_07;
                else if (id == 8)
                    return CH2_08;
                else if (id == 9)
                    return CH2_09;
                else if (id == 10)
                    return CH2_10;
                else
                    return Unkonw;
            }
        }
    }
}
