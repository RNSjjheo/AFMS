using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSSediService
{
    public sealed class RadsReplicationOptions
    {
        public const string SectionName = "RadsReplication";

        public bool Enabled { get; set; }
        public string RemoteHost { get; set; } = "localhost";
        public int RemotePort { get; set; } = 3050;
        public string RemoteDatabase { get; set; } = @"D:\RADS\Database\RADS.FDB";
        public string UserId { get; set; } = "rads";
        public string Password { get; set; } = string.Empty;
        public string Charset { get; set; } = "UTF8";
        public int ConnectionTimeoutSeconds { get; set; } = 10;
        public DateTime StartTime { get; set; } =
            new(2026, 9, 1, 0, 0, 0, DateTimeKind.Local);
        public int BatchSize { get; set; } = 100;
        public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(10);

        public void Validate()
        {
            if (!Enabled) return;
            if (string.IsNullOrWhiteSpace(RemoteHost))
                throw new InvalidOperationException("RADS 원격 DB 호스트가 설정되지 않았습니다.");
            if (RemotePort is < 1 or > 65535)
                throw new InvalidOperationException("RADS 원격 DB 포트가 올바르지 않습니다.");
            if (string.IsNullOrWhiteSpace(RemoteDatabase))
                throw new InvalidOperationException("RADS 원격 DB 경로가 설정되지 않았습니다.");
            if (string.IsNullOrWhiteSpace(UserId))
                throw new InvalidOperationException("RADS 원격 DB 계정이 설정되지 않았습니다.");
            if (string.IsNullOrWhiteSpace(Password))
                throw new InvalidOperationException("RADS 원격 DB 비밀번호가 설정되지 않았습니다.");
            if (BatchSize <= 0)
                throw new InvalidOperationException("RADS 복제 배치 크기는 1 이상이어야 합니다.");
            if (PollInterval <= TimeSpan.Zero)
                throw new InvalidOperationException("RADS 복제 조회 주기는 0보다 커야 합니다.");
            if (ConnectionTimeoutSeconds <= 0)
                throw new InvalidOperationException("RADS 원격 DB 연결 제한시간은 1초 이상이어야 합니다.");
        }
    }
}
