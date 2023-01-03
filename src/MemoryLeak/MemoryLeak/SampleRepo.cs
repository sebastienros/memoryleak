using MemoryLeak.DataModels;
using Microsoft.EntityFrameworkCore;

namespace MemoryLeak.Repositories
{
    public class SampleRepo
    {
        private readonly MyDbContext ctx;

        public SampleRepo(MyDbContext ctx)
        {
            this.ctx = ctx;
        }

        //Every 2 seconds
        public List<T_TOAST> GetLayers(string id)
        {
            try
            {
                List<T_TOAST> result = ctx.T_TOAST.AsNoTracking().Where(x => x.C_ID == id).ToList();
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}