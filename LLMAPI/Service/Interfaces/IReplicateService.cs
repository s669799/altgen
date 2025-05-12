using System.Collections.Generic;
using System.Threading.Tasks;

namespace LLMAPI.Service.Interfaces
{
    /// <summary>
    /// Interface defining the contract for services interacting with the Replicate API.
    /// </summary>
    public interface IReplicateService
    {
        /// <summary>
        /// Creates a prediction in Replicate with the specified model version and input parameters.
        /// </summary>
        /// <param name="modelVersion">The version of the model to be used for prediction.</param>
        /// <param name="input">A dictionary of input parameters required by the model.</param>
        /// <returns>The ID of the newly created prediction.</returns>
        Task<string> CreatePrediction(string modelVersion, Dictionary<string, object> input);

        /// <summary>
        /// Retrieves the result of a prediction from Replicate using its prediction ID.
        /// </summary>
        /// <param name="predictionId">The ID of the prediction for which to retrieve the result.</param>
        /// <returns>The JSON string containing the prediction result.</returns>
        Task<string> GetPredictionResult(string predictionId);

        /// <summary>
        /// Tests the account access to the Replicate API by calling the account endpoint.
        /// </summary>
        /// <returns>The JSON string response from the Replicate account endpoint.</returns>
        Task<string> TestAccountAccess();
    }
}
