
using KDMApi.DataContexts;
using KDMApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KDMApi.Services
{
    public class ActivityLogService
    {
        private DefaultContext _context;

        public ActivityLogService(DefaultContext context)
        {
            _context = context;
        }

        public async Task<KmActivityLog> AddLog(string action, int userId, int fileId, DateTime dt)
        {
            KmActivityLog log = new KmActivityLog()
            {
                Action = action,
                UserId = userId,
                FileId = fileId,
                CreatedDate = dt
            };
            _context.KmActivityLogs.Add(log);
            await _context.SaveChangesAsync();

            return log;
        }
    }
}
