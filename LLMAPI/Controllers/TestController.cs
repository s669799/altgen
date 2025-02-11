using LLMAPI.Service;
using Microsoft.AspNetCore.Mvc;

namespace LLMAPI.Controllers;



[ApiController]
[Route("[controller]")]
public class TestController : ControllerBase
{

    private readonly ILLMService _llmService;

    public TestController(ILLMService llmService)
    {
        _llmService = llmService;
    }
    
    
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

    
    
}