using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace AFMSSediService
{
    public enum InvalidType
    {
        ProgromPathUnknown
    }
    internal static class ExInvalid
    {
        public static InvalidOperationException Throw(InvalidType type)
        {
            string msg = "";

            switch (type)
            {
                case InvalidType.ProgromPathUnknown:
                    msg = "현재 프로그램 경로를 확인할 수 없습니다.";
                    break; 
            }

            return new InvalidOperationException(msg);
        }     
    }
}
