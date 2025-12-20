using IoTMonitoringSystem.Core.DTOs;

namespace IoTMonitoringSystem.Services
{
    public interface IAlertRuleService
    {
        Task<List<AlertRuleDto>> GetAllAlertRulesAsync();
        Task<AlertRuleDto> GetAlertRuleByIdAsync(int alertRuleId);
        Task<AlertRuleDto> CreateAlertRuleAsync(CreateAlertRuleDto dto);
        Task<AlertRuleDto> UpdateAlertRuleAsync(int alertRuleId, UpdateAlertRuleDto dto);
        Task DeleteAlertRuleAsync(int alertRuleId);
        Task<List<AlertRuleDto>> GetAlertRulesByDeviceIdAsync(int deviceId);
    }
}

