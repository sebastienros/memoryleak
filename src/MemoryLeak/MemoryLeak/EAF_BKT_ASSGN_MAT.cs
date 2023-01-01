using System.ComponentModel.DataAnnotations;

namespace MemoryLeak.DataModels
{
    public class EAF_BKT_ASSGN_MAT
    {
        [Key]
        public string C_Toast { get; internal set; }
        public string C_MAT { get; internal set; }
        public string C_MAT_DESC { get; internal set; }
    }
}