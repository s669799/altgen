using LLMAPI.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;


namespace LLMAPI.Controllers;



[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{

    private readonly ILLMService _llmService;

    public TestController(ILLMService llmService)
    {
        _llmService = llmService;
    }
    
/*     
    /// <summary>
    /// Does a get request to open router
    /// </summary>
    /// <returns> nothing atm </returns>
    [HttpGet(Name = "Test get method")]
    public async Task<IActionResult> Something()
    {
        var data = _llmService.GetDataFromImageGoogle();
        return Ok(data);
    }

    [HttpPost(Name = "post request")]
    public IActionResult poopy()
    {
        return Ok();
    }
 */
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { message = "Hello, this is the API!" });
    }

    [HttpPost("google/gemini-flash-1.5-8b")]
    public async Task<IActionResult> Model1Response([FromBody] PromptRequest request)
    {
        string response = await _llmService.GetDataOpenRouter("google/gemini-flash-1.5-8b", request.Prompt);
        return Ok(new { response });
    }

    [HttpPost("openai/gpt-4o-mini")]
    public async Task<IActionResult> Model2Response([FromBody] PromptRequest request)
    {
        string response = await _llmService.GetDataOpenRouter("openai/gpt-4o-mini", request.Prompt);
        return Ok(new { response });
    }
        
    [HttpPost("meta-llama/llama-3.3-70b-instruct")]
    public async Task<IActionResult> Model3Response([FromBody] PromptRequest request)
    {
        string response = await _llmService.GetDataOpenRouter("meta-llama/llama-3.3-70b-instruct", request.Prompt);
        return Ok(new { response });
    }
                
    [HttpPost("deepseek/deepseek-r1")]
    public async Task<IActionResult> Model4Response([FromBody] PromptRequest request)
    {
        string response = await _llmService.GetDataOpenRouter("deepseek/deepseek-r1", request.Prompt);
        return Ok(new { response });
    }


    public class PromptRequest
    {
        public string? Prompt { get; set; }
    }
    
}