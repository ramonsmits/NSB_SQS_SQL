using System;
using System.Threading.Tasks;
using System.Web.Http;
using NSB_AWS.API.Models;
using Shared.Command;
using NServiceBus;

namespace API.Controllers
{
    public class OrderController : ApiController
    {
        readonly IEndpointInstance endpoint;

        public OrderController(IEndpointInstance endpoint)
        {
            this.endpoint = endpoint;
        }

        [Route("order")]
        [HttpPost]
        public async Task<IHttpActionResult> CommitOrder(Order order)
        {
            var placeOrder = new PlaceOrder
            {
                Product = order.ProductCode,
                Id = Guid.NewGuid()
            };

            await endpoint.Send("NSB.Server", placeOrder)
                .ConfigureAwait(false);

            return Ok("Order received : " + placeOrder.Id);
        }


        [Route("bulkorder")]
        [HttpPost]
        public async Task<IHttpActionResult> BulkOrder(Order order)
        {
            var bulkOrder = new BulkOrder
            {
                Data = new byte[257 * 1024],
                Id = Guid.NewGuid()
            };

            await endpoint.Send("NSB.Server", bulkOrder)
                .ConfigureAwait(false);

            return Ok("Order received : " + bulkOrder.Id);
        }
    }
}
