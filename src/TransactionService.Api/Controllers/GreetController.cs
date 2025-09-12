using Microsoft.AspNetCore.Mvc;
using TransactionService.Grpc;

namespace TransactionService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GreetController(Greeter.GreeterClient greeter) : ControllerBase
    {
        private readonly Greeter.GreeterClient _greeter = greeter;

        /// <summary>
        /// Sends a greeting using gRPC.
        /// </summary>
        /// <param name="name">The name to greet.</param>
        /// <returns>A greeting message.</returns>
        [HttpGet("{name}")]
        public async Task<IActionResult> SayHello(string name)
        {
            var reply = await _greeter.SayHelloAsync(new HelloRequest { Name = name });
            return Ok(reply.Message);
        }
    }
}
