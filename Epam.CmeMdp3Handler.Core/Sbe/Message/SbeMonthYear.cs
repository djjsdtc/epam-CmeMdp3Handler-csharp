namespace Epam.CmeMdp3Handler.Sbe.Message
{
    public class SbeMonthYear
    {
        private int _year;
        private short _month;
        private short _day;
        private short _week;

        // Null values from SbePrimitiveType
        private const int NULL_YEAR   = 0xFFFE; // UInt16 null
        private const short NULL_MONTH = unchecked((short)0xFF); // UInt8 null cast to short
        private const short NULL_DAY  = unchecked((short)0xFF);
        private const short NULL_WEEK = unchecked((short)0xFF);

        public void Reset()
        {
            _year = NULL_YEAR;
            _month = NULL_MONTH;
            _day = NULL_DAY;
            _week = NULL_WEEK;
        }

        public int GetYear() => _year;
        public void SetYear(int year) { _year = year; }

        public short GetMonth() => _month;
        public void SetMonth(short month) { _month = month; }

        public short GetDay() => _day;
        public void SetDay(short day) { _day = day; }

        public short GetWeek() => _week;
        public void SetWeek(short week) { _week = week; }

        public bool IsNullYear()  => _year == NULL_YEAR;
        public bool IsNullMonth() => _month == NULL_MONTH;
        public bool IsNullDay()   => _day == NULL_DAY;
        public bool IsNullWeek()  => _week == NULL_WEEK;
    }
}
