using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace AFMSSediService
{
    internal static class ExInvalid
    {
        public static InvalidOperationException ProgramPathUnknown()
        {
            return new InvalidOperationException("현재 프로그램 경로를 확인할 수 없습니다.");
        }

        public static InvalidOperationException SlotProcessCanotCreateSlot(DateTime time, string error)
        {
            return new InvalidOperationException($"{time:yyyy-MM-dd HH:mm:ss} SEDI 슬롯 생성 실패: {error}");
        }
    }
}
