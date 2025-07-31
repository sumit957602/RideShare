using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class FrontendProxyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public FrontendProxyController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("home-about")]
    public async Task<IActionResult> GetHomeAboutPage()
    {
        var client = _httpClientFactory.CreateClient("FrontendClient");

        HttpResponseMessage response = await client.GetAsync("Home/About");

        if (response.IsSuccessStatusCode)
        {
            string htmlContent = await response.Content.ReadAsStringAsync();
            return Content(htmlContent, "text/html");
        }

        return StatusCode((int)response.StatusCode, "Error accessing frontend page.");
    }

    [HttpGet("user-dashboard")]
    public async Task<IActionResult> GetUserDashboard()
    {
        var client = _httpClientFactory.CreateClient("FrontendClient");

        HttpResponseMessage response = await client.GetAsync("UserDashboard/User");

        if (response.IsSuccessStatusCode)
        {
            string htmlContent = await response.Content.ReadAsStringAsync();
            return Content(htmlContent, "text/html");
        }

        return StatusCode((int)response.StatusCode, "Failed to fetch user dashboard");
    }
}
