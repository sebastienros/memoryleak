using System.ComponentModel.DataAnnotations;

namespace MemoryLeak.DataModels
{
    public class T_TOAST
    {
        [Key]
        public string C_ID { get; internal set; }
        public string C_MAT { get; internal set; }
        public string C_MAT_DESC { get; internal set; }
    }
}