using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuyOldBike_DAL.Entities
{
    public partial class FrameSize
    {
        public int FrameSizeId { get; set; }
        public string SizeValue { get; set; } = null!;
    }
}
