using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;

namespace AICore.ObjectDetection
{
    interface IObjectDetection
    {
        Task<Data.Output[]> ProcessImageAsync(Data.Input input);
    }
}
