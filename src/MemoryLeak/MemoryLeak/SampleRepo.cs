using MemoryLeak.DataModels;
using Microsoft.EntityFrameworkCore;

namespace MemoryLeak.Repositories
{
    public class SampleRepo
    {
        private readonly MLTLVL2Context ctx;

        public  SampleRepo(MLTLVL2Context ctx)
            {
            this.ctx = ctx;
        }

        //Every 2 seconds
        public List<EAF_BKT_ASSGN_MAT> GetLayers(string Toastid)
        {
            try
            {
                List<EAF_BKT_ASSGN_MAT> result = ctx.EAF_BKT_ASSGN_MAT.AsNoTracking().Where(x => x.C_Toast == Toastid).ToList();
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}