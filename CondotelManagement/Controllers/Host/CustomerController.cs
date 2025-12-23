using CondotelManagement.DTOs;
using CondotelManagement.Helpers;
using CondotelManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CondotelManagement.Services.Interfaces;
using CondotelManagement.Models;

namespace CondotelManagement.Controllers.Host
{
    [ApiController]
    [Route("api/host/[controller]")]
    [Authorize(Roles = "Host")]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly IHostService _hostService;

        public CustomerController(ICustomerService customerService, IHostService hostService)
        {
            _customerService = customerService;
            _hostService = hostService;
        }
        //GET all customer was booked
        [HttpGet]
        public async Task<IActionResult> GetCustomerBooked()
        {
            //current host login
            var hostId = _hostService.GetByUserId(User.GetUserId()).HostId;
            var customers = await _customerService.GetCustomerBookingsByHostAsync(hostId);
            return Ok(ApiResponse<object>.SuccessResponse(customers));

		}
    }
}
