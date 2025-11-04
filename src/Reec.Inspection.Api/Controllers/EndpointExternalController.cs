using Microsoft.AspNetCore.Mvc;

namespace Reec.Inspection.Api.Controllers
{
    [ApiController]
    public class EndpointExternalController : Controller
    {
        private readonly IHttpClientFactory _factory;
        private const string NameClient = "PlaceHolder";
        public EndpointExternalController(IHttpClientFactory factory)
        {
            this._factory = factory;
        }

        [HttpGet(nameof(Users))]
        public async Task<IActionResult> Users()
        {
            var client = _factory.CreateClient(NameClient);
            var response = await client.GetAsync("users");
            return Ok(response.Content.ReadAsStream());
        }

        [HttpGet(nameof(UsersErrorLink))]
        public async Task<IActionResult> UsersErrorLink()
        {
            var client = _factory.CreateClient(NameClient);
            var response = await client.GetAsync("users_errorLink/2");
            return Ok(response.Content.ReadAsStream());
        }

    }
}
