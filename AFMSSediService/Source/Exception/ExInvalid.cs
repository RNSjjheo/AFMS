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
    }
}
