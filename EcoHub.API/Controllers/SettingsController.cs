using EcoHub.API.Services;
using EcoHub.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoHub.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly ISettingsService _settingsService;

        public SettingsController(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        [HttpGet]
        public async Task<ActionResult<List<SystemSettingDto>>> GetAll()
        {
            var settings = await _settingsService.GetAllAsync();
            return Ok(settings);
        }

        [HttpGet("{key}")]
        public async Task<ActionResult<SystemSettingDto>> GetByKey(string key)
        {
            var setting = await _settingsService.GetByKeyAsync(key);
            if (setting == null) return NotFound();
            return Ok(setting);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{key}")]
        public async Task<IActionResult> Set(string key, [FromBody] string value)
        {
            await _settingsService.SetAsync(key, value);
            return NoContent();
        }
    }
}
