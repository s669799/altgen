using System.Collections.Generic;
using System.Threading.Tasks;

namespace LLMAPI.Service.Interfaces
{
    public interface IReplicateService
    {
        Task<string> CreatePrediction(string modelVersion, Dictionary<string, object> input);
        Task<string> GetPredictionResult(string predictionId);
        Task<string> TestAccountAccess();
    }
}