using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VL.Lib.IO
{
    // Used by VL.Sync.vl
    public record struct PlayerController(float LoopFrom, float LoopTo = -1, bool Loop = default, float SeekTime = default, int Seek = default, float Rate = default, bool Play = default);
    public record struct FrameController(int FrameIncrement = 1, bool Loop = default, int LoopFrom = default, int LoopTo = -1, int SeekFrame = 0, bool Seek = false, int IncrementEveryNthFrame = 1);
}
